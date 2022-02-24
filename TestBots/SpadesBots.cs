using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class SpadesBots
    {
        private const string passingPlayerHand = "ASKSQSJSAD9D8D3DAH2HAC3C2C";

        [TestMethod]
        public void TestPassNilBid()
        {
            //  assert we are passing high cards for nil bid
            Assert.AreEqual("ASKSQSAH", GetSuggestedPass(0));
        }

        [TestMethod]
        public void TestPassNonNilBid()
        {
            //  assert we are passing low card for non-nil bid
            Assert.AreEqual("2C2H3D3C", GetSuggestedPass(3));
        }

        private static string GetSuggestedPass(int bid)
        {
            var passState = new SuggestPassState<SpadesOptions>
            {
                options = new SpadesOptions { nilPass = 4 },
                player = new PlayerBase { Hand = passingPlayerHand, Bid = bid },
                trumpSuit = Suit.Spades,
                hand = new Hand(passingPlayerHand),
                passCount = 4
            };

            var bot = new SpadesBot(passState.options, passState.trumpSuit);
            var cards = bot.SuggestPass(passState);
            return new Hand(cards).ToString();
        }
    }
}