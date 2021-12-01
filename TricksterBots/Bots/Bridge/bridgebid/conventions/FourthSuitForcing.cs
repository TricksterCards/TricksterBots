using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class FourthSuitForcing
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.BidPhase == BidPhase.ResponderRebid) return CheckResponderRebid(bid);
            if (bid.Index >= 2 && bid.History[bid.Index - 2].BidConvention == BidConvention.FourthSuitForcing)
            {
                OpenerRebidAfterFSF(bid);
                return true;
            }

            if (bid.Index >= 4 && bid.History[bid.Index - 4].BidConvention == BidConvention.FourthSuitForcing)
            {
                ResponderRebidAfterFSF(bid);
                return true;
            }

            return false;
        }

        private static bool CheckResponderRebid(InterpretedBid rebid)
        {
            //  FSF only occurs in a suit beyond the 1-level
            if (!rebid.bidIsDeclare || rebid.declareBid.suit == Suit.Unknown || rebid.declareBid.level == 1)
                return false;

            //  FSF is OFF if responder was a passed hand before partner opened
            if (rebid.History[rebid.Index % 4].bid == BidBase.Pass)
                return false;

            //  FSF is OFF if opponents intervene (except for a double)
            for (var i = rebid.Index - 1; i > 0; i -= 2)
                if (rebid.History[i].bidIsDeclare)
                    return false;

            //  TODO: FSF is OFF after a 2/1 response

            //  ensure this is the 4th suit
            var teamBids = rebid.History.Where(b => b.Index < rebid.Index && (rebid.Index - b.Index) % 2 == 0);
            var teamSuits = teamBids.Where(b => b.bidIsDeclare && b.BidConvention == BidConvention.None).Select(b => b.declareBid.suit);
            var bidSuits = SuitRank.stdSuits.Where(s => teamSuits.Contains(s)).ToList();
            if (bidSuits.Count < 3 || bidSuits.Contains(rebid.declareBid.suit))
                return false;

            //  TODO: determine when we should actually bid FSF
            rebid.Points.Min = 13;
            rebid.BidConvention = BidConvention.FourthSuitForcing;
            rebid.BidMessage = BidMessage.Forcing;
            rebid.Validate = hand => false;

            return true;
        }

        private static void OpenerRebidAfterFSF(InterpretedBid rebid)
        {
            if (!rebid.bidIsDeclare)
                return;

            var opening = rebid.History[rebid.Index - 8];
            var response = rebid.History[rebid.Index - 6];
            var openerRebid = rebid.History[rebid.Index - 4];
            var responderRebid = rebid.History[rebid.Index - 2];

            if (rebid.declareBid.suit == response.declareBid.suit && BridgeBot.IsMajor(response.declareBid.suit))
            {
                //  show delayed 3-card support for a major
                rebid.HandShape[rebid.declareBid.suit].Min = 3;
                rebid.Description = $"3 {rebid.declareBid.suit}";
            }
            else if (rebid.declareBid.suit == opening.declareBid.suit)
            {
                //  TODO: sources vary on exactly what opener rebidding his first suit means; clarify
                rebid.HandShape[rebid.declareBid.suit].Min = 5;
                rebid.Description = $"5 {rebid.declareBid.suit}";
            }
            else if (rebid.declareBid.suit == openerRebid.declareBid.suit)
            {
                //  TODO: sources vary on exactly what opener rebidding his second suit means; clarify
                rebid.HandShape[rebid.declareBid.suit].Min = 5;
                rebid.Description = $"5 {rebid.declareBid.suit}";
            }
            else if (rebid.declareBid.suit == responderRebid.declareBid.suit && rebid.declareBid.level <= 3)
            {
                rebid.HandShape[rebid.declareBid.suit].Min = 4;
                rebid.Description = $"4 {rebid.declareBid.suit}";
            }
            else if (rebid.declareBid.suit == Suit.Unknown && rebid.declareBid.level == rebid.LowestAvailableLevel(rebid.declareBid.suit))
            {
                rebid.HandShape[responderRebid.declareBid.suit].Min = 1;
                rebid.Description = $"Stopper in {responderRebid.declareBid.suit}";
                rebid.Validate = hand => BasicBidding.HasStopper(hand, responderRebid.declareBid.suit);
            }
        }

        private static void ResponderRebidAfterFSF(InterpretedBid rebid)
        {
            if (!rebid.bidIsDeclare)
                return;

            ResponderRebid.TryPlaceContract(rebid);
        }
    }
}