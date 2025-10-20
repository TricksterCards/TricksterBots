using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestPitchBot
    {

        private static readonly PitchOptions fourPointDrawWithKittyOptions = new PitchOptions
        {
            variation = PitchVariation.FourPoint,
            drawOption = PitchDrawOption.NonTrump,
            gameOverScore = 15,
            isPartnership = true,
            kitty = 3,
            lowGoesToTaker = true,
            minBid = 2,
            offerShootBid = false,
            pitcherLeadsTrump = true,
            playTrump = PitchPlayTrump.Anytime,
            stickTheDealer = true,
        };

        private static readonly PitchOptions tenPointOptions = new PitchOptions
        {
            variation = PitchVariation.TenPoint,
            gameOverScore = 32,
            isPartnership = true,
            lowGoesToTaker = false,
            minBid = 5,
            pitcherLeadsTrump = true,
            playTrump = PitchPlayTrump.Only,
            stickTheDealer = true,
            tenOfTrumpReplacesGamePoint = true,
        };

        [TestMethod]
        [DataRow("ACKC3S6H7HQH",  3, false, PitchVariation.FourPoint)]
        [DataRow("7D5S8SQSAS6H",  3, false, PitchVariation.FourPoint)]
        [DataRow("ACKCQCJC2CAH",  4, false, PitchVariation.FourPoint)]
        [DataRow("6C5C5S4S3D2D",  0, false, PitchVariation.FourPoint)]
        [DataRow("ACKCQCJC2CAH", 15,  true, PitchVariation.FourPoint)]
        [DataRow("KCQCJCJS3C2C",  8, false, PitchVariation.TenPoint )]
        [DataRow("JCJSHJLJTC2C",  6, false, PitchVariation.TenPoint )]
        [DataRow("JCJS5C4C3C2C",  6, false, PitchVariation.TenPoint )]
        public void TestBids(string handString, int expectedBid, bool offerShoot, PitchVariation variation)
        {
            var isTenPoint = variation == PitchVariation.TenPoint;
            var options = isTenPoint ? tenPointOptions : new PitchOptions
            {
                variation = variation,
                drawOption = PitchDrawOption.NonTrump,
                gameOverScore = 15,
                isPartnership = true,
                lowGoesToTaker = true,
                minBid = 2,
                offerShootBid = offerShoot,
                pitcherLeadsTrump = true,
                playTrump = PitchPlayTrump.Anytime,
                stickTheDealer = true,
            };

            var bid = (int)PitchBid.Base + expectedBid;
            if (expectedBid == 0)
                bid = BidBase.Pass;
            if (expectedBid == 15)
                bid = (int)PitchBid.ShootMoonBid;

            var suggestion = GetSuggestedBid(handString, out var hand, options);
            var suggestionValue = suggestion >= (int)PitchBid.Base ? suggestion - (int)PitchBid.Base : suggestion;
            Assert.AreEqual(bid, suggestion, $"Expect bid of {expectedBid} for hand {Util.PrettyHand(hand)}, but got {suggestionValue}");
        }

        [TestMethod]
        public void TestHighBids()
        {
            Assert.AreEqual((int)PitchBid.Base + 3, GetSuggestedBid("AHQH6H5DJC5C", out var hand, fourPointDrawWithKittyOptions), $"Expect bid of 3 for hand {Util.PrettyHand(hand)}");
        }

        [TestMethod]
        public void PlayLowFirstTrickInCallPartner()
        {
            var players = new[]
            {
                new TestPlayer((int)PitchBid.NotPitching, "KH4HTS9STC9C"),
                new TestPlayer((int)PitchBid.NotPitching),
                new TestPlayer((int)PitchBid.NotPitching),
                new TestPlayer((int)PitchBid.NotPitching),
                new TestPlayer(GetBid(5, Suit.Hearts)),
            };

            var bot = new PitchBot(GetCallForBestOptions(callPartnerSeat: 2), Suit.Hearts);
            var cardState = new TestCardState<PitchOptions>(bot, players, "3H");
            var suggestion = bot.SuggestNextCard(cardState);

            Assert.AreEqual("4H", $"{suggestion}");
        }

        [TestMethod]
        [DataRow("3H", "4H3H2HTS9S")]
        [DataRow("3H", "JH3H2HTS9S")]
        [DataRow("JH", "JH4H2HTS9S")]
        [DataRow("HJ", "JDHJ2HTS9S")]
        [DataRow("JH", "AHJH2HTS9S")]
        public void LeadBackPointsSecondTrickInCallPartner(string card, string hand)
        {
            var players = new[]
            {
                new TestPlayer((int)PitchBid.NotPitching, hand),
                new TestPlayer((int)PitchBid.NotPitching),
                new TestPlayer((int)PitchBid.NotPitching),
                new TestPlayer((int)PitchBid.NotPitching),
                new TestPlayer(GetBid(5, Suit.Hearts)),
            };

            var bot = new PitchBot(GetCallForBestOptions(callPartnerSeat: 0), Suit.Hearts);
            var cardState = new TestCardState<PitchOptions>(bot, players);
            var suggestion = bot.SuggestNextCard(cardState);

            Assert.AreEqual(card, $"{suggestion}");
        }

        [TestMethod]
        public void DuckIfPartnerTakingTrick()
        {
            var players = new[]
            {
                new TestPlayer((int)PitchBid.NotPitching, "3H6SQS"),
                new TestPlayer(GetBid(2, Suit.Spades), "0U0U"),
                new TestPlayer((int)PitchBid.NotPitching, "0U0U"),
                new TestPlayer((int)PitchBid.NotPitching, "0U0U"),
            };

            var bot = new PitchBot(fourPointDrawWithKittyOptions, Suit.Spades);
            var cardState = new TestCardState<PitchOptions>(bot, players, "TC7S9C");
            var suggestion = bot.SuggestNextCard(cardState);

            Assert.AreEqual("3H", $"{suggestion}");
        }


        [TestMethod]
        [DataRow("",       "ADJHQHAH", "AH", PitchVariation.FourPoint,  true, 0, DisplayName = "Lead high trump if pitcher")]
        [DataRow("",       "ADJHQHAH", "AH", PitchVariation.FourPoint,  true, 2, DisplayName = "Lead high trump if pitcher's partner")]
        [DataRow("",       "ADJHQHAH", "AD", PitchVariation.FourPoint,  true, 1, DisplayName = "Avoid leading trump on defense")]
        [DataRow("",       "5H9HJHQH", "5H", PitchVariation.FourPoint,  true, 0, DisplayName = "Lead a low non-pointer if not holding high")]
        [DataRow("",       "5H6H9HJH", "6H", PitchVariation.FourPoint,  true, 0, DisplayName = "Lead higher of touching low cards")]
        [DataRow("",       "2H4H9HJH", "4H", PitchVariation.FourPoint,  true, 0, DisplayName = "Avoid leading capturable low")]
        [DataRow("",       "2H4H9HJH", "2H", PitchVariation.FourPoint, false, 0, DisplayName = "Lead non-capturable low")]
        [DataRow("",       "3H7H9HJH", "7H", PitchVariation.FourPoint,  true, 0, DisplayName = "Avoid leading possible capturable low")]
        [DataRow("AH",         "QHKH", "QH", PitchVariation.FourPoint,  true, 0, DisplayName = "Dump lower card")]
        [DataRow("AH",         "THJH", "TH", PitchVariation.FourPoint,  true, 0, DisplayName = "Dump card worth fewer points")]
        [DataRow("AH",         "THQH", "QH", PitchVariation.FourPoint,  true, 0, DisplayName = "Dump card worth fewer game points")]
        [DataRow("AS",     "5S2H7H9H", "7H", PitchVariation.FourPoint,  true, 0, DisplayName = "Trump in above capturable low from 2nd seat")]
        [DataRow("AS",     "5S2H2D2C", "5S", PitchVariation.FourPoint,  true, 0, DisplayName = "Avoid trumping in with capturable low from 2nd seat")]
        [DataRow("AS",     "5S2H7H9H", "2H", PitchVariation.FourPoint, false, 0, DisplayName = "Trump in with non-capturable low from 2nd seat")]
        [DataRow("ASQS",   "5S2H7H9H", "7H", PitchVariation.FourPoint,  true, 0, DisplayName = "Trump in above capturable low from 3rd seat")]
        [DataRow("ASQS",   "5S2H2D2C", "5S", PitchVariation.FourPoint,  true, 0, DisplayName = "Avoid trumping in with capturable low from 3rd seat")]
        [DataRow("ASQS",   "5S2H7H9H", "2H", PitchVariation.FourPoint, false, 0, DisplayName = "Trump in with non-capturable low from 3rd seat")]
        [DataRow("JSQSAS", "5C7HJHKH", "JH", PitchVariation.FourPoint,  true, 0, DisplayName = "Trump in with a pointer from last seat")]
        [DataRow("JSQSAS", "5C2HJHKH", "2H", PitchVariation.FourPoint,  true, 0, DisplayName = "Trump in with lowest pointer from last seat")]
        [DataRow("JSQSAS", "5C2HJHKH", "JH", PitchVariation.FourPoint, false, 0, DisplayName = "Trump in with lowest capturable pointer from last seat")]
        [DataRow("JSQSAS", "5S7HJHKH", "JH", PitchVariation.FourPoint,  true, 0, DisplayName = "Trump in with a pointer from last seat (could follow)")]
        [DataRow("JSQSAS", "5S2HJHKH", "2H", PitchVariation.FourPoint,  true, 0, DisplayName = "Trump in with lowest pointer from last seat (could follow)")]
        [DataRow("JSQSAS", "5S2HJHKH", "JH", PitchVariation.FourPoint, false, 0, DisplayName = "Trump in with lowest capturable pointer from last seat (could follow)")]
        [DataRow("6DAD8H", "5C7HJHKH", "JH", PitchVariation.FourPoint,  true, 0, DisplayName = "Over-trump with a pointer from last seat")]
        [DataRow("4CAC4H", "AS2C2H8H", "8H", PitchVariation.FourPoint,  true, 0, DisplayName = "Over-trump from last seat if trick is worth taking")]
        [DataRow("6DAD8H", "5C2HTHJH", "TH", PitchVariation. TenPoint,  true, 0, DisplayName = "Over-trump with lowest pointer from last seat")]
        public void PlayOfTheHand(string trick, string hand, string expected, PitchVariation variation, bool lowToTaker, int pitchingSeat)
        {
            PitchOptions baseOptions;
            switch (variation)
            {
                case PitchVariation.FourPoint:
                    baseOptions = fourPointDrawWithKittyOptions;
                    break;
                case PitchVariation.TenPoint:
                    baseOptions = tenPointOptions;
                    break;
                default:
                    throw new Exception($"Unsupported variation: {variation}");
            }

            var trump = Suit.Hearts;
            var options = JsonConvert.DeserializeObject<PitchOptions>(JsonConvert.SerializeObject(baseOptions));
            var players = new[]
            {
                new TestPlayer(pitchingSeat == 0 ? GetBid(options.minBid, trump) : (int)PitchBid.NotPitching, hand),
                new TestPlayer(pitchingSeat == 1 ? GetBid(options.minBid, trump) : (int)PitchBid.NotPitching, string.Concat(Enumerable.Repeat("0U", hand.Length/2 - ((trick.Length/2 > 2) ? 1 : 0)))),
                new TestPlayer(pitchingSeat == 2 ? GetBid(options.minBid, trump) : (int)PitchBid.NotPitching, string.Concat(Enumerable.Repeat("0U", hand.Length/2 - ((trick.Length/2 > 1) ? 1 : 0)))),
                new TestPlayer(pitchingSeat == 3 ? GetBid(options.minBid, trump) : (int)PitchBid.NotPitching, string.Concat(Enumerable.Repeat("0U", hand.Length/2 - ((trick.Length/2 > 0) ? 1 : 0)))),
            };

            options.lowGoesToTaker = lowToTaker;
            var bot = new PitchBot(options, trump);
            var cardState = new TestCardState<PitchOptions>(bot, players, trick, trumpSuit: trump, trumpAnytime: options.playTrump != PitchPlayTrump.FollowSuit);
            var suggestion = bot.SuggestNextCard(cardState);

            Assert.AreEqual(expected, $"{suggestion}");
        }

        [TestMethod]
        public void PlayIfOthersFolded()
        {
            var baseOptions = tenPointOptions;
            var trump = Suit.Diamonds;
            var options = JsonConvert.DeserializeObject<PitchOptions>(JsonConvert.SerializeObject(baseOptions));
            var players = new[]
            {
                new TestPlayer((int)PitchBid.NotPitching, "3H3DTDHJ"),
                new TestPlayer((int)PitchBid.NotPitching, "0U0U0U0U") { Folded = true },
                new TestPlayer((int)PitchBid.NotPitching, "0U0U0U0U"),
                new TestPlayer(GetBid(6, trump), "0U0U0U"),
            };

            var bot = new PitchBot(options, trump);
            var cardState = new TestCardState<PitchOptions>(bot, players, "2D", trumpSuit: trump, trumpAnytime: options.playTrump != PitchPlayTrump.FollowSuit);
            var suggestion = bot.SuggestNextCard(cardState);

            Assert.AreEqual("3D", $"{suggestion}");
        }

        private static PitchOptions GetCallForBestOptions(int? callPartnerSeat = null)
        {
            return new PitchOptions
            {
                _callPartnerSeat = callPartnerSeat,
                gameOverScore = 32,
                isPartnership = false,
                minBid = 5,
                pitcherLeadsTrump = true,
                players = 5,
                playTrump = PitchPlayTrump.Only,
                tenOfTrumpReplacesGamePoint = true,
                variation = PitchVariation.TenPoint,
            };
        }

        private static int GetBid(int level, Suit suit)
        {
            return (int)PitchBid.Pitching + (10 * level) + (int)suit;
        }

        private static PitchBot GetBot(PitchVariation variation)
        {
            return GetBot(new PitchOptions() { variation = variation });
        }

        private static PitchBot GetBot(PitchOptions options)
        {
            return new PitchBot(options, Suit.Unknown);
        }

        private static int GetSuggestedBid(string handString, out Hand hand, PitchOptions options)
        {
            var players = new[]
            {
                new TestPlayer(seat: 0, hand: handString.Replace(" ", string.Empty)),
                new TestPlayer(seat: 1),
                new TestPlayer(seat: 2),
                new TestPlayer(seat: 3)
            };

            var maxBid = 4;
            switch (options.variation)
            {
                case PitchVariation.FourPoint:
                    maxBid = 4;
                    break;
                case PitchVariation.FivePoint:
                    maxBid = 5;
                    break;
                case PitchVariation.SixPoint:
                    maxBid = 6;
                    break;
                case PitchVariation.SevenPoint:
                    maxBid = 7;
                    break;
                case PitchVariation.TenPoint:
                    maxBid = 10;
                    break;
                case PitchVariation.ElevenPoint:
                    maxBid = 11;
                    break;
                case PitchVariation.ThirteenPoint:
                    maxBid = 13;
                    break;
                case PitchVariation.NinePoint:
                    maxBid = 9;
                    break;
            }

            var legalBids = new List<BidBase>();
            for (var v = options.minBid; v <= maxBid; ++v) legalBids.Add(new BidBase((int)PitchBid.Base + v));
            if (options.offerShootBid)
            {
                legalBids.Add(new BidBase((int)PitchBid.ShootMoonBid));
            }
            legalBids.Add(new BidBase(BidBase.Pass));

            hand = new Hand(players[0].Hand);
            var bidState = new SuggestBidState<PitchOptions>
            {
                dealerSeat = 3,
                player = players[0],
                players = players,
                legalBids = legalBids,
                hand = hand
            };

            return GetBot(options).SuggestBid(bidState).value;
        }
    }
}