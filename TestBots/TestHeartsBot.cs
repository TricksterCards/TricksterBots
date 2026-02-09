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


        [TestMethod]
        [DataRow("8S",   "KSTS", "QS8S", DisplayName = "Don't play QS if partner taking trick with higher spade")]
        [DataRow("AS",   "KSTS", "QSAS", DisplayName = "Don't play QS if partner taking trick with higher spade, even holding AS")]
        [DataRow("8S", "4SKSTS", "QS8S", DisplayName = "Don't play QS if last to play and partner taking trick")]
        public void PlayOfHandAfterFirstTrick(string card, string trick, string hand)
        {
            var players = new[]
            {
                new TestPlayer(hand: hand, cardsTaken: "2CACKCQC"),
                new TestPlayer(),
                new TestPlayer(),
                new TestPlayer()
            };

            var options = new HeartsOptions
            {
                isPartnership = true
            };

            var bot = GetBot(options);
            var cardState = new TestCardState<HeartsOptions>(bot, players, trick);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual(card, suggestion.ToString());
        }

        [TestMethod]
        public void DuckWhenQueenSpadesIsSloughedOnDiamondTrick()
        {
            // Scenario: Diamond trick, QS was sloughed by another player
            // Bot plays last and has lower diamonds - should duck to avoid taking QS
            var players = new[]
            {
                new TestPlayer(hand: "4D5D6DAH", cardsTaken: "2C3C4C5C"),
                new TestPlayer(),
                new TestPlayer(),
                new TestPlayer()
            };

            var bot = GetBot();
            // Trick: AD (led), QS (sloughed), KD (taking)
            // Bot has 4D, 5D, 6D - all are below KD, so bot can duck
            var cardState = new TestCardState<HeartsOptions>(bot, players, "ADQSKD");
            var suggestion = bot.SuggestNextCard(cardState);
            
            // Bot should play a low diamond (4D, 5D, or 6D) to duck, not try to take
            Assert.IsTrue(suggestion.suit == Suit.Diamonds, $"Suggested {suggestion.StdNotation}; expected a diamond");
            Assert.IsTrue(suggestion.rank < Rank.King, $"Suggested {suggestion.StdNotation}; expected to duck below King");
        }

        [TestMethod]
        public void DuckQueenSpadesOnDiamondTrickWhenPossible()
        {
            // Issue scenario: Diamond trick, QS sloughed, bot plays last with lower diamonds
            // Bot CAN duck but may not be doing so
            var players = new[]
            {
                new TestPlayer(hand: "3D5D7D9DAH", cardsTaken: "2C3C4C5C"),
                new TestPlayer(),
                new TestPlayer(),
                new TestPlayer()
            };

            var bot = GetBot();
            // Trick: 4D (led), QS (sloughed), 8D (currently taking)
            // Bot has 3D, 5D, 7D, 9D
            // Bot can play 3D, 5D, or 7D to duck (avoid taking QS)
            // Or play 9D to take the trick with QS
            var cardState = new TestCardState<HeartsOptions>(bot, players, "4DQS8D");
            var suggestion = bot.SuggestNextCard(cardState);
            
            // Bot MUST duck - should NOT play 9D
            Assert.IsTrue(suggestion.rank < Rank.Nine, 
                $"Suggested {suggestion.StdNotation}; expected to duck (3D, 5D, or 7D) to avoid taking Queen of Spades");
        }

        [TestMethod]
        public void AvoidTakingQueenSpadesWhenCantDuckBelowWinner()
        {
            // Scenario: Diamond trick, QS was sloughed, bot plays last
            // Bot has only high diamonds (can't get below 9D) but SHOULD still duck
            // to avoid taking the QS - playing lower diamond is better than taking trick
            var players = new[]
            {
                new TestPlayer(hand: "JDQDKDAH", cardsTaken: "2C3C4C5C"),
                new TestPlayer(),
                new TestPlayer(),
                new TestPlayer()
            };

            var bot = GetBot();
            // Trick: 3D (led), QS (sloughed), 9D (taking)
            // Bot has JD, QD, KD - all are above 9D so can't duck below winner
            // But bot should still play lowest diamond (JD) instead of highest
            var cardState = new TestCardState<HeartsOptions>(bot, players, "3DQS9D");
            var suggestion = bot.SuggestNextCard(cardState);
            
            // Bot will take the trick, but should play JD (lowest) not KD (highest)
            // to minimize losing a high diamond unnecessarily
            Assert.AreEqual("JD", suggestion.ToString(), 
                $"Suggested {suggestion.StdNotation}; expected JD (lowest diamond) to minimize damage when forced to take QS");
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