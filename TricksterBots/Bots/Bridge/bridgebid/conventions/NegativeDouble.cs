using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class NegativeDouble
    {
        public static bool CanUseAfter(InterpretedBid opening, InterpretedBid overcall)
        {
            //  the negative double is used through 2S over a suited overcall to show support for unbid major(s)
            if (!overcall.bidIsDeclare || overcall.declareBid.suit == Suit.Unknown || overcall.declareBid.level > 2)
                return false;

            //  if we're in-range, make sure we have at least one unbid major
            return DetermineUnbidMajors(opening, overcall).Length > 0;
        }

        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.BidPhase == BidPhase.Response && bid.bid == BridgeBid.Double)
                //  we responded with a double - check if it is a negative double and interpret appropriately
                return Response(bid.History[bid.Index - 2], bid.History[bid.Index - 1], bid);
            if (bid.Index >= 4 && bid.History[bid.Index - 4].BidConvention == BidConvention.NegativeDouble)
            {
                var overcall = bid.History[bid.Index - 5];
                var openerRebid = bid.History[bid.Index - 2];
                if (openerRebid.bidIsDeclare) return ResponderRebid(overcall, openerRebid, bid);
            }

            return false;
        }

        private static Suit[] DetermineUnbidMajors(InterpretedBid opening, InterpretedBid overcall)
        {
            var bidSuits = new[] { opening.declareBid.suit, overcall.declareBid.suit };
            return SuitRank.stdSuits.Where(BridgeBot.IsMajor).Where(s => !bidSuits.Contains(s)).ToArray();
        }

        //  TODO: OpenerRebid

        private static bool ResponderRebid(InterpretedBid overcall, InterpretedBid openerRebid, InterpretedBid rebid)
        {
            //  TODO
            return false;
        }

        private static bool Response(InterpretedBid opening, InterpretedBid overcall, InterpretedBid response)
        {
            if (!CanUseAfter(opening, overcall))
                return false;

            //  we have a negative double
            var unbidMajors = DetermineUnbidMajors(opening, overcall);
            response.Points.Min = overcall.declareBid.level == 1 ? 6 : overcall.declareBid.level == 2 ? 8 : 10;
            response.BidPointType = BidPointType.Hcp;
            response.BidConvention = BidConvention.NegativeDouble;
            response.BidMessage = BidMessage.Forcing;
            foreach (var s in unbidMajors) response.HandShape[s].Min = 4;
            if (unbidMajors.Length == 1 && unbidMajors[0] == Suit.Spades) response.HandShape[Suit.Spades].Max = 4;
            response.Description = unbidMajors.Length == 1 ? $"4 {unbidMajors[0]}" : "4-4 or better in Hearts & Spades";

            return true;
        }
    }
}