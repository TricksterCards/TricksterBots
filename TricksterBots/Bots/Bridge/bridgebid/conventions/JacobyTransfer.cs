using Trickster.cloud;

namespace Trickster.Bots
{
    internal class JacobyTransfer
    {
        public static bool CanUseTransfers(InterpretedBid bid)
        {
            return Stayman.CanUseStayman(bid);
        }

        public static bool Interpret(InterpretedBid bid)
        {
            if (CanUseTransfers(bid)) return InterpretTransfers(bid);

            if (bid.bidIsDeclare && bid.Index >= 4 && bid.History[bid.Index - 2].BidConvention == BidConvention.JacobyTransfer)
            {
                AcceptTransfer(bid.History[bid.Index - 2], bid.History[bid.Index - 1], bid);
                return true;
            }

            if (bid.bidIsDeclare && bid.Index >= 6 && bid.History[bid.Index - 4].BidConvention == BidConvention.JacobyTransfer)
            {
                InterpretResponderRebid(bid.History[bid.Index - 4], bid.History[bid.Index - 2], bid);
                return true;
            }

            // Now check for opener's re-rebid to place contract TODO: Need to go to slam in some cases?
            if (bid.bidIsDeclare && bid.Index >= 8 && bid.History[bid.Index - 4].BidConvention == BidConvention.AcceptJacobyTransfer)
            {
                PlaceContract(bid.History[bid.Index - 4], bid.History[bid.Index -2], bid);
                return true;
            }

            return false;
        }

        public static bool InterpretTransfers(InterpretedBid response)
        {
            if (!response.bidIsDeclare)
                return false;

            if (response.declareBid.suit != Suit.Diamonds && response.declareBid.suit != Suit.Hearts)
                return false;

            if (response.declareBid.level != response.LowestAvailableLevel(response.declareBid.suit))
                return false;

            //  1N-2D
            //  1N-2H
            //  2N-3D
            //  2N-3H
            //  3N-4D
            //  3N-4H
            //  2C-2D-2N-3D
            //  2C-2D-2N-3H
            //  2C-2D-3N-4D
            //  2C-2D-3N-4H
            response.BidConvention = BidConvention.JacobyTransfer;
            response.BidMessage = BidMessage.Forcing;
            var transferSuit = response.declareBid.suit == Suit.Diamonds ? Suit.Hearts : Suit.Spades;
            response.HandShape[transferSuit].Min = 5;
            response.Description = $" to {response.declareBid.level}{Card.SuitSymbol(transferSuit)}; 5+ {transferSuit}";

            return true;
        }

        private static void AcceptTransfer(InterpretedBid transfer, InterpretedBid interference, InterpretedBid accept)
        {
            if (!accept.bidIsDeclare || accept.declareBid.level > transfer.declareBid.level + 1)
                return;

            if (transfer.declareBid.suit == Suit.Diamonds && accept.declareBid.suit != Suit.Hearts)
                return;

            if (transfer.declareBid.suit == Suit.Hearts && accept.declareBid.suit != Suit.Spades)
                return;

            //  1N-2D-2H
            //  1N-2H-2S
            //  ...
            accept.BidConvention = BidConvention.AcceptJacobyTransfer;
            accept.Description = string.Empty;

            if (interference.bid == BridgeBid.Double)
                //  if transfer is doubled, opener only completes the transfer with 3+ trumps
                accept.HandShape[accept.declareBid.suit].Min = 3;
            else
                //  otherwise we always accept the transfer if opponent hasn't bid (in which case this won't be an option anyway)
                accept.AlternateMatches = hand => true;

            if (accept.declareBid.level == transfer.declareBid.level + 1)
            {
                //  1N-2D-3H
                //  1N-2D-3S
                //  ...
                accept.Points.Min = 17;
                accept.BidPointType = BidPointType.Dummy;
                accept.HandShape[accept.declareBid.suit].Min = 4;
                accept.Description = $"super-accept; 4+ {accept.declareBid.suit}";
                accept.AlternateMatches = null;
            }
        }

