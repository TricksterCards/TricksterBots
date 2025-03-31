using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text.RegularExpressions;
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
        [DataRow(  "6♦", "4D5D6D8D9CKC5H8HQHAH", FiveHundredVariation.Australian, 3,   0,    "",     "",     "",     "",  BidAfterPass.Never, DisplayName = "Keep bid minimal with weak trump")]
        [DataRow("Pass", "4D5D6D8D9CKC5H8HQHAH", FiveHundredVariation.Australian, 3,   0,    "", "Pass", "Pass",   "6S",  BidAfterPass.Never, DisplayName = "Keep bid minimal with weak trump (Pass)")]
        [DataRow(  "6♠", "AHKHTH7HKSQSJS8SKCTC", FiveHundredVariation.Australian, 3,   0,    "",     "",     "",     "",  BidAfterPass.Never, DisplayName = "Prefer picking suit with stronger trump")]
        [DataRow("Pass", "AHKHTH7HKSQSJS8SKCTC", FiveHundredVariation.Australian, 3,   0,  "6S", "Pass", "Pass", "Pass",  BidAfterPass.Never, DisplayName = "Prefer picking suit with stronger trump (at correct level)")]
        [DataRow(  "7♥", "HJJHQH4H6S9D8D8C7C6C", FiveHundredVariation.Australian, 3,   0, "6NT",     "",   "6H", "Pass",  BidAfterPass.Never, DisplayName = "Raise partner with support")]
        [DataRow("Pass", "KSJS7SJH9D6DQC5C4CHJ", FiveHundredVariation.Australian, 5,   0, "6NT", "Pass",   "8C", "Pass",  BidAfterPass.Never, DisplayName = "Don't raise past 8 if missing tricks")]
        [DataRow(  "8♦", "HJASJS7SQHKDQD9D7D4D", FiveHundredVariation.Australian, 3,   0, "6NT", "Pass",   "7D", "Pass",  BidAfterPass.Never, DisplayName = "Raise with a good fit with partner")]
        [DataRow( "10♦", "HJJDJHADKDQDTD9D7D4D", FiveHundredVariation.Australian, 3,   0, "6NT", "Pass",   "7D", "Pass",  BidAfterPass.Never, DisplayName = "Bid 10 if we have it")]
        [DataRow("Pass", "HJJDJHADKDQDTD9D7D4D", FiveHundredVariation.Australian, 3, 480, "6NT", "Pass",   "7D", "Pass",  BidAfterPass.Never, DisplayName = "Don't bid higher than needed to win")]
        [DataRow(  "9♦", "HJJDJHADKDQDTD9D7D4D", FiveHundredVariation.Australian, 3, 120, "6NT", "Pass",   "7D", "Pass",  BidAfterPass.Never, DisplayName = "Bid just high enough to win the game")]
        [DataRow(  "8♦", "HJJDJHADKDQDTD9D7D4D", FiveHundredVariation.Australian, 3, 480, "6NT",     "",   "7D", "Pass",  BidAfterPass.Never, DisplayName = "Keep bidding if opponents might overbid but no higher than necessary")]
        [DataRow("Pass", "HJJDJHADKDQDTD9D7D4D", FiveHundredVariation.Australian, 3, 480, "6NT",     "",   "7D", "Pass", BidAfterPass.Always, DisplayName = "Don't bid higher than partner if we can reenter bidding")]
        [DataRow(  "8♦", "HJJDJHADKDQDTD9D7D4D", FiveHundredVariation.Australian, 3, 480, "6NT", "Pass",   "7D",   "7H",  BidAfterPass.Never, DisplayName = "Overbid opponents but no higher than necessary")]
        [DataRow(  "8♦", "HJJDJHADKDQDTD9D7D4D", FiveHundredVariation.Australian, 3, 480, "6NT", "Pass",   "7D",   "7H", BidAfterPass.Always, DisplayName = "Overbid opponents even if we can reenter bidding")]
        [DataRow("Pass", "HJKSJSTS6H5HKC7CKDQD",   FiveHundredVariation.American, 5,   0, "6NT",     "", "Pass",   "8H",  BidAfterPass.Never, DisplayName = "Pass with insufficient strength to overbid opponents")]
        [DataRow( "7NT", "HJQSKH6HQC5C4CQD7D6D",   FiveHundredVariation.American, 5,   0, "6NT",     "",     "",     "",  BidAfterPass.Never, DisplayName = "Only count the Joker as stopper once in NT")]
        public void TestBidding(string bid, string hand, FiveHundredVariation variation, int kittySize, int score, string firstBidStr, string lhoBidStr, string partnerBidStr, string rhoBidStr, BidAfterPass bidAfterPass)
        {
            var firstBid = new FiveHundredBid(GetBid(firstBidStr));
            var lhoBid = new FiveHundredBid(GetBid(lhoBidStr));
            var partnerBid = new FiveHundredBid(GetBid(partnerBidStr));
            var rhoBid = new FiveHundredBid(GetBid(rhoBidStr));
            var options = new FiveHundredOptions
            {
                bidAfterPass = bidAfterPass,
                deckSize = 40 + kittySize,
                variation = variation,
                whenNullo = FiveHundredWhenNullo.Off,
            };
            var players = new[]
            {
                new TestPlayer(hand: hand, seat: 0, bid: firstBid, gameScore: score),
                new TestPlayer(hand: "0U0U0U0U0U0U0U0U0U0U", seat: 1, bid: lhoBid),
                new TestPlayer(hand: "0U0U0U0U0U0U0U0U0U0U", seat: 2, bid: partnerBid, gameScore: score),
                new TestPlayer(hand: "0U0U0U0U0U0U0U0U0U0U", seat: 3, bid: rhoBid)
            };
            
            foreach (var p in players.Where(p => p.Bid != BidBase.NoBid))
                p.BidHistory.Add(p.Bid);

            var bot = GetBot(Suit.Unknown, options);
            var bidState = new SuggestBidState<FiveHundredOptions>
            {
                dealerSeat = 3,
                hand = new Hand(players[0].Hand),
                legalBids = GetLegalBids(variation, rhoBid.IsContractor ? rhoBid : partnerBid.IsContractor ? partnerBid : lhoBid.IsContractor ? lhoBid : firstBid.IsContractor ? firstBid : new FiveHundredBid(BidBase.NoBid)),
                options = options,
                player = players[0],
                players = players
            };
            var suggestion = bot.SuggestBid(bidState);
            Assert.AreEqual(bid, suggestion.value == BidBase.Pass ? "Pass" : new FiveHundredBid(suggestion.value).ToString());
        }

        [TestMethod]
        [DataRow("Pass", "HJ5S4S5H4H5D4D6C5C4C", FiveHundredVariation.Australian,              null, DisplayName = "Don't bid 6NT with Joker and a weak hand in Australian")]
        [DataRow( "6NT", "HJAS4S5H4HKD4D6C5C4C", FiveHundredVariation.Australian,              null, DisplayName = "Bid 6NT with Joker and a near weak hand in Australian")]
        [DataRow("Pass", "HJAS4S5H4HKD4D6C5C4C", FiveHundredVariation.Australian,      BidBase.Pass, DisplayName = "Don't bid 6NT with Joker and a near weak hand in Australian if partner passed")]
        [DataRow( "6NT", "HJASKSAH4H5D4D6C5C4C", FiveHundredVariation.Australian,              null, DisplayName = "Bid 6NT with Joker and a medium hand in Australian")]
        [DataRow( "iNT", "HJ5S4S5H4H5D4D6C5C4C", FiveHundredVariation.American,                null, DisplayName = "Bid iNT in American")]
        [DataRow( "6NT", "HJJSJCASKSQSTS9S8S7S", FiveHundredVariation.Australian,              null, DisplayName = "Bid 6NT with Joker and a strong hand in Australian")]
        [DataRow("10NT", "HJASKSQSJSTS9S8S7S6S", FiveHundredVariation.American,   (int)Suit.Unknown, DisplayName = "Bid 10NT with Joker and a strong hand in American")]
        [DataRow( "10♠", "HJJSJCASKSQSTS9S8S7S", FiveHundredVariation.American,   (int)Suit.Unknown, DisplayName = "Bid 10♠ with Joker, a strong hand, and off-Jack in American")]
        [DataRow("Pass", "ASKSQSAHKHQHACKCADKD", FiveHundredVariation.Australian,              null, DisplayName = "Don't bid any NT without Joker in Australian (if partner hasn't bid)")]
        [DataRow("Pass", "ASKSQSAHKHQHACKCADKD", FiveHundredVariation.Australian,  (int)Suit.Spades, DisplayName = "Don't bid any NT without Joker in Australian (if partner didn't bid NT)")]
        [DataRow( "7NT", "ASKSQSAHKHQHACKCADKD", FiveHundredVariation.Australian, (int)Suit.Unknown, DisplayName = "Bid 7NT without Joker if strong in NT and partner bid 6NT")]
        [DataRow(  "9♠", "HJJSJCASKSQSTS9S7S4D", FiveHundredVariation.American,                null, DisplayName = "Bid natural instead of iNT in American with Joker")]
        public void Bid6NtWithJoker(string bid, string hand, FiveHundredVariation variation, int? partnerBidSuit)
        {
            var partnerBid = BidBase.NoBid;
            if (partnerBidSuit.HasValue)
                partnerBid = partnerBidSuit < 0 ? partnerBidSuit.Value : new FiveHundredBid((Suit)partnerBidSuit.Value, 6);
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
                legalBids = GetLegalBids(variation, partnerBidSuit > -1 ? 7 : 6),
                options = options,
                player = players[0],
                players = players
            };
            var suggestion = bot.SuggestBid(bidState);
            Assert.AreEqual(bid, suggestion.value == BidBase.Pass ? "Pass" : new FiveHundredBid(suggestion.value).ToString());
        }

        [TestMethod]
        public void TrumpBossEvenIfLhoIsVoid()
        {
            var options = new FiveHundredOptions
            {
                deckSize = 46,
                variation = FiveHundredVariation.American,
            };
            var players = new[]
            {
                new TestPlayer(new FiveHundredBid(GetBid("7NT")),   "HJKSQSTSJH9HKC"),
                new TestPlayer(FiveHundredBid.NotContractorBid,     "0?0?0?0?0?0?0?", cardsTaken: "ADLJKD9D") { VoidSuits = new List<Suit> { Suit.Diamonds }},
                new TestPlayer(FiveHundredBid.ContractorPartnerBid, "0?0?0?0?0?0?0?"),
                new TestPlayer(FiveHundredBid.NotContractorBid,     "0?0?0?0?0?0?", cardsTaken: "ACJC6C4C"),
            };
            var bot = GetBot(Suit.Unknown, options);
            var cardState = new TestCardState<FiveHundredOptions>(
                bot,
                players,
                trick: "QD"
            );
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("HJ", $"{suggestion}");
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

        private static int GetBid(string bid)
        {
            if (string.IsNullOrEmpty(bid))
                return BidBase.NoBid;

            if (bid == "Pass")
                return BidBase.Pass;

            var level = int.Parse(Regex.Match(bid, @"\d+").Value);
            var suitString = Regex.Match(bid, @"\D+").Value;
            var suitNames = Enum.GetNames(typeof(Suit));
            var suitValues = Enum.GetValues(typeof(Suit));
            var suitIndex = Array.FindIndex(suitNames, n => n.StartsWith(suitString));
            var suit = suitIndex == -1 ? Suit.Unknown : (Suit)suitValues.GetValue(suitIndex);

            return new FiveHundredBid(suit, level);
        }

        private static FiveHundredBot GetBot(Suit trumpSuit, FiveHundredOptions options)
        {
            return new FiveHundredBot(options, trumpSuit);
        }

        private static List<BidBase> GetLegalBids(FiveHundredVariation variation, FiveHundredBid afterBid)
        {
            if ((int)afterBid == BidBase.NoBid)
                return GetLegalBids(variation);

            if (afterBid.Suit == Suit.Unknown)
                return GetLegalBids(variation, afterBid.Tricks + 1);

            var nextSuitRank = FiveHundredBid.suitRank[afterBid.Suit] + 1;
            var nextSuit = FiveHundredBid.suitRank.FirstOrDefault(sr => sr.Value == nextSuitRank).Key;
            return GetLegalBids(variation, afterBid.Tricks, nextSuit);
        }

        private static List<BidBase> GetLegalBids(FiveHundredVariation variation, int start = FiveHundredBid.MinTricks, Suit startSuit = Suit.Spades)
        {
            var bids = new List<BidBase>();

            for (var nTricks = start; nTricks <= FiveHundredBid.MaxTricks; ++nTricks)
            {
                var inkle = variation == FiveHundredVariation.American && nTricks == FiveHundredBid.MinTricks;
                bids.AddRange(FiveHundredBid.suitRank.OrderBy(sr => sr.Value)
                    .Where(sr => nTricks > start || sr.Value >= FiveHundredBid.suitRank[startSuit])
                    .Select(sr => new BidBase(new FiveHundredBid(sr.Key, nTricks, inkle))));
            }

            bids.Add(new BidBase(BidBase.Pass));

            return bids;
        }
    }
}