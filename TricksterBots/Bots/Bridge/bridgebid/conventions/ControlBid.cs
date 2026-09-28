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

            var partnerBid = bid.History[bid.Index - 2];
            if (!partnerBid.bidIsDeclare || partnerBid.declareBid.suit == Suit.Unknown)
                return false;

            //  partner's suit bid answered an Ace ask, so it says nothing about controls
            if (partnerBid.BidConvention == BidConvention.AnswerBlackwood || partnerBid.BidConvention == BidConvention.AnswerGerber)
                return false;

            var summary = new InterpretedBid.TeamSummary(bid.History, bid.Index - 2);
            var suit = summary.HandShape.Where(hs => hs.Value.Min >= 8).Select(hs => hs.Key).FirstOrDefault();
            if (suit == Suit.Unknown)
                return false;

            //  a slam in the agreed suit ends the sequence; anything else must be the cheapest way to show a control
            var isSlamInAgreedSuit = bid.declareBid.suit == suit && bid.declareBid.level == 6;
            if (bid.declareBid.level > 4 && !isSlamInAgreedSuit && bid.declareBid.level > bid.LowestAvailableLevel(bid.declareBid.suit))
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

        private static bool ShouldInitiateControlBids(Hand hand, Suit trump, int partnerMinPoints)
        {
            //  we need enough combined strength to be interested in slam
            if (BasicBidding.ComputeHighCardPoints(hand) + BasicBidding.ComputeDummyPoints(hand) + partnerMinPoints < InterpretedBid.SmallSlamPoints - 2)
                return false;

            //  control bids (rather than an Ace ask) are what resolve a side suit where we could lose the first two tricks
            return SuitRank.stdSuits.Any(s => s != trump && !HasFirstRoundControl(hand, s) && !HasSecondRoundControl(hand, s));
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
                if (bid.declareBid.level == 6)
                {
                    //  bid the slam once the partnership has shown a control in every side suit
                    var partnerControls = bid.History.Where((b, i) => (bid.Index - i) % 4 == 2 && b.BidConvention == BidConvention.ControlBid && b.bidIsDeclare)
                        .Select(b => b.declareBid.suit).ToList();
                    bid.BidMessage = BidMessage.Signoff;
                    bid.Description = "Small slam";
                    bid.Priority = 10; // prefer bidding the slam we control bid toward
                    bid.Validate = hand => SuitRank.stdSuits.All(s =>
                        s == suit || partnerControls.Contains(s) || HasFirstRoundControl(hand, s) || HasSecondRoundControl(hand, s));
                }
                else
                {
                    //  attempt to sign-off
                    bid.Description = "no further interest";
                    bid.Validate = hand => true;
                }
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
                //  initiating the sequence requires slam interest, so points can't be bypassed by an alternate match
                var showsControl = bid.AlternateMatches;
                var partnerSummary = new InterpretedBid.PlayerSummary(bid.History, bid.Index - 2);
                bid.AlternateMatches = null;
                bid.Priority = 10; // prefer control bidding over settling for game when slam is still in reach
                bid.Validate = hand => showsControl != null && showsControl(hand) && ShouldInitiateControlBids(hand, suit, partnerSummary.Points.Min);
            }
        }
    }
}