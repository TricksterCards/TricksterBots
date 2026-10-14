using System.Collections.Generic;
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
                //  6-9 points (6-10 in Acol) = raise with 3+ card support, e.g. (1C)-1H-(P)-2H
                //  (weak hands with 4+ support prefer the preemptive jump raise)
                if (advance.declareBid.level == overcall.declareBid.level + 1)
                {
                    advance.Points.Min = 6 + adjust;
                    advance.Points.Max = (advance.Options.bidding == BridgeBiddingScheme.Acol ? 10 : 9) + adjust;
                    advance.HandShape[advance.declareBid.suit].Min = 3;
                    advance.Description = $"Raise; 3+ {advance.declareBid.suit}";

                    if (BridgeBot.IsMajor(advance.declareBid.suit))
                        advance.BidPointType = BidPointType.Dummy;
                }

                //  0-9 points = jump raise with 4+ card support, e.g. (1C)-1H-(P)-3H (preemptive; stronger hands cuebid)
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
                    advance.Description = $"Jump raise; 4+ {advance.declareBid.suit}";

                    if (BridgeBot.IsMajor(advance.declareBid.suit))
                        advance.BidPointType = BidPointType.Dummy;
                }

                var suit = advance.declareBid.suit;
                var isMinor = !BridgeBot.IsMajor(suit);

                //  0-9 points = preemptive double-jump raise of a minor to the 4-level with extra shape
                //  (5-card support with a void, or 6+ card support), e.g. (1C)-1D-(P)-4D
                if (isMinor && advance.declareBid.level == 4 && advance.declareBid.level > overcall.declareBid.level + 2)
                {
                    advance.Points.Max = 9;
                    advance.HandShape[suit].Min = 5;
                    advance.Description = $"Preemptive raise; 5+ {suit}";
                    advance.Validate = hand =>
                    {
                        var counts = BasicBidding.CountsBySuit(hand);
                        return counts[suit] >= 6 || SuitRank.stdSuits.Any(s => counts[s] == 0);
                    };
                }

                //  0-9 points = bid game with 5+ card support in a major, or 6+ card support and a singleton or void in a minor
                if (advance.declareBid.level == (isMinor ? 5 : 4))
                {
                    advance.Points.Max = 9;
                    advance.HandShape[suit].Min = isMinor ? 6 : 5;
                    advance.Description = $"{advance.HandShape[suit].Min}+ {suit}";

                    if (isMinor)
                    {
                        advance.Validate = hand =>
                        {
                            var counts = BasicBidding.CountsBySuit(hand);
                            return SuitRank.stdSuits.Any(s => counts[s] <= 1);
                        };
                    }
                }
            }
            else if (advance.declareBid.suit == Suit.Unknown)
            {
                var stopperSuits = OpponentsSuits(opening, overcall, advance);
                var stopperDescription = $"stopper{(stopperSuits.Count > 1 ? "s" : "")} in {string.Join(" and ", stopperSuits)}";
                bool hasStoppers(Hand hand) => stopperSuits.All(s => BasicBidding.HasStopper(hand, s));

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
                    advance.Description = stopperDescription;
                    advance.Validate = hasStoppers;

                    //  with stoppers, notrump is more descriptive than raising a minor (3NT is likelier than 5m)
                    if (!BridgeBot.IsMajor(overcall.declareBid.suit))
                        advance.Priority = 95;
                }
                //  e.g. (1C)-1H-(P)-2N or (1S)-2D-(X)-2N
                else if (advance.declareBid.level == 2)
                {
                    advance.BidPointType = BidPointType.Hcp;
                    advance.Points.Min = 11 + adjust;
                    advance.Points.Max = 12 + adjust;
                    advance.IsBalanced = true;
                    advance.Description = stopperDescription;
                    advance.Validate = hasStoppers;
                }
                //  e.g. (1C)-1H-(P)-3N or (1S)-2D-(P)-3N
                else if (advance.declareBid.level == 3)
                {
                    advance.BidPointType = BidPointType.Hcp;
                    advance.Points.Min = 13 + adjust;
                    advance.Points.Max = 16 + adjust;
                    advance.IsBalanced = true;
                    advance.Description = stopperDescription;
                    advance.Validate = hasStoppers;
                }
            }
            else if (advance.declareBid.suit == opening.declareBid.suit)
            {
                //  TODO: a jump cuebid of opener's suit (e.g. splinter or strong raise) is unknown (for now)
            }
            else if (OpponentsSuits(opening, overcall, advance).Contains(advance.declareBid.suit))
            {
                //  a cuebid of responder's suit is unknown since its meaning depends on partnership agreement, e.g. (1C)-1D-(1S)-2S
            }
            else if (IsFitJump(opening, overcall, advance))
            {
                InterpretFitJump(overcall, advance);
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

        //  the opponents' natural suits: opener's suit plus any new suit bid by responder, e.g. (1C)-1D-(1S)-1N needs clubs and spades
        private static List<Suit> OpponentsSuits(InterpretedBid opening, InterpretedBid overcall, InterpretedBid advance)
        {
            var suits = new List<Suit> { opening.declareBid.suit };
            var interference = advance.History[advance.Index - 1];
            if (interference.bidIsDeclare && interference.BidConvention == BidConvention.None &&
                interference.declareBid.suit != Suit.Unknown && interference.declareBid.suit != overcall.declareBid.suit &&
                !suits.Contains(interference.declareBid.suit))
                suits.Add(interference.declareBid.suit);

            return suits;
        }

        //  modern Acol: a single jump in an unbid suit by an advancer who passed before the opening can't be strong,
        //  so it shows a good 5+ card suit with support for partner's overcall, e.g. P-(1C)-1D-(1S)-3H
        private static bool IsFitJump(InterpretedBid opening, InterpretedBid overcall, InterpretedBid advance)
        {
            if (advance.Options.bidding != BridgeBiddingScheme.Acol || !advance.bidIsDeclare || advance.declareBid.suit == Suit.Unknown)
                return false;

            var passedBeforeOpening = advance.Index >= 4 && advance.Index - 4 < opening.Index && advance.History[advance.Index - 4].bid == BidBase.Pass;
            if (!passedBeforeOpening || advance.SuitsBid.Contains(advance.declareBid.suit))
                return false;

            return advance.declareBid.level <= 3 && advance.declareBid.level == advance.LowestAvailableLevel(advance.declareBid.suit, true) + 1;
        }

        private static void InterpretFitJump(InterpretedBid overcall, InterpretedBid advance)
        {
            var suit = advance.declareBid.suit;
            var overcallSuit = overcall.declareBid.suit;

            advance.Points.Min = 8;
            advance.Points.Max = 12;
            advance.HandShape[suit].Min = 5;
            advance.HandShape[overcallSuit].Min = 4;
            advance.Description = $"Fit jump; 5+ {suit} with 4+ {overcallSuit}";
            //  values should be concentrated in the suit we're showing (two of the top five honors)
            advance.Validate = hand => hand.Count(c => c.suit == suit && c.rank >= Rank.Ten) >= 2;
        }
    }
}