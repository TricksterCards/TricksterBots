using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestEuchreBot
    {

        [TestMethod]
        public void TestNoBidAloneWithoutJack()
        {
            //  do not bid alone without either jack
            //  if partner has high jack, they can help make 5 by beating off jack
            Assert.AreEqual("♥", GetSuggestedBid("9HTHQHKHAH", "JH"));
        }

        [TestMethod]
        public void TestBidAloneWithOffSuitAce()
        {
            //  bid alone with guaranteed trump and an off suit Ace
            Assert.AreEqual("♥ alone", GetSuggestedBid("AS KHAHJDJH", "9H"));
        }

        [TestMethod]
        public void TestBidAloneWithoutOffJack()
        {
            //  bid alone even when missing off jack with sufficient strength
            //  if partner doesn't have it, you won't take all five tricks even together
            Assert.AreEqual("♠ alone", GetSuggestedBid("AC QSKSASJS", "9S"));
        }

        [TestMethod]
        public void TestNoAloneWithThreeTrump()
        {
            //  do not bid alone with only three trump
            //  higher risk of opponents trumping an off-suit card
            //  higher chance partner can help make 5 tricks
            Assert.AreEqual("♣", GetSuggestedBid("ADKD ACJSJC", "9C"));
        }

        [TestMethod]
        public void TestNoAloneWithoutOffSuitAce()
        {
            //  do not bid alone with non-boss off-suit cards
            //  higher chance partner can cover off-suit card to help make 5 tricks
            Assert.AreEqual("♦", GetSuggestedBid("KH KDADJHJD", "9D"));
            Assert.AreEqual("♥", GetSuggestedBid("9D KHAHJDJH", "9H"));
        }

        [TestMethod]
        public void TestBidTwoSuited()
        {
            //  bid when two-suited with three trump of reasonable strength
            Assert.AreEqual("♠", GetSuggestedBid("9HTH KSASJC", "9S"));
        }

        // TODO:
        //    bid = "♣", hand = " AD AH QCKCAC", up = "9C" },
        //    bid = "♦", hand = " 9C 9S ADJHJD", up = "9D" },
        //    bid = "♥", hand = "  9DTD QHKHAH", up = "9H" },
        //    bid = "♠", hand = " ACKC AH JCJS", up = "9S" },
        //    bid = "♣", hand = " 9D 9H KCACJS", up = "TC" },
        //    bid = "♦", hand = " 9C 9S QDADJH", up = "TD" },
        //    bid = "♥", hand = "AC 9D 9S JDJH", up = "TH" },
        //    bid = "♠", hand = "  9DKD 9SKSJC", up = "TS" },
        //    bid = "♣", hand = "  9HQH 9CQCJC", up = "AC" },
        //    bid = "♦", hand = "  ASKS TDQDKD", up = "AD" },
        //    bid = "♥", hand = "  TC 9HTHQHJH", up = "AH" },
        //    bid = "♠", hand = "  9D 9STSQSKS", up = "AS" },
        //    bid = "♣", hand = " AD AH TCQCKC", up = "9C" },
        //    bid = "♦", hand = " ASKS 9C JHJD", up = "9D" },

        //    bid = "Pass", hand = " 9C 9S 9HAHJD", up = "QH" },
        //    bid = "Pass", hand = "9C 9D 9H JCJS", up = "9S" },
        //    bid = "Pass", hand = "ADKD AS AH JS", up = "9C" },
        //    bid = "Pass", hand = "  TSJS 9DQDKD", up = "TD" },
        //    bid = "Pass", hand = "ACKC AD AS JH", up = "9H" },
        //    bid = "Pass", hand = "AC AD AH9H AS", up = "9S" },
        //    bid = "Pass", hand = "  AHKH 9STSQS", up = "KS" },
        //    bid = "Pass", hand = " AD AH 9CTCQC", up = "KC" },
        //    bid = "Pass", hand = " AC 9S 9DTDQD", up = "KD" },
        //    bid = "Pass", hand = " AC KS 9HTHQH", up = "KH" },
        //    bid = "Pass", hand = " 9D 9H 9STSQS", up = "KS" }

        private static string GetBidText(BidBase bid)
        {
            if (bid.value == BidBase.Pass)
                return "Pass";

            if (bid.value > (int)EuchreBid.MakeAlone)
                return Card.SuitSymbol(bid.value - (int)EuchreBid.MakeAlone) + " alone";

            return Card.SuitSymbol(bid.value - (int)EuchreBid.Make);
        }

        /// <summary>
        /// Generate a suggested bid assuming default rules and first to bid to left of dealer.
        /// </summary>
        /// <param name="hand">First bidder's hand</param>
        /// <param name="upCardString">The card turned up by the dealer</param>
        /// <returns>The suggested bid for the first bidder</returns>
        private static string GetSuggestedBid(string hand, string upCardString)
        {
            var upCard = new Card(upCardString);
            var bot = new EuchreBot(new EuchreOptions(), Suit.Unknown);
            var bid = bot.SuggestBid(new Hand(hand.Replace(" ", "")), upCard, upCard.suit, false);
            return GetBidText(bid);
        }
    }
}
