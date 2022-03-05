using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestWhistBot
    {
        [TestMethod]
        public void DiscardJokersInNT()
        {
            var players = new[]
            {
                new TestPlayer(1561, "6DKDJDJH8S7H5DLJTDQSHJKS5SAHQHJCJSQC"),
                new TestPlayer(1400),
                new TestPlayer(1401),
                new TestPlayer(1400)
            };

            var bot = GetBot(new WhistOptions
                { variation = WhistVariation.BidWhist, bidderGetsKitty = true, bidderLeads = true });

            var discardState = new SuggestDiscardState<WhistOptions>
            {
                player = players[0],
                hand = new Hand(players[0].Hand)
            };

            var suggestion = bot.SuggestDiscard(discardState);
            Assert.AreEqual(6, suggestion.Count, "Discarded 6 cards");
            Assert.AreEqual(2, suggestion.Count(c => c.suit == Suit.Joker), $"Suggestion {Util.PrettyCards(suggestion)} contains both jokers");
        }

        [TestMethod]
        public void DontLeadTrumpWhenDefending()
        {
            var players = new[]
            {
                new TestPlayer(1400, "HJACKDQDAH"),
                new TestPlayer(1564),
                new TestPlayer(1400),
                new TestPlayer(1401)
            };

            var bot = GetBot(Suit.Clubs);
            var cardState = new TestCardState<WhistOptions>(bot, players);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(suggestion.suit != Suit.Clubs, "Suggested lead is not trump");
        }

        [TestMethod]
        public void SignalGoodSuitOnFirstSlough_Lead()
        {
            var players = new[]
            {
                new TestPlayer(1564, "HJ4D3DTH2S", cardsTaken: "2C3C4C5C6C7C8C9CTCJCQCLJ"),
                new TestPlayer(1400),
                new TestPlayer(1401),
                new TestPlayer(1400)
            };

            var bot = GetBot(Suit.Clubs);
            var cardState = new TestCardState<WhistOptions>(bot, players);
            Assert.IsTrue(new Hand(cardState.player.Hand).Any(c => bot.EffectiveSuit(c) == Suit.Clubs),
                $"Player's hand {Util.PrettyHand(cardState.player.Hand)} contains trump");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.IsTrue(bot.EffectiveSuit(suggestion) == Suit.Clubs, "Suggested lead is trump");
        }

        [TestMethod]
        public void SignalGoodSuitOnFirstSlough_LeadBack()
        {
            var players = new[]
            {
                new TestPlayer(1564, "4D3DTH2S", cardsTaken: "2C3C4C5C6C7C8C9CTCJCQCLJHJKCTDAC"),
                new TestPlayer(1400),
                new TestPlayer(1401) { GoodSuit = Suit.Diamonds },
                new TestPlayer(1400)
            };

            var bot = GetBot(Suit.Clubs);
            var cardState = new TestCardState<WhistOptions>(bot, players);
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("3D", suggestion.ToString(), $"Suggested {suggestion.StdNotation} is suit sloughed by partner");
        }

        [TestMethod]
        public void SignalGoodSuitOnFirstSlough_Slough()
        {
            var players = new[]
            {
                new TestPlayer(1401, "ADTD6H7S8S"),
                new TestPlayer(1400),
                new TestPlayer(1564, cardsTaken: "2C3C4C5C6C7C8C9CTCJCQCLJ"),
                new TestPlayer(1400)
            };

            var bot = GetBot(Suit.Clubs);
            var cardState = new TestCardState<WhistOptions>(bot, players, "HJKC");
            var suggestion = bot.SuggestNextCard(cardState);
            Assert.AreEqual("TD", suggestion.ToString(), $"Suggested {suggestion.StdNotation} is lowest card of best suit");
        }

        private static WhistBot GetBot(WhistOptions options)
        {
            return new WhistBot(options, Suit.Unknown);
        }

        private static WhistBot GetBot(Suit trumpSuit)
        {
            return GetBot(trumpSuit, new WhistOptions { variation = WhistVariation.BidWhist });
        }

        private static WhistBot GetBot(Suit trumpSuit, WhistOptions options)
        {
            return new WhistBot(options, trumpSuit);
        }
    }
}