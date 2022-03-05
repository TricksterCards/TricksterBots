using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestHeartsBot
    {
        [TestMethod]
        public void DontSloughSpadesIfHoldingQS()
        {
            var players = new[]
            {
                new TestPlayer(hand: "ASKSQSJSTC5C"),
                new TestPlayer(),
                new TestPlayer(),
                new TestPlayer(cardsTaken: "2C3C4C6C")
            };

            var bot = GetBot(new HeartsOptions { qsAfterHearts = true });
            var cardState = new TestCardState<HeartsOptions>(bot, players, "9D8D7D", "QS");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(suggestion.suit != Suit.Spades, $"Suggested {suggestion.StdNotation}; expected non-spade");
        }

        [TestMethod]
        public void QueenSpadesAfterHearts()
        {
            var players = new[]
            {
                new TestPlayer(hand: "ASKSQSJSTSQC"),
                new TestPlayer(),
                new TestPlayer(),
                new TestPlayer(cardsTaken: "2C4C5C6C")
            };

            var bot = GetBot(new HeartsOptions { qsAfterHearts = true });
            var cardState = new TestCardState<HeartsOptions>(bot, players, "9D8D7D", "QS");
            Assert.AreEqual(cardState.legalCards.Count, new Hand(cardState.players[0].Hand).Count - 1, "All but one card is legal");
            Assert.IsFalse(cardState.legalCards.Contains(new Card("QS")), "Queen of Spades is not legal");

            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(suggestion.suit != Suit.Spades, $"Suggested {suggestion.StdNotation}; expected non-spade");
        }

        [TestMethod]
        public void TryToDrawOutQS()
        {
            var players = new[]
            {
                new TestPlayer(hand: "JSTSQC5C4C"),
                new TestPlayer(),
                new TestPlayer(),
                new TestPlayer()
            };

            var bot = GetBot();
            var cardState = new TestCardState<HeartsOptions>(bot, players);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("TS", suggestion.ToString(), $"Suggested {suggestion.StdNotation}; expected lowest spade (ten)");
        }

        private static HeartsBot GetBot()
        {
            return GetBot(new HeartsOptions());
        }

        private static HeartsBot GetBot(HeartsOptions options)
        {
            return new HeartsBot(options, Suit.Unknown);
        }
    }
}