        private static void InterpretResponderRebid(InterpretedBid transfer, InterpretedBid accept, InterpretedBid rebid)
        {
            if (accept.declareBid == null || transfer.declareBid == null)
                return; // TODO: Handle X, etc...

            // Validate that transfer happened
            var transferSuit = transfer.declareBid.suit == Suit.Diamonds ? Suit.Hearts : Suit.Spades;
            if (accept.declareBid.suit != transferSuit)
                return;

            if (rebid.bid == BidBase.Pass)
            {
                rebid.Points.Max = 7;
                rebid.BidPointType = BidPointType.Hcp;
                return;
            }

            if (rebid.declareBid == null)
                return;     // TODO: Handle these cases X, etc.

            if (transferSuit == rebid.declareBid.suit)
            {
                if (rebid.declareBid.level == 3)
                {
                    rebid.Points.Min = 8;
                    rebid.Points.Max = 9;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.HandShape[transferSuit].Min = 6;
                    rebid.Description = $"Inviting game; 6+ {transferSuit}";
                    return;
                }
                // TODO: Slam stuff not implemented
                if (rebid.declareBid.level == 4)
                {
                    // If opener has super-accepted the transfer then game can be bid with weaker hand
                    if (accept.declareBid.level == 3)
                    {
                        rebid.Points.Min = 6;
                        rebid.BidPointType = BidPointType.Hcp;
                        rebid.HandShape[transferSuit].Min = 5;
                        rebid.Description = $"Sign-off at game; 5+ {transferSuit}";
                        // TODO: Weaker game with more trump cards - alternate matches
                    }
                    else
                    {
                        rebid.Points.Min = 10;
                        rebid.BidPointType = BidPointType.Hcp;
                        rebid.HandShape[transferSuit].Min = 6;
                        rebid.Description = $"Sign-off at game; 6+ {transferSuit}";
                    }
                }
                return;
            }

            if (rebid.declareBid.suit == Suit.Unknown)
            {
                if (rebid.declareBid.level == 2)
                {
                    rebid.Points.Min = 8;
                    rebid.Points.Max = 9;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.HandShape[transferSuit].Max = 5;
                    rebid.HandShape[transferSuit].Min = 5;
                    rebid.Description = $"Inviting game; 5 {transferSuit}";
                    return;
                }
                if (rebid.declareBid.level == 3)
                {
                    rebid.Points.Min = 10;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.HandShape[transferSuit].Max = 5;
                    rebid.HandShape[transferSuit].Min = 5;
                    rebid.Description = $"Sign-off at game; 5 {transferSuit}";
                    return;
                }
            }


        }

        private static void PlaceContract(InterpretedBid accept, InterpretedBid responderRebid, InterpretedBid rebid)
        {
            if (accept.declareBid == null || responderRebid.declareBid == null)
                return; // TODO: Handle X, etc...

            if (rebid.declareBid == null)
                return;     // TODO: Handle these cases X, etc.

            // Validate that transfer happened -- TODO: Check earlier bids were successful transfers?
            var transferSuit = accept.declareBid.suit;

            if (rebid.bid == BidBase.Pass)
            {
                rebid.Points.Max = 15;
                rebid.HandShape[transferSuit].Min = 2;
                rebid.HandShape[transferSuit].Max = 2;
                rebid.BidPointType = BidPointType.Hcp;
                return;
            }
            
            if (rebid.declareBid.suit == transferSuit)
            {
                if (rebid.declareBid.level == 3)
                {
                    rebid.Points.Min = 15;
                    rebid.Points.Max = 15;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.HandShape[transferSuit].Min = 3;
                    rebid.Description = $"Sign-off at partscore; 3+ {transferSuit}";
                    return;
                }
                if (rebid.declareBid.level == 4)
                {
                    rebid.Points.Min = (responderRebid.declareBid.level == 3 && responderRebid.declareBid.suit == Suit.Unknown) ? 15 : 16;
                    rebid.Points.Max = 17;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.HandShape[transferSuit].Min = 3;
                    rebid.Description = $"Sign-off at game; 3+ {transferSuit}";
                    return;
                }
            }

            if (rebid.declareBid.suit == Suit.Unknown && rebid.declareBid.level == 3)
            {
                rebid.Points.Min = 16;
                rebid.Points.Max = 17;
                rebid.BidPointType = BidPointType.Hcp;
                rebid.HandShape[transferSuit].Max = 2;
                rebid.HandShape[transferSuit].Min = 2;
                rebid.Description = $"Sign-off in game; 2 {transferSuit}";
                return;
            }

        }
    }
}