using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    //  Doubling an opponent's artificial bid (Stayman, a transfer, a Blackwood answer) asks partner to lead that suit
    internal static class LeadDirectingDouble
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.bid != BridgeBid.Double || bid.Index < 1)
                return false;

            var artificial = bid.History[bid.Index - 1];
            if (!artificial.bidIsDeclare || artificial.declareBid.suit == Suit.Unknown)
                return false;

            var isAnswer = artificial.BidConvention == BidConvention.AnswerBlackwood || artificial.BidConvention == BidConvention.AnswerGerber;
            if (!isAnswer && artificial.BidConvention != BidConvention.Stayman && artificial.BidConvention != BidConvention.JacobyTransfer)
                return false;

            var suit = artificial.declareBid.suit;
            bid.Description = $"Lead-directing; asking for a {suit} lead";
            //  a good 5+ card suit, or (against slam) a void we can ruff
            bid.Validate = hand =>
            {
                var length = hand.Count(c => c.suit == suit);
                return length >= 5 && BasicBidding.IsGoodSuit(hand, suit, 5) || isAnswer && length == 0;
            };

            return true;
        }
    }
}
