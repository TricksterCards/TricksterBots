using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestFiveHundredBot
    {
        private static readonly Suit defaultTrump = Suit.Diamonds;

        private static readonly FiveHundredOptions defaultOptions = new FiveHundredOptions();

        private static readonly FiveHundredOptions threePlayerOptions = new FiveHundredOptions
        {
            isPartnership = false,
            players = 3
        };

        [TestMethod]
        [DataRow("Pass", "HJ5S4S5H4H5D4D6C5C4C", FiveHundredVariation.Australian,         null, DisplayName = "Don't bid 6NT with Joker and a weak hand in Australian")]
        [DataRow("Pass", "HJAS4S5H4H5D4D6C5C4C", FiveHundredVariation.Australian,         null, DisplayName = "Bid 6NT with Joker and a near weak hand in Australian")]
        [DataRow( "6NT", "HJASKSAH4H5D4D6C5C4C", FiveHundredVariation.Australian,         null, DisplayName = "Bid 6NT with Joker and a medium hand in Australian")]
        [DataRow( "iNT", "HJ5S4S5H4H5D4D6C5C4C", FiveHundredVariation.American,           null, DisplayName = "Bid iNT in American")]
        [DataRow( "6NT", "HJJSJCASKSQSTS9S8S7S", FiveHundredVariation.Australian,         null, DisplayName = "Bid 6NT with Joker and a strong hand in Australian")]
        [DataRow("Pass", "ASKSQSAHKHQHACKCADKD", FiveHundredVariation.Australian,         null, DisplayName = "Don't bid any NT without Joker in Australian (if partner hasn't bid)")]
        [DataRow("Pass", "ASKSQSAHKHQHACKCADKD", FiveHundredVariation.Australian,  Suit.Spades, DisplayName = "Don't bid any NT without Joker in Australian (if partner didn't bid NT)")]
        [DataRow( "7NT", "ASKSQSAHKHQHACKCADKD", FiveHundredVariation.Australian, Suit.Unknown, DisplayName = "Bid 7NT without Joker if strong in NT and partner bid 6NT")]
        [DataRow(  "9♠", "HJJSJCASKSQSTS9S7S4D", FiveHundredVariation.American,           null, DisplayName = "Bid natural instead of iNT in American with Joker")]
        public void Bid6NtWithJoker(string bid, string hand, FiveHundredVariation variation, Suit? partnerBidSuit)
        {
            var partnerBid = !partnerBidSuit.HasValue ? BidBase.NoBid : new FiveHundredBid(partnerBidSuit.Value, 6);
            var options = new FiveHundredOptions
            {
                variation = variation,
                whenNullo = FiveHundredWhenNullo.Off,
            };
            var players = new[]
            {
                new TestPlayer(hand: hand, seat: 0),
                new TestPlayer(hand: "0U0U0U0U0U0U0U0U0U0U", seat: 1),
                new TestPlayer(hand: "0U0U0U0U0U0U0U0U0U0U", seat: 2, bid: partnerBid),
                new TestPlayer(hand: "0U0U0U0U0U0U0U0U0U0U", seat: 3, bid: BidBase.Pass)
            };
            var bot = GetBot(Suit.Unknown, options);
            var bidState = new SuggestBidState<FiveHundredOptions>
            {
                dealerSeat = 3,
                hand = new Hand(players[0].Hand),
                legalBids = GetLegalBids(variation, partnerBidSuit.HasValue ? 7 : 6),
                options = options,
                player = players[0],
                players = players
            };
            var suggestion = bot.SuggestBid(bidState);
            Assert.AreEqual(bid, suggestion.value == BidBase.Pass ? "Pass" : new FiveHundredBid(suggestion.value).ToString());
        }

        [TestMethod]
        public void SoloDucksIfEffectivePartnerTakingTrick()
        {
            var players = new[]
            {
                new TestPlayer(FiveHundredBid.NotContractorBid, "HJKD3DTH2S"),
                new TestPlayer(new FiveHundredBid(defaultTrump, 7), gameScore: 100),
                new TestPlayer(FiveHundredBid.NotContractorBid)
            };
            var bot = GetBot(defaultTrump, threePlayerOptions);
            var cardState = new TestCardState<FiveHundredOptions>(
                bot,
                players,
                trick: "8DTD"
            );
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("3D", suggestion.ToString(), "Avoids taking trick because leader is already losing trick");
        }

        [TestMethod]
        public void SoloTakesIfEffectivePartnerNotTakingTrick()
        {
            var players = new[]
            {
                new TestPlayer(FiveHundredBid.NotContractorBid, "HJKD3DTH2S"),
                new TestPlayer(new FiveHundredBid(defaultTrump, 7), gameScore: 100),
                new TestPlayer(FiveHundredBid.NotContractorBid)
            };
            var bot = GetBot(defaultTrump, threePlayerOptions);
            var cardState = new TestCardState<FiveHundredOptions>(
                bot,
                players,
                trick: "TD8D"
            );
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("KD", suggestion.ToString(), "Takes trick because leader is taking trick");
        }

        [TestMethod]
        public void SoloTakesIfNoEffectivePartner()
        {
            var players = new[]
            {
                new TestPlayer(FiveHundredBid.NotContractorBid, "HJKD3DTH2S"),
                new TestPlayer(new FiveHundredBid(defaultTrump, 7)),
                new TestPlayer(FiveHundredBid.NotContractorBid, gameScore: 100)
            };
            var bot = GetBot(defaultTrump, threePlayerOptions);
            var cardState = new TestCardState<FiveHundredOptions>(
                bot,
                players,
                trick: "8DTD"
            );
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("KD", suggestion.ToString(), "Takes trick because declarer is losing");
        }

        [TestMethod]
        public void PlayHighIn3rdIfMisereIsUnder()
        {
            var players = new[]
            {
                new TestPlayer(FiveHundredBid.NotContractorBid, "ACKD3DTH9H8H7S4S3S2S"),
                new TestPlayer(FiveHundredBid.Misere250Before8SBid, "0?0?0?0?0?0?0?0?0?"),
                new TestPlayer(FiveHundredBid.NotContractorBid, "0?0?0?0?0?0?0?0?0?"),
                new TestPlayer(BidBase.NotPlaying, "0?0?0?0?0?0?0?0?0?"),
            };
            var bot = GetBot(Suit.Unknown, defaultOptions);
            var cardState = new TestCardState<FiveHundredOptions>(
                bot,
                players,
                trick: "8DTD"
            );
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("KD", $"{suggestion}");
        }

        [TestMethod]
        public void PlayUnderMisereIfPossible()
        {
            var players = new[]
            {
                new TestPlayer(FiveHundredBid.NotContractorBid, "ACKD3DTH9H8H7S4S3S2S"),
                new TestPlayer(FiveHundredBid.Misere250Before8SBid, "0?0?0?0?0?0?0?0?0?"),
                new TestPlayer(FiveHundredBid.NotContractorBid, "0?0?0?0?0?0?0?0?0?"),
                new TestPlayer(BidBase.NotPlaying, "0?0?0?0?0?0?0?0?0?"),
            };
            var bot = GetBot(Suit.Unknown, defaultOptions);
            var cardState = new TestCardState<FiveHundredOptions>(
                bot,
                players,
                trick: "TD8D"
            );
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("3D", $"{suggestion}");
        }

        [TestMethod]
        public void PlayUnderMisereIfPossibleWithJoker()
        {
            var players = new[]
            {
                new TestPlayer(FiveHundredBid.NotContractorBid, "HJKC3CTH9H8H7S4S3S2S"),
                new TestPlayer(FiveHundredBid.Misere250Before8SBid, "0?0?0?0?0?0?0?0?0?0?"),
                new TestPlayer(FiveHundredBid.NotContractorBid, "0?0?0?0?0?0?0?0?0?"),
                new TestPlayer(BidBase.NotPlaying, "0?0?0?0?0?0?0?0?0?0?"),
            };
            var bot = GetBot(Suit.Unknown, defaultOptions);
            var cardState = new TestCardState<FiveHundredOptions>(
                bot,
                players,
                trick: "8D"
            );
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("KC", $"{suggestion}");
        }

        [TestMethod]
        [DataRow("7S",   "", "ADKDQD8C7C6CHJ7S", "JD9D8D6D8HJSTS5S", 3)] // Don't lead suit where misere bidder is void
        [DataRow("6C",   "",       "ADKD8C7C6C",       "JSTS9D8D6D", 3)] // Don't lead boss until no other choice (at which point we claim)
        [DataRow("9C", "6C",           "9C5C9S",           "JSTS6D", 1)] // Follow high if we know misere bidder is void in led suit
        [DataRow("5C", "6C",           "9C5C9S",           "JCTC6D", 1)] // Follow low if misere bidder still has led suit
        public void SetOpenMisere(string expectedCard, string trick, string hand, string misereHand, int misereSeat)
        {
            var otherHandLength = trick.Length > 0 ? hand.Length / 2 - 1 : hand.Length / 2;
            var misereBid = FiveHundredBid.OpenMisereBidByPoints[FiveHundredOpenNulloPoints.FiveHundred];
            var players = new[]
            {
                new TestPlayer(FiveHundredBid.NotContractorBid, hand),
                new TestPlayer(misereSeat == 1 ? misereBid : BidBase.NotPlaying, misereSeat == 1 ? misereHand : "0?0?0?0?0?0?0?0?0?0?"),
                new TestPlayer(FiveHundredBid.NotContractorBid, string.Join("", Enumerable.Repeat("0?", otherHandLength))),
                new TestPlayer(misereSeat == 3 ? misereBid : BidBase.NotPlaying, misereSeat == 3 ? misereHand : "0?0?0?0?0?0?0?0?0?0?"),
            };
            var bot = GetBot(Suit.Unknown, defaultOptions);
            var cardState = new TestCardState<FiveHundredOptions>(
                bot,
                players,
                trick: trick
            );
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual(expectedCard, $"{suggestion}");
        }

        private static FiveHundredBot GetBot(Suit trumpSuit, FiveHundredOptions options)
        {
            return new FiveHundredBot(options, trumpSuit);
        }

        private static List<BidBase> GetLegalBids(FiveHundredVariation variation, int start = FiveHundredBid.MinTricks)
        {
            var bids = new List<BidBase>();

            for (var nTricks = start; nTricks <= FiveHundredBid.MaxTricks; ++nTricks)
            {
                var inkle = variation == FiveHundredVariation.American && nTricks == FiveHundredBid.MinTricks;
                bids.AddRange(FiveHundredBid.suitRank.OrderBy(sr => sr.Value)
                    .Select(sr => new BidBase(new FiveHundredBid(sr.Key, nTricks, inkle))));
            }

            bids.Add(new BidBase(BidBase.Pass));

            return bids;
        }
    }
}