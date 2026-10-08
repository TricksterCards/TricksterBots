using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class UnusualNotrump
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (!bid.bidIsDeclare)
                return false;

            if (bid.Index >= 2 && bid.History[bid.Index - 2].BidConvention == BidConvention.UnusualNotrump)
            {
                InterpretAdvance(bid.History[bid.Index - 2], bid);
                return true;
            }

            if (bid.Index >= 4 && bid.History[bid.Index - 4].BidConvention == BidConvention.UnusualNotrump &&
                IsPreference(bid.History[bid.Index - 4], bid.History[bid.Index - 2]))
            {
                InterpretRebid(bid.History[bid.Index - 2], bid);
                return true;
            }

            return false;
        }

        private static bool IsPreference(InterpretedBid overcall, InterpretedBid advance)
        {
            if (!advance.bidIsDeclare)
                return false;

            var suit = advance.declareBid.suit;
            return suit != Suit.Unknown && overcall.HandShape[suit].Min >= 5 && advance.declareBid.level == advance.LowestAvailableLevel(suit, true);
        }

        private static void InterpretAdvance(InterpretedBid overcall, InterpretedBid advance)
        {
            if (!IsPreference(overcall, advance))
                return;

            var suit = advance.declareBid.suit;
            var otherSuit = SuitRank.stdSuits.First(s => s != suit && overcall.HandShape[s].Min >= 5);
            advance.BidMessage = BidMessage.Signoff;
            advance.Points.Max = 10;
            advance.Description = $"Preference for {suit}";
            //  with equal length, prefer the cheaper suit
            advance.Validate = hand =>
            {
                var counts = BasicBidding.CountsBySuit(hand);
                return counts[suit] > counts[otherSuit] ||
                       counts[suit] == counts[otherSuit] && BridgeBot.suitRank[suit] < BridgeBot.suitRank[otherSuit];
            };
        }

        //  a weak Unusual 2NT passes partner's preference; the strong type bids again
        private static void InterpretRebid(InterpretedBid advance, InterpretedBid rebid)
        {
            var suit = advance.declareBid.suit;
            if (rebid.declareBid.suit != suit)
                return;

            if (rebid.declareBid.level == rebid.GameLevel)
            {
                rebid.Points.Min = 19;
                rebid.HandShape[suit].Min = 5;
                rebid.BidMessage = BidMessage.Signoff;
                rebid.Description = $"Strong two-suiter; game in {suit}";
            }
            else if (rebid.declareBid.level == rebid.LowestAvailableLevel(suit, true))
            {
                rebid.Points.Min = 16;
                rebid.Points.Max = 18;
                rebid.HandShape[suit].Min = 5;
                rebid.Description = $"Strong two-suiter; inviting game in {suit}";
            }
        }
    }
}
