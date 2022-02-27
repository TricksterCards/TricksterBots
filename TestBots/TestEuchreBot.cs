using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trickster.Bots;
using Trickster.cloud;

namespace TestBots
{
    [TestClass]
    public class TestEuchreBot
    {

        [TestMethod]
        public void TestAvoidBidAlone()
        {
            //  if partner has high jack, they can help make 5 by beating off jack
            Assert.AreEqual("♥", GetSuggestedBid("9HTHQHKHAH", "JH"), "Do not bid alone without either jack");
            //  higher chance partner can cover off-suit card(s) to help make 5 tricks
            Assert.AreEqual("♣", GetSuggestedBid("ADKD ACJSJC", "9C"), "Do not bid alone with only three trump");
            Assert.AreEqual("♦", GetSuggestedBid("KH KDADJHJD", "9D"), "Do not bid alone with non-boss off-suit");
            Assert.AreEqual("♥", GetSuggestedBid("9D KHAHJDJH", "9H"), "Do not bid alone with weak off-suit");
        }

        [TestMethod]
        public void TestTakeBidAlone()
        {
            //  risk of both same suit as Ace being led on first trick AND getting trumped is small
            Assert.AreEqual("♥ alone", GetSuggestedBid("AS KHAHJDJH", "9H"), "Should bid alone with sure trump and off-suit Ace");
            //  if partner doesn't have the off-Jack, you won't take all five tricks even together, may as well go for it
            Assert.AreEqual("♠ alone", GetSuggestedBid("AC QSKSASJS", "9S"), "Should bid alone with strength even if missing off-Jack");
        }

        [TestMethod]
        public void TestTakeBid()
        {
            Assert.AreEqual("♠", GetSuggestedBid("  9HTH KSASJC", "9S"), "Should bid when two-suited with three trump of reasonable strength");
            Assert.AreEqual("♣", GetSuggestedBid(" AD AH QCKCAC", "9C"), "Should bid three-suited missing both Jacks with strong off-suit");
            Assert.AreEqual("♣", GetSuggestedBid(" AD AH TCQCKC", "9C"), "Should bid three-suited missing both Jacks with strong off-suit");
            Assert.AreEqual("♦", GetSuggestedBid(" 9C 9S ADJHJD", "9D"), "Should bid with three sure tricks, regardless of other cards");
            Assert.AreEqual("♥", GetSuggestedBid("  9DTD QHKHAH", "9H"), "Should bid missing both Jacks with remaining high trump and two-suited");
            Assert.AreEqual("♠", GetSuggestedBid(" ACKC AH JCJS", "9S"), "Should bid with only Jacks with strong off-suit support");
            Assert.AreEqual("♦", GetSuggestedBid(" ASKS 9C JHJD", "9D"), "Should bid with only Jacks with mostly strong off-suit support");
            Assert.AreEqual("♣", GetSuggestedBid(" 9D 9H KCACJS", "TC"), "Should bid with weak off-suit and no high Jack if three strong trump");
            Assert.AreEqual("♦", GetSuggestedBid(" 9C 9S QDADJH", "TD"), "Should bid with weak off-suit and no high Jack if three strong trump");
            Assert.AreEqual("♥", GetSuggestedBid("AC 9D 9S JDJH", "TH"), "Should bid four-suited having both Jacks and an off-suit Ace");
            Assert.AreEqual("♠", GetSuggestedBid("  9DKD 9SKSJC", "TS"), "Should bid with three trump if two-suited");
            Assert.AreEqual("♣", GetSuggestedBid("  9HQH 9CQCJC", "AC"), "Should bid with three trump if two-suited");
            Assert.AreEqual("♦", GetSuggestedBid("  ASKS TDQDKD", "AD"), "Should bid with three weak trump if two-suited with high off-suit");
            Assert.AreEqual("♥", GetSuggestedBid("  TC 9HTHQHJH", "AH"), "Should bid with four trump, regardless of off-suit");
            Assert.AreEqual("♠", GetSuggestedBid("  9D 9STSQSKS", "AS"), "Should bid with four trump, regardless of off-suit");
        }

        [TestMethod]
        public void TestAvoidBid()
        {
            Assert.AreEqual("Pass", GetSuggestedBid(" 9C 9S 9HAHJD", "QH"), "Should pass if three-suited and weak, even with three trump");
            Assert.AreEqual("Pass", GetSuggestedBid("9C 9D 9H JCJS", "9S"), "Should pass if four-suited and weak, even with both Jacks");
            Assert.AreEqual("Pass", GetSuggestedBid("AC AD AH9H AS", "9S"), "Should pass if four-suited with only one trump, even if Ace");
            Assert.AreEqual("Pass", GetSuggestedBid("ADKD AS AH JS", "9C"), "Should pass if four-suited with only one trump, even if off-Jack");
            Assert.AreEqual("Pass", GetSuggestedBid("ACKC AD AS JH", "9H"), "Should pass if four-suited with only one trump, even if high Jack");
            Assert.AreEqual("Pass", GetSuggestedBid("  TSJS 9DQDKD", "TD"), "Should pass if two-suited with weak trump and off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid("  AHKH 9STSQS", "KS"), "Should pass if two-suited with very weak trump, even with high off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid(" AD AH 9CTCQC", "KC"), "Should pass if three-suited with very weak trump, even with high off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid(" AC 9S 9DTDQD", "KD"), "Should pass if three-suited with very weak trump and mixed off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid(" AC KS 9HTHQH", "KH"), "Should pass if three-suited with very weak trump and mixed off-suit");
            Assert.AreEqual("Pass", GetSuggestedBid(" 9D 9H 9STSQS", "KS"), "Should pass if three-suited with very weak trump and weak off-suit");
        }

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
