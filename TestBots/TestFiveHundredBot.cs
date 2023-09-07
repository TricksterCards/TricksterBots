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
        [DataRow("9C", "6C",           "9C5C9S",           "JSTS6D", 1)] // Follow high if we know misere bidder is void
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
    }
}