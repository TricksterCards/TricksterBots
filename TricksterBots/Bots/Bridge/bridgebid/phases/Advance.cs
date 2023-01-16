using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class Advance
    {
        public static void Interpret(InterpretedBid advance)
        {
            var opening = advance.History.First(b => b.bid != BidBase.Pass);
            var overcall = advance.History[advance.Index - 2];
            //  TODO: var interference = advance.History[advance.Index - 1];

            if (advance.IsPass)
            {
                advance.Description = "No fit";
            }
            else if (advance.IsDouble)
            {
                //  TODO: InterpretDouble(overcall, interference, advance);
            }
            else if (advance.IsReDouble)
            {
                //  TODO: InterpretRedouble(overcall, interference, advance);
            }
            else if (overcall.IsDouble)
            {
                // TODO: This is takeout double stuff here.  Should be handled by takeout double logic.
            }
            else if (overcall.Suit == Suit.Unknown)
            {
                // TODO: REMOVE this section since NT is now handled by state machine....
                // TODO: Make Unusual NT state-machine before removing this logic...
            }
            else if (advance.IsBid && overcall.IsBid)
            {
                AdvanceSuitedOvercall(opening, overcall, advance);
            }
        }

        private static void AdvanceSuitedOvercall(InterpretedBid opening, InterpretedBid overcall, InterpretedBid advance)
        {
            if (opening.declareBid.suit == overcall.declareBid.suit && opening.declareBid.suit == advance.declareBid.suit)
            {
                //  a cuebid advance when overcall was also a cuebid is unknown (for now)
                //  TODO: Determine if there are conditions where this makes sense
            }
            else if (opening.declareBid.suit == advance.declareBid.suit)
            {
                //  cuebid the oppenents' suit to show support with 10+ points
                advance.BidConvention = BidConvention.Cuebid;
                advance.BidMessage = BidMessage.Forcing;
                advance.Points.Min = 10;
                advance.HandShape[overcall.declareBid.suit].Min = 3;
                advance.Description = string.Empty;
            }
            else if (overcall.declareBid.suit == advance.declareBid.suit)
            {
                //  advancing with support
                //  6-9 points = raise with 3-card support, e.g. (1C)-1H-(P)-2H
                if (advance.declareBid.level == overcall.declareBid.level + 1)
                {
                    advance.Points.Min = 6;
                    advance.Points.Max = 9;
                    advance.HandShape[advance.declareBid.suit].Min = 3;
                    advance.HandShape[advance.declareBid.suit].Max = 3;
                    advance.Description = $"Raise; 3+ {advance.declareBid.suit}";
                }

                //  0-9 points = jump raise with 4-card support, e.g. (1C)-1H-(P)-3H
                if (advance.declareBid.level == overcall.declareBid.level + 2)
                {
                    advance.Points.Max = 9;
                    advance.HandShape[advance.declareBid.suit].Min = 4;
                    advance.HandShape[advance.declareBid.suit].Max = 4;
                    advance.Description = $"Jump raise; 4+ {advance.declareBid.suit}";
                }

                //  0-9 points = bid game with 5+ card support, 
                if (advance.declareBid.level == (BridgeBot.IsMajor(advance.declareBid.suit) ? 4 : 5))
                {
                    advance.Points.Max = 9;
                    advance.HandShape[advance.declareBid.suit].Min = 5;
                    advance.Description = $"5+ {advance.declareBid.suit}";
                }
            }
            else if (advance.declareBid.suit == Suit.Unknown)
            {
                //  advancing in notrump, e.g. (1C)-1H-(P)-1N
                advance.IsBalanced = true;
                advance.Description = $"stopper in {opening.declareBid.suit}";
                advance.Validate = hand => BasicBidding.HasStopper(hand, opening.declareBid.suit);

                if (advance.declareBid.level == overcall.declareBid.level)
                {
                    advance.Points.Min = 6;
                    advance.Points.Max = 10;
                }
                else if (advance.declareBid.level == overcall.declareBid.level + 1)
                {
                    advance.Points.Min = 11;
                    advance.Points.Max = 12;
                }
            }
            else
            {
                //  advancing in a new suit, e.g. (1C)-1H-(P)-1S
                advance.Points.Min = advance.declareBid.level == 1 ? 6 : 11;
                advance.HandShape[advance.declareBid.suit].Min = 5;
                advance.IsGood = true;
                advance.Description = $"5+ {advance.declareBid.suit}";
            }
        }
    }
}