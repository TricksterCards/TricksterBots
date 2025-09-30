using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestPinochleBot
    {
        private static readonly PinochleOptions singleDeckOptions = new PinochleOptions
        {
            passCount = 4,
        };

        /// <summary>
        /// Declarer's partner should pass parts of a Pinochle when one of the
        /// suits in a Pinochle is trump (Diamonds or Spades)
        /// and not otherwise (Clubs and Hearts).
        /// </summary>
        [TestMethod]
        [DataRow(Suit.Clubs,    "QDJDTDTD9DKSQSJSJSAHAHKH", "AHAH9DQD")]
        [DataRow(Suit.Diamonds, "JDQSJSAHAHKHACACKCKCQCQC", "JDQSACAC")]
        [DataRow(Suit.Hearts  , "KDJDTDTDQSJSKCKCQCQCJCJC", "JCJCQCQC")]
        [DataRow(Suit.Spades  , "QSJSJDAHAHKHACACKCKCQCQC", "QSJSJDAC")]
        public void ShouldPassPinochlePartsToDeclarer(Suit trump, string hand, string expected)
        {
            var bot = GetBot(trump, singleDeckOptions);
            var player = new PlayerBase
            {
                Bid = PinochleBid.DeclarerPartnerBid,
                Hand = hand,
            };
            var passState = new SuggestPassState<PinochleOptions>
            {
                passCount = singleDeckOptions.passCount,
                player = player,
                hand = new Hand(player.Hand),
            };
            passState.SortCardMembers();
            var actual = string.Join("", bot.SuggestPass(passState));
            Assert.AreEqual(expected, actual);
        }

        private static PinochleBot GetBot(Suit trumpSuit, PinochleOptions options)
        {
            return new PinochleBot(options, trumpSuit);
        }
    }
}