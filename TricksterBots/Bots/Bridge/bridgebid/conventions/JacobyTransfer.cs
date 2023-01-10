using System.Net.Http.Headers;
using System.Numerics;
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
        public static void InitiateTransfer(InterpretedBid call, NTFundamentals ntInfo, bool fourWayTransfers)
        {
            if (call.RhoInterferedAbove(ntInfo.BidLevel + 1, Suit.Clubs))   // TODO: Is this correct for SAYC?  Systems still on?  Need to review.
            {
                CompetitiveAuction.HandleInterference(call);
            }
            else if (call.declareBid != null && call.declareBid.level == ntInfo.BidLevel + 1)
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
                   // TODO: This is not used - maybe it should be  BidConvention?  Relay is NOT a convention anyway call.BidConvention = BidConvention.Relay;
                    call.BidMessage = BidMessage.Forcing;
                    call.HandShape[Suit.Hearts].Max = 4;
                    call.HandShape[Suit.Spades].Max = 4;
                    call.SetHighCardPoints(0, ntInfo.ResponderInvitationalPoints.Min - 1);   // Must have < invitational points
                    // TODO: With 4-way transfers we DO want to transfer with slam invitational points...
                    call.Description = " to 3♣; 6+ Clubs or Diamonds";
                    call.Validate = hand =>
                    {
                        //  validate matched hands have 6+ cards in a minor
                        var counts = BasicBidding.CountsBySuit(hand);
                        return counts[Suit.Clubs] >= 6 || counts[Suit.Diamonds] >= 6;
                    };
                    call.PartnersCall = c => AcceptMinorTransfer(c, ntInfo);
                }
                // TODO: Where does this go?  If 4-way transfers then we need to control this bid within the Transfer logic.  
                if (call.Is(2, Suit.Unknown))
                {
                    call.SetHighCardPoints(ntInfo.ResponderInvitationalPoints);
                    call.IsBalanced = true;
                    call.Description = "Invite to game";
                    call.PartnersCall = c => OpenerRebidAfterGameInvitation(c, ntInfo);
                }
            }
        }


        // Responder has bid 2NT inviatation to game.  
        private static void OpenerRebidAfterGameInvitation(InterpretedBid rebid, NTFundamentals ntInfo)
        {
            ShowMajorBid(rebid, ntInfo, Suit.Hearts);
            ShowMajorBid(rebid, ntInfo, Suit.Spades);
			if (rebid.Is(3, Suit.Unknown))
			{
				rebid.SetHighCardPoints(ntInfo.OpenerAcceptInvitePoints);
                rebid.HandShape[Suit.Hearts].Max = 4;
                rebid.HandShape[Suit.Spades].Max = 4;
				rebid.Description = $"Accept invitation to play in 3NT; No 5 card major";
                rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
			}
		}

        private static void ShowMajorBid(InterpretedBid call, NTFundamentals ntInfo, Suit major)
        {
            if (call.Is(3, major))
            {
				call.SetHighCardPoints(ntInfo.OpenerAcceptInvitePoints);
				call.HandShape[major].Min = 5;
				call.HandShape[major].Max = 5;
				call.Description = $"Show 5 {major} and accept invitation to game";
				call.PartnersCall = c => TryOpenersMajorAfterInvitation(c, ntInfo, major);
			}
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
                    call.SetHighCardPoints(ntInfo.OpenerPoints);
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
                // call.HandShape[transferSuit].Min = minKnown;
                // call.HandShape[transferSuit].Max = 5;
                // TODO: Always match?  Or use these points?  call.SetPoints(ntInfo.OpenerPoints);
                call.AlternateMatches = hand => true;
                call.Description = $"Accept transfer to {transferSuit}";
                call.PartnersCall = c => DescribeTransfer(c, ntInfo, transferSuit, minKnown);
            }
            if (ntInfo.BidLevel == 1 && call.Is(3, transferSuit))
            {
                call.SetHighCardPoints(ntInfo.OpenerPoints.Max, ntInfo.OpenerPoints.Max);
                call.HandShape[transferSuit].Min = 4;
                call.HandShape[transferSuit].Max = 5;
                call.PartnersCall = c => DescribeTransfer(c, ntInfo, transferSuit, 4);
            }
        }

        private static void AcceptMinorTransfer(InterpretedBid call, NTFundamentals ntInfo)
        {
            // TODO: Think about all interference here...  Do we actually accept the transfer?  Or just pass.....
            // If RHO doubled then conditionally accept the transfer.  Pass if only two cards in the suit
            // 
            
            // If there is any other interference then punt
            if (call.RhoInterfered && !call.RhoDoubled)
            {
                CompetitiveAuction.HandleInterference(call);
                return;
            }

            
            if (call.Is(ntInfo.BidLevel + 2, Suit.Clubs))
            {
                call.BidConvention = BidConvention.AcceptRelay;
                call.Description = string.Empty;
                call.AlternateMatches = hand => true;
                call.PartnersCall = c => CompleteMinorTransfer(c, ntInfo);
            }
        }

        private static void CompleteMinorTransfer(InterpretedBid call, NTFundamentals ntInfo)
        {
            // TODO: Interference...
            if (call.IsPass)
            {
                call.BidMessage = BidMessage.Signoff;
                call.HandShape[Suit.Clubs].Min = 6;
                call.Description = $"6+ {Suit.Clubs}";
                call.PartnersCall = CompetitiveAuction.PassOrCompete;
            }
            if (call.Is(ntInfo.BidLevel + 2, Suit.Diamonds)) 
            {
                call.BidMessage = BidMessage.Signoff;
                call.HandShape[Suit.Diamonds].Min = 6;
                call.Description = $"6+ {Suit.Diamonds}";
                call.PartnersCall = CompetitiveAuction.PassOrCompete;
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
                call.SetHighCardPoints(0, ntInfo.ResponderInvitationalPoints.Min - 1);
                call.HandShape[transferSuit].Min = 5;
                call.PartnersCall = CompetitiveAuction.PassOrCompete;
            }

            // TODO: Maybe if suit is stopped and 5-cards we would want to bid this even if knownFit...
            if (call.Is(2, Suit.Unknown) && minFit == 2)
            {
                call.SetHighCardPoints(ntInfo.ResponderInvitationalPoints);
                call.HandShape[transferSuit].Min = 5;
                call.HandShape[transferSuit].Max = 5;
                call.Description = $"Invite to game; 5 {transferSuit}";
                call.PartnersCall = c => RebidAfterInvitation(c, ntInfo, transferSuit);
            }
            else if (call.Is(3, transferSuit))
            {
                call.SetHighCardPoints(ntInfo.ResponderInvitationalPoints);
                call.HandShape[transferSuit].Min = minFit > 2 ? 5 : 6;
                call.HandShape[transferSuit].Max = 10;  // TODO: This seems a bit silly.  
                call.Description = $"Invite to game; 6+ {transferSuit} or known fit";
                call.PartnersCall = c => RebidAfterInvitation(c, ntInfo, transferSuit);
            }
            else if (call.Is(3, Suit.Unknown) && minFit == 2)
            {
                call.SetHighCardPoints(ntInfo.ResponderGamePoints);
                call.HandShape[transferSuit].Min = 5;
                call.HandShape[transferSuit].Max = 5;
                call.Description = $"Game in NT or {transferSuit}; 5 {transferSuit}";
                call.PartnersCall = c => PickGameAfterTransfer(c, ntInfo, transferSuit);
            }
            else if (call.Is(4, transferSuit))
            { 
                call.SetHighCardPoints(ntInfo.ResponderGamePoints);
                if (minFit == 4)    // TODO: Kind of side-effect working here...  This means super-accepted
                {
                    call.Points.Min -= 4;
                }
                call.HandShape[transferSuit].Min = minFit > 2 ? 5 : 6;
                call.HandShape[transferSuit].Max = 10;  // TODO: Seems silly too
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

            if (call.Is(3, transferSuit))
            {
                call.SetHighCardPoints(ntInfo.OpenerPoints.Min, ntInfo.OpenerPoints.Min);
                call.HandShape[transferSuit].Min = 3;
                call.HandShape[transferSuit].Max = 5;
                call.Description = $"Reject invitation to game, play at 3{transferSuit}; 3+ {transferSuit}";
                return;
            }

            if (call.Is(3, Suit.Unknown))
            {
                call.SetHighCardPoints(ntInfo.OpenerAcceptInvitePoints);
                call.HandShape[transferSuit].Min = 2;
                call.HandShape[transferSuit].Max = 2;
                call.Description = $"Accept invitation to play in 3NT; 2 {transferSuit}";
            }

            var otherMajor = transferSuit == Suit.Hearts ? Suit.Spades : Suit.Hearts;
            if (call.Is(3, otherMajor))
            {
                call.SetHighCardPoints(ntInfo.OpenerAcceptInvitePoints); 
                call.HandShape[transferSuit].Min = 2;
                call.HandShape[transferSuit].Max = 2;
                call.HandShape[otherMajor].Min = 5;
                call.HandShape[otherMajor].Max = 5;
                call.Description = $"No fit in {transferSuit}.  Show 5 {otherMajor} and accept invitation to game";
                call.PartnersCall = c => TryOpenersMajorAfterInvitation(c, ntInfo, otherMajor);
            } 
            if (call.Is(4, transferSuit))
            {
                call.SetHighCardPoints(ntInfo.OpenerAcceptInvitePoints);
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
            if (call.IsPass)
            {
                // TODO: Describe meaning here...
                call.SetHighCardPoints(ntInfo.OpenerPoints);
                call.HandShape[transferSuit].Max = 2;
                call.HandShape[transferSuit].Min = 2;
                call.Description = $"2 {transferSuit};  Play in 3NT";
                call.BidMessage = BidMessage.Signoff;
            }
            if (call.Is(4, transferSuit))
            {        
                call.SetHighCardPoints(ntInfo.OpenerPoints);
                call.HandShape[transferSuit].Min = 3;
                call.HandShape[transferSuit].Max = 5;
                call.Description = $"Accept transfer; 3+ {transferSuit}";
            }
            // No matter what, we are done with the auction now.
            call.PartnersCall = CompetitiveAuction.PassOrCompete;
        }
  

        // There are two ways to get to this function:
        //    Responder invites with 2NT and opener has game-going values and a 5-card major
        //    Responder transfers to major X and then invites with 2NT.  Opener has other major and game values
        public static void TryOpenersMajorAfterInvitation(InterpretedBid call, NTFundamentals ntInfo, Suit openersMajor)
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
                call.SetHighCardPoints(ntInfo.ResponderInvitationalPoints);
                call.HandShape[openersMajor].Max = 2;
                call.HandShape[openersMajor].Min = 2;
                call.Description = $"No fit in {openersMajor};  Play game at 3NT";
            }
            if (call.Is(4, openersMajor))
            {
                call.SetHighCardPoints(ntInfo.ResponderInvitationalPoints);
                call.HandShape[openersMajor].Min = 3;
                call.HandShape[openersMajor].Max = 10;  // TODO: Need Max?
                call.Description = $"Play game in {openersMajor}";
            }
        }
    }
}