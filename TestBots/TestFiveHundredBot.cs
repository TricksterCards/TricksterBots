using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestFiveHundredBot
    {
        private static readonly Suit defaultTrump = Suit.Diamonds;

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

        private static FiveHundredBot GetBot(Suit trumpSuit, FiveHundredOptions options)
        {
            return new FiveHundredBot(options, trumpSuit);
        }
    }
}