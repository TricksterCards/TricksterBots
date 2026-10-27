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

            if (overcall.bidIsDeclare && overcall.declareBid.suit == Suit.Unknown &&
                advance.bidIsDeclare && advance.declareBid.suit != Suit.Unknown && advance.BidConvention == BidConvention.None &&
                rebid.History[rebid.Index - 1].bid == BidBase.Pass)
            {
                RebidAfterNaturalAdvanceOfNotrump(overcall, advance, rebid);
                return;
            }

            AcolOvercallRebid.Interpret(rebid);
        }

        //  (1C)-1N-(P)-2D/3H/3D/4H: advancer's natural suit bids after a notrump overcall (see Advance)
        private static void RebidAfterNaturalAdvanceOfNotrump(InterpretedBid overcall, InterpretedBid advance, InterpretedBid rebid)
        {
            var suit = advance.declareBid.suit;
            if (advance.BidMessage == BidMessage.Signoff)
            {
                if (rebid.bid == BidBase.Pass)
                    rebid.Description = "Accept sign-off";
                return;
            }

            if (advance.BidMessage == BidMessage.Forcing)
            {
                //  choose between 3NT and 4 of advancer's major
                if (!rebid.bidIsDeclare)
                    return;

                if (rebid.declareBid.suit == suit && rebid.declareBid.level == rebid.GameLevel)
                {
                    rebid.BidMessage = BidMessage.Signoff;
                    rebid.HandShape[suit].Min = 3;
                    rebid.Description = $"Sign-off at game; 3+ {suit}";
                }
                else if (rebid.declareBid.suit == Suit.Unknown && rebid.declareBid.level == 3)
                {
                    rebid.BidMessage = BidMessage.Signoff;
                    rebid.HandShape[suit].Max = 2;
                    rebid.Description = $"Sign-off at game; 0-2 {suit}";
                }

                return;
            }

            //  advancer invited 3NT with a good 6-card minor
            if (rebid.bid == BidBase.Pass)
            {
                rebid.BidPointType = BidPointType.Hcp;
                rebid.Points.Max = (overcall.Points.Min + overcall.Points.Max) / 2;
                rebid.Description = "Minimum";
            }
            else if (rebid.bidIsDeclare && rebid.declareBid.suit == Suit.Unknown && rebid.declareBid.level == 3)
            {
                rebid.BidPointType = BidPointType.Hcp;
                rebid.Points.Min = (overcall.Points.Min + overcall.Points.Max) / 2 + 1;
                rebid.BidMessage = BidMessage.Signoff;
                rebid.Description = "Accept invitation";
            }
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
