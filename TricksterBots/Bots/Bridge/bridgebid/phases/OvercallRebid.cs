using Trickster.cloud;

namespace Trickster.Bots
{
    //  Overcaller's second call (SAYC and Acol share the natural-overcall continuations)
    internal static class OvercallRebid
    {
        public static void Interpret(InterpretedBid rebid)
        {
            var overcall = rebid.History[rebid.Index - 4];
            var advance = rebid.History[rebid.Index - 2];

            //  conventional overcalls (Michaels, Unusual 2NT, Cappelletti, takeout doubles) define their own continuations
            if (overcall.BidConvention != BidConvention.None)
                return;

            if (rebid.bid == BridgeBid.Double && IsCooperativeDoubleAfterNotrump(overcall, advance, rebid))
            {
                //  (1S)-1N-(2S)-P-(P)-X: the opponents found a fit, so double asks partner to compete
                var theirSuit = rebid.History[rebid.Index - 1].bidIsDeclare ? rebid.History[rebid.Index - 1].declareBid.suit : rebid.History[rebid.Index - 3].declareBid.suit;
                rebid.BidConvention = BidConvention.TakeoutDouble;
                rebid.HandShape[theirSuit].Max = 2;
                rebid.Description = "Takeout; asking partner to compete";
                return;
            }

            if (advance.BidConvention == BidConvention.Cuebid && overcall.bidIsDeclare && advance.bidIsDeclare)
            {
                RebidAfterCuebid(overcall, rebid);
                return;
            }

            AcolOvercallRebid.Interpret(rebid);
        }

        private static bool IsCooperativeDoubleAfterNotrump(InterpretedBid overcall, InterpretedBid advance, InterpretedBid rebid)
        {
            if (!overcall.bidIsDeclare || overcall.declareBid.suit != Suit.Unknown || advance.bid != BidBase.Pass)
                return false;

            var last = rebid.History[rebid.Index - 1].bidIsDeclare ? rebid.History[rebid.Index - 1] : rebid.History[rebid.Index - 3];
            return last.bidIsDeclare && last.declareBid.suit != Suit.Unknown && last.declareBid.level <= 3;
        }

        //  advancer's cuebid shows a good raise (or a strong hand); overcaller describes his strength
        private static void RebidAfterCuebid(InterpretedBid overcall, InterpretedBid rebid)
        {
            if (!rebid.bidIsDeclare)
                return;

            var suit = rebid.declareBid.suit;
            var ourSuit = overcall.declareBid.suit;
            if (suit != ourSuit || suit == Suit.Unknown)
                return;

            var lowestAvailableLevel = rebid.LowestAvailableLevel(suit, true);
            if (rebid.declareBid.level == lowestAvailableLevel && lowestAvailableLevel < rebid.GameLevel)
            {
                rebid.Points.Max = 14;
                rebid.HandShape[suit].Min = overcall.HandShape[suit].Min;
                rebid.Description = $"Minimum overcall; {overcall.HandShape[suit].Min}+ {suit}";
            }
            else if (rebid.declareBid.level == rebid.GameLevel)
            {
                rebid.Points.Min = 15;
                rebid.HandShape[suit].Min = overcall.HandShape[suit].Min;
                rebid.BidMessage = BidMessage.Signoff;
                rebid.Description = "Sign-off at game";
            }
        }
    }
}
