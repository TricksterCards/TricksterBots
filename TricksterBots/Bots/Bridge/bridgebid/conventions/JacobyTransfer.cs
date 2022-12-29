using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Policy;
using Trickster.cloud;
using TricksterBots.Bots;
using static Trickster.Bots.InterpretedBid;
using static TricksterBots.Bots.NTFundamentals;

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
                PlaceContract(bid.History[bid.Index - 4], bid.History[bid.Index - 2], bid);
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
                    // bec
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
                    rebid.Description = $"Invite in game; 5 {transferSuit}";
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

        // NEW STUFF BELOW...
        // NOTE: Because this method is called as one of many conventinons, it returns true if it 
        // handled the call.  Because 
        public static bool InitiateTransfer(InterpretedBid call, NTFundamentals ntInfo, bool fourWayTransfers)
        {
            if (call.RhoInterfered)
            {
                return false;
            }

            if (call.declareBid.level == ntInfo.BidLevel + 1)
            {
                if (call.declareBid.suit == Suit.Diamonds || call.declareBid.suit == Suit.Hearts)
                {
                    var transferSuit = call.declareBid.suit == Suit.Diamonds ? Suit.Hearts : Suit.Spades;
                    call.HandShape[transferSuit].Min = 5;
                    call.Description = $"Transfer to {Card.SuitSymbol(transferSuit)}; 5+ {transferSuit}";
                    call.PartnersCall = c => AcceptMajorTransfer(c, ntInfo, transferSuit);
                }
                if (call.declareBid.suit == Suit.Spades)
                {
                    // TODO: Minor transfers here...
                }
                return true;
            }
            // Because we in the future we will support 4-way transfers, this code it responsible for
            // defining 2NT.  If thats what we're being asked about then define it as a balanced invitation
            if (call.Is(2, Suit.Unknown))
            {
                call.BidPointType = BidPointType.Hcp;
                call.SetPoints(ntInfo.ResponderInvitationalPoints);
                call.IsBalanced = true;
                call.Description = string.Empty;
                return true;
            }
            // If we don't handle the call then return false to let some other code have a wack at it...
            return false;
        }

        private static void AcceptMajorTransfer(InterpretedBid call, NTFundamentals ntInfo, Suit transferSuit)
        {

            // If there is any other interference then punt
            // TODO: Perhaps look for opportunities to super-accept...
            if (call.RhoInterfered && !call.RhoDoubled)
            {
                CompetitiveAuction.HandleInterference(call);
                return;
            }

            // If RHO doubled then conditionally accept the transfer.  Pass if only two cards in the suit.
            int minKnown = 2;
            if (call.RhoDoubled)
            {
                if (call.IsPass)
                {
                    call.SetPoints(ntInfo.OpenerPoints);
                    call.HandShape[transferSuit].Min = 2;
                    call.HandShape[transferSuit].Max = 2;
                    call.Description = $"pass transfer to {transferSuit} indicating no fit after opponent X";
                    call.PartnersCall = c => DescribeTransfer(c, ntInfo, transferSuit, minKnown);
                    return;
                }
                minKnown = 3;
            }

            if (call.Is(ntInfo.BidLevel + 1, transferSuit))
            {
                call.HandShape[transferSuit].Min = minKnown;
                call.HandShape[transferSuit].Max = 5;
                call.SetPoints(ntInfo.OpenerPoints);
                call.Description = $"Accept transfer to {transferSuit}";
                call.PartnersCall = c => DescribeTransfer(c, ntInfo, transferSuit, minKnown);
            }
            if (ntInfo.BidLevel == 1 && call.Is(3, transferSuit))
            {
                call.SetPoints(ntInfo.OpenerPoints.Max, ntInfo.OpenerPoints.Max);
                call.HandShape[transferSuit].Min = 4;
                call.HandShape[transferSuit].Max = 5;
                call.PartnersCall = c => DescribeTransfer(c, ntInfo, transferSuit, minKnown);
            }
        }


        public static void DescribeTransfer(InterpretedBid call, NTFundamentals ntInfo, Suit transferSuit, int minFit)
        {
            if (call.RhoBid)    // Ignore doubles but punt on any RHO bid
            {
                // TODO: Maybe still do some thing here if competition level is low...
                CompetitiveAuction.HandleInterference(call);
                return;
            }

            // This is only possible if 1NT bidder has passed when opps have doubled.  If
            // we have a minimal hand then we need to complete the transfer ourselves...
            if (call.Is(2, transferSuit))
            {
                call.SetPoints(0, ntInfo.ResponderInvitationalPoints.Min - 1);
                call.HandShape[transferSuit].Min = 5;
                call.PartnersCall = CompetitiveAuction.PassOrCompete;
            }

            // TODO: Maybe if suit is stopped and 5-cards we would want to bid this even if knownFit...
            if (call.Is(2, Suit.Unknown) && minFit == 2)
            {
                call.SetPoints(ntInfo.ResponderInvitationalPoints);
                call.HandShape[transferSuit].Min = 5;
                call.HandShape[transferSuit].Max = 5;
                call.Description = $"Invite to game; 5 {transferSuit}";
                call.PartnersCall = c => RebidAfterInvitation(c, ntInfo, transferSuit);
            }
            else if (call.Is(3, transferSuit))
            {
                call.SetPoints(ntInfo.ResponderInvitationalPoints);
                call.HandShape[transferSuit].Min = minFit > 2 ? 5 : 6;
                call.Description = $"Invite to game; 6+ {transferSuit} or known fit";
                call.PartnersCall = c => RebidAfterInvitation(c, ntInfo, transferSuit);
            }
            else if (call.Is(3, Suit.Unknown) && minFit == 2)
            {
                call.SetPoints(ntInfo.ResponderGamePoints);
                call.HandShape[transferSuit].Min = 5;
                call.HandShape[transferSuit].Max = 5;
                call.Description = $"Game in NT or {transferSuit}; 5 {transferSuit}";
                call.PartnersCall = c => PickGameAfterTransfer(c, ntInfo, transferSuit);
            }
            else if (call.Is(4, transferSuit))
            {
                // TODO: Super-accept should shave some points off of this...
                call.SetPoints(ntInfo.ResponderGamePoints);
                call.HandShape[transferSuit].Min = minFit == 2 ? 5 : 6;
                // TODO: Need Max too?
                call.Description = $"Game in {transferSuit}; 6+ {transferSuit} or known fit";
                call.PartnersCall = CompetitiveAuction.PassOrCompete;
            }
            // TODO: Slam bids here at 4NT...
        }
    

        public static void RebidAfterInvitation(InterpretedBid call, NTFundamentals ntInfo, Suit transferSuit)
        {
            // TODO: need to deal with some interference...  For now ignore X and punt on anything else.
            if (call.RhoBid)
            {
                CompetitiveAuction.HandleInterference(call);
                return;
            }
            // Assume this is the last bid in the auction.  Override if not 
            call.PartnersCall = CompetitiveAuction.PassOrCompete;

            if (call.Is(3, Suit.Unknown))
            {
                call.SetPoints(ntInfo.OpenerAcceptInvitePoints);
                call.HandShape[transferSuit].Min = 2;
                call.HandShape[transferSuit].Max = 2;
                call.Description = $"Accept invitation to play in 3NT; 2 {transferSuit}";
            }

            var otherMajor = transferSuit == Suit.Hearts ? Suit.Spades : Suit.Hearts;
            if (call.Is(3, otherMajor))
            {
                call.SetPoints(ntInfo.OpenerAcceptInvitePoints); 
                call.HandShape[transferSuit].Min = 2;
                call.HandShape[transferSuit].Max = 2;
                call.HandShape[otherMajor].Min = 5;
                call.HandShape[otherMajor].Max = 5;
                call.Description = $"No fit in {transferSuit}.  Show 5 {otherMajor} and accept invitation to game";
                call.PartnersCall = c => TrySecondMajorAfterTransfer(c, ntInfo, otherMajor);
            } 
            if (call.Is(4, transferSuit))
            {
                call.SetPoints(ntInfo.OpenerAcceptInvitePoints);
                call.HandShape[transferSuit].Min = 3;
                call.HandShape[transferSuit].Max = 5;
                call.Description = $"Accept invitation to game in {transferSuit}; 3+ {transferSuit}";
            }
        }

   

        public static void PickGameAfterTransfer(InterpretedBid call, NTFundamentals ntInfo, Suit transferSuit)
        {
            // TODO: need to deal with some interference...  For now ignore X and punt on anything else.
            if (call.RhoBid)
            {
                CompetitiveAuction.HandleInterference(call);
                return;
            }
            // TODO: Interference handle it here?  Maybe ignore it
            if (call.Is(4, transferSuit))
            {        
                call.SetPoints(ntInfo.OpenerPoints);
                call.HandShape[transferSuit].Min = 3;
                call.HandShape[transferSuit].Max = 5;
                call.Description = $"Accept transfer; 3+ {transferSuit}";
            }
            // No matter what, we are done with the auction now.
            call.PartnersCall = CompetitiveAuction.PassOrCompete;
        }
  

        public static void TrySecondMajorAfterTransfer(InterpretedBid call, NTFundamentals ntInfo, Suit otherMajor)
        {
            // TODO: need to deal with some interference...  For now ignore X and punt on anything else.
            if (call.RhoBid)
            {
                CompetitiveAuction.HandleInterference(call);
                return;
            }

            call.PartnersCall = CompetitiveAuction.PassOrCompete;

            if (call.Is(3, Suit.Unknown))
            {
                call.SetPoints(ntInfo.ResponderInvitationalPoints);
                call.HandShape[otherMajor].Max = 2;
                call.HandShape[otherMajor].Min = 2;
                call.Description = $"No fit in {otherMajor};  Play game at 3NT";
            }
            if (call.Is(4, otherMajor))
            {
                call.SetPoints(ntInfo.ResponderInvitationalPoints);
                call.HandShape[otherMajor].Min = 3;
                call.HandShape[otherMajor].Max = 10;  // TODO: Need Max?
                call.Description = $"Play game in {otherMajor}";
            }
        }
    }
}