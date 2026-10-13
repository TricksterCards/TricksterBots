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

            if (advance.bid == BidBase.Pass)
            {
                advance.Description = "No fit";
            }
            else if (advance.bid == BridgeBid.Double)
            {
                //  TODO: InterpretDouble(overcall, interference, advance);
            }
            else if (advance.bid == BridgeBid.Redouble)
            {
                //  TODO: InterpretRedouble(overcall, interference, advance);
            }
            else if (overcall.bid == BridgeBid.Double)
            {
                //  TODO: advance a double overcall
            }
            else if (overcall.declareBid.suit == Suit.Unknown)
            {
                AdvanceNotrumpOvercall(overcall, advance);
            }
            else if (overcall.IsPreemptive)
            {
                Response.InterpretResponseToPreempt(overcall, advance.History[advance.Index - 1], advance);
                //  Keep a new-suit advance constructive (and forcing), even opposite a weak jump overcall.
                if (advance.declareBid.suit != Suit.Unknown && advance.declareBid.suit != overcall.declareBid.suit &&
                    advance.declareBid.level < advance.GameLevel)
                    advance.Points.Min = 14;
            }
            else
            {
                AdvanceSuitedOvercall(opening, overcall, advance);
            }
        }

        //  natural notrump raises of a notrump overcall (Stayman and transfers are handled as conventions)
        private static void AdvanceNotrumpOvercall(InterpretedBid overcall, InterpretedBid advance)
        {
            if (!advance.bidIsDeclare || advance.declareBid.suit != Suit.Unknown || overcall.declareBid.level != 1 || overcall.Points.Max >= 37)
                return;

            advance.BidPointType = BidPointType.Hcp;
            if (advance.declareBid.level == 2)
            {
                advance.Points.Min = 25 - overcall.Points.Max;
                advance.Points.Max = 25 - 1 - overcall.Points.Min;
                advance.IsBalanced = true;
                advance.Description = "Inviting game";
            }
            else if (advance.declareBid.level == 3)
            {
                advance.Points.Min = 25 - overcall.Points.Min;
                advance.BidMessage = BidMessage.Signoff;
                advance.Description = "Sign-off at game";
            }
        }

        private static void AdvanceSuitedOvercall(InterpretedBid opening, InterpretedBid overcall, InterpretedBid advance)
        {
            //  partner may have overcalled with less in the balancing seat, so we need about 3 more points for each action
            var isBalancing = Overcall.IsBalancing(overcall);
            var adjust = isBalancing ? Overcall.BalancingAdjustment : 0;

            if (opening.declareBid.suit == overcall.declareBid.suit && opening.declareBid.suit == advance.declareBid.suit && advance.declareBid.suit != Suit.Unknown)
            {
                //  a cuebid advance when overcall was also a cuebid is unknown (for now)
                //  TODO: Determine if there are conditions where this makes sense
            }
            else if (opening.declareBid.suit == advance.declareBid.suit && advance.declareBid.level <= 3 && advance.declareBid.level == advance.LowestAvailableLevel(advance.declareBid.suit, true) && advance.declareBid.suit != Suit.Unknown)
            {
                //  cuebid the oppenents' suit to show support with 10+ points
                advance.BidConvention = BidConvention.Cuebid;
                advance.BidMessage = BidMessage.Forcing;
                advance.Points.Min = (advance.Options.bidding == BridgeBiddingScheme.Acol ? 11 : 10) + adjust;
                advance.HandShape[overcall.declareBid.suit].Min = 3;
                advance.Description = $"3+ {overcall.declareBid.suit} (usually)";

                if (BridgeBot.IsMajor(overcall.declareBid.suit))
                    advance.BidPointType = BidPointType.Dummy;
            }
            else if (overcall.declareBid.suit == advance.declareBid.suit)
            {
                //  advancing with support
                //  6-9 points = raise with 3-card support, e.g. (1C)-1H-(P)-2H
                if (advance.declareBid.level == overcall.declareBid.level + 1)
                {
                    advance.Points.Min = 6 + adjust;
                    advance.Points.Max = (advance.Options.bidding == BridgeBiddingScheme.Acol ? 10 : 9) + adjust;
                    advance.HandShape[advance.declareBid.suit].Min = 3;
                    advance.HandShape[advance.declareBid.suit].Max = isBalancing ? 4 : 3;
                    advance.Description = $"Raise; 3+ {advance.declareBid.suit}";

                    if (BridgeBot.IsMajor(advance.declareBid.suit))
                        advance.BidPointType = BidPointType.Dummy;
                }

                //  0-9 points = jump raise with 4-card support, e.g. (1C)-1H-(P)-3H (preemptive; stronger hands cuebid)
                if (advance.declareBid.level == overcall.declareBid.level + 2)
                {
                    advance.Points.Max = 9;
                    if (isBalancing)
                    {
                        //  opposite a balancing overcall a jump raise is invitational, not preemptive
                        advance.Points.Min = 10 + adjust;
                        advance.Points.Max = 12 + adjust;
                    }

                    advance.HandShape[advance.declareBid.suit].Min = 4;
                    advance.HandShape[advance.declareBid.suit].Max = 4;
                    advance.Description = $"Jump raise; 4+ {advance.declareBid.suit}";

                    if (BridgeBot.IsMajor(advance.declareBid.suit))
                        advance.BidPointType = BidPointType.Dummy;
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
                if (advance.declareBid.level == 4)
                {
                    advance.BidConvention = BidConvention.Blackwood;
                    advance.BidMessage = BidMessage.Forcing;
                    advance.Description = "asking for Aces";
                    //  TODO: validate knowing count of Aces will help decision to bid slam
                    advance.Validate = hand => false;
                }
                //  advancing in notrump, e.g. (1C)-1H-(P)-1N
                else if (advance.declareBid.level == 1)
                {
                    advance.BidPointType = BidPointType.Hcp;
                    advance.Points.Min = 6 + adjust;
                    advance.Points.Max = 10 + adjust;
                    advance.IsBalanced = true;
                    advance.Description = $"stopper in {opening.declareBid.suit}";
                    advance.Validate = hand => BasicBidding.HasStopper(hand, opening.declareBid.suit);
                }
                //  e.g. (1C)-1H-(P)-2N or (1S)-2D-(X)-2N
                else if (advance.declareBid.level == 2)
                {
                    advance.BidPointType = BidPointType.Hcp;
                    advance.Points.Min = 11 + adjust;
                    advance.Points.Max = 12 + adjust;
                    advance.IsBalanced = true;
                    advance.Description = $"stopper in {opening.declareBid.suit}";
                    advance.Validate = hand => BasicBidding.HasStopper(hand, opening.declareBid.suit);
                }
                //  e.g. (1C)-1H-(P)-3N or (1S)-2D-(P)-3N
                else if (advance.declareBid.level == 3)
                {
                    advance.BidPointType = BidPointType.Hcp;
                    advance.Points.Min = 13 + adjust;
                    advance.Points.Max = 16 + adjust;
                    advance.IsBalanced = true;
                    advance.Description = $"stopper in {opening.declareBid.suit}";
                    advance.Validate = hand => BasicBidding.HasStopper(hand, opening.declareBid.suit);
                }
            }
            else if (advance.declareBid.suit == opening.declareBid.suit)
            {
                //  TODO: a jump cuebid of opener's suit (e.g. splinter or strong raise) is unknown (for now)
            }
            else
            {
                //  advancing in a new suit (without jumping), e.g. (1C)-1H-(P)-1S
                if (advance.declareBid.level != advance.LowestAvailableLevel(advance.declareBid.suit, true))
                    return;

                advance.Points.Min = (advance.declareBid.level == 1 ? 6 : 11) + adjust;
                advance.HandShape[advance.declareBid.suit].Min = 5;
                advance.IsGood = true;
                advance.Description = $"5+ {advance.declareBid.suit}";

                if (advance.Options.bidding == BridgeBiddingScheme.Acol)
                    advance.BidMessage = BidMessage.Forcing;
            }
        }
    }
}