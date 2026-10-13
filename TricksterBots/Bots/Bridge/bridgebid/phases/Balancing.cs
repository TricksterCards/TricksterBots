using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    //  (1X)-P-(2X)-P-(P)-? and similar: our side has only passed and the opponents are about to play a low-level contract,
        //  so compete in the pass-out seat with a takeout double or a 5+ card suit (or double directly over their fit) (SAYC)
    internal static class Balancing
    {
        public static bool Applies(InterpretedBid bid)
        {
            if (bid.Index < 4 || bid.Options.bidding == BridgeBiddingScheme.Acol)
                return false;

            //  pass-out seat, or a double directly over the opponents' low-level fit
            var isDirect = IsDirect(bid);
            if (!isDirect && (bid.History[bid.Index - 1].bid != BidBase.Pass || bid.History[bid.Index - 2].bid != BidBase.Pass))
                return false;

            for (var i = bid.Index - 2; i >= 0; i -= 2)
                if (bid.History[i].bid != BidBase.Pass)
                    return false;

            var last = isDirect ? bid.History[bid.Index - 1] : bid.History[bid.Index - 3];
            if (!last.bidIsDeclare || last.declareBid.suit == Suit.Unknown || last.declareBid.level > 2)
                return false;

            //  only when the opponents have bid their suit twice (a fit or a rebid suit), not after e.g. a 1NT opening and a sign-off
            return bid.History.Take(last.Index).Any(b => (last.Index - b.Index) % 2 == 0 && b.bidIsDeclare && b.declareBid.suit == last.declareBid.suit);
        }

        private static bool IsDirect(InterpretedBid bid)
        {
            return bid.bid == BridgeBid.Double && bid.History[bid.Index - 1].bidIsDeclare;
        }

        public static void Interpret(InterpretedBid bid)
        {
            var theirSuits = bid.History.Take(bid.Index)
                .Where(b => (bid.Index - b.Index) % 2 == 1 && b.bidIsDeclare && b.declareBid.suit != Suit.Unknown)
                .Select(b => b.declareBid.suit).Distinct().ToList();

            if (bid.bid == BridgeBid.Double)
            {
                var isDirect = IsDirect(bid);
                bid.BidConvention = BidConvention.TakeoutDouble;
                bid.BidMessage = BidMessage.Forcing;
                bid.BidPointType = BidPointType.Dummy;
                bid.Points.Min = isDirect ? 12 : 8;
                foreach (var s in SuitRank.stdSuits)
                {
                    if (theirSuits.Contains(s))
                        bid.HandShape[s].Max = 2;
                    else
                        bid.HandShape[s].Min = 3;
                }

                bid.Description = isDirect ? "Takeout double" : "Balancing takeout double";
                return;
            }

            if (!bid.bidIsDeclare)
                return;

            var suit = bid.declareBid.suit;
            if (suit == Suit.Unknown || theirSuits.Contains(suit) || bid.declareBid.level != bid.LowestAvailableLevel(suit, true) ||
                bid.declareBid.level > 3)
                return;

            bid.Points.Min = 8;
            bid.Points.Max = 14;
            bid.HandShape[suit].Min = 5;
            bid.Description = $"Balancing; 5+ {suit}";
        }
    }
}
