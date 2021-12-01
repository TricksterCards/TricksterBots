using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class ControlBid
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.Index < 2 || !bid.bidIsDeclare || bid.declareBid.suit == Suit.Unknown || bid.declareBid.level < 4)
                return false;

            if (bid.declareBid.level > 4 && bid.declareBid.level > bid.LowestAvailableLevel(bid.declareBid.suit))
                return false;

            var partnerBid = bid.History[bid.Index - 2];
            if (!partnerBid.bidIsDeclare || partnerBid.declareBid.suit == Suit.Unknown)
                return false;

            var summary = new InterpretedBid.TeamSummary(bid.History, bid.Index - 2);
            var suit = summary.HandShape.Where(hs => hs.Value.Min >= 8).Select(hs => hs.Key).FirstOrDefault();
            if (suit == Suit.Unknown)
                return false;

            if (partnerBid.BidConvention != BidConvention.ControlBid && bid.declareBid.suit == suit)
                return false;

            if (partnerBid.declareBid.suit == bid.declareBid.suit)
                return false;

            ShowControls(suit, partnerBid, bid);
            return true;
        }

        private static bool HasFirstRoundControl(Hand hand, Suit suit)
        {
            return hand.Any(c => c.suit == suit && c.rank == Rank.Ace) || BasicBidding.CountsBySuit(hand)[suit] == 0;
        }

        private static bool HasSecondRoundControl(Hand hand, Suit suit)
        {
            return hand.Any(c => c.suit == suit && c.rank == Rank.King) || BasicBidding.CountsBySuit(hand)[suit] <= 1;
        }

        private static void ShowControls(Suit suit, InterpretedBid partnerBid, InterpretedBid bid)
        {
            bid.BidConvention = BidConvention.ControlBid;

            var start = BridgeBot.suitRank[partnerBid.declareBid.suit];
            var stop = BridgeBot.suitRank[bid.declareBid.suit];
            var skipped = stop > start
                ? SuitRank.stdSuits.Where(s => BridgeBot.suitRank[s] > start && BridgeBot.suitRank[s] < stop)
                : SuitRank.stdSuits.Where(s => BridgeBot.suitRank[s] < start && BridgeBot.suitRank[s] > stop);

            if (bid.declareBid.suit == suit)
            {
                //  attempt to sign-off
                bid.Description = "no further interest";
                bid.Validate = hand => true;
            }
            else if (bid.Index < 4 || bid.History[bid.Index - 4].BidConvention != BidConvention.ControlBid)
            {
                //  showing first-round controls (denying controls in skipped suits)
                bid.Description = $"Ace or void in {bid.declareBid.suit}";
                bid.AlternateMatches = hand => HasFirstRoundControl(hand, bid.declareBid.suit) && skipped.All(s => !HasFirstRoundControl(hand, s));
            }
            else
            {
                //  showing second-round controls (denying controls in skipped suits)
                bid.Description = $"King or singleton in {bid.declareBid.suit}";
                bid.AlternateMatches = hand => HasSecondRoundControl(hand, bid.declareBid.suit) && skipped.All(s => !HasSecondRoundControl(hand, s));
            }

            if (partnerBid.BidConvention != BidConvention.ControlBid)
            {
                //  TODO: remove to allow initiating a control bid sequence
                bid.AlternateMatches = null;
                bid.Validate = hand => false;
            }
        }
    }
}