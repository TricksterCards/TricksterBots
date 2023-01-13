using System.CodeDom;
using System.Security.Cryptography;
using System.Threading;
using Trickster.cloud;
using TricksterBots.Bots;

namespace Trickster.Bots
{
    internal class Stayman 
    { 
    
    

        // TODO: Need to bid Stayman no matter what shape if using 4-way transfers.  
        public static void InitiateStayman(InterpretedBid response, NoTrump nt)
        {
            // TODO: Do I worrry about interference here or has caller already done that?
            // TODO: Should this be an assert that the bid is xC?  Or do I check for it an just return
            // if it isn't
            // TODO: Garbage Stayman... If 4441 or better then bid stayman and pass...
            if (response.Is(nt.BidLevel + 1, Suit.Clubs))
            {
			    response.BidConvention = BidConvention.Stayman;
			    response.BidMessage = BidMessage.Forcing;
                response.SetPoints(nt.ResponderInvitationalOrBetterPoints);
			    response.Description = "asking for a major";
			    response.Priority = 100; // always prefer Stayman over other bids when valid
                response.PartnersCall = c => OpenerRebid(c, nt);
			    response.Validate = hand =>
			    {
				    //  we should have 4H or 4S (any more and we'll use a transfer instead)
	                // TODO: Need to take 6/4 into account?  5/4 will happen after opener denies 4-card major
                    // Never bid stayman with 4333 - NOTE: This is not true with 4-way transfers
				    var counts = BasicBidding.CountsBySuit(hand);
				    return ((!BasicBidding.Is4333(counts)) &&
                            (counts[Suit.Hearts] == 4 || counts[Suit.Spades] == 4));
			    };
		    }
        }
   

        public static void OpenerRebid(InterpretedBid rebid, NoTrump nt)
        {
            if (rebid.Is(nt.BidLevel + 1, Suit.Diamonds))
            {
                rebid.HandShape[Suit.Hearts].Max = 3;
                rebid.HandShape[Suit.Spades].Max = 3;
                rebid.Description = "No 4+ card major";
                rebid.PartnersCall = c => ResponderRebid(c, nt, Suit.Unknown);
            }
            // TODO: How to bid if 5/4?  This just takes care of 4/4
            if (rebid.Is(nt.BidLevel + 1, Suit.Hearts))
            {
                rebid.HandShape[Suit.Hearts].Min = 4;
                rebid.Description = "4+ hearts";
                rebid.PartnersCall = c => ResponderRebid(c, nt, Suit.Hearts);
            }
            if (rebid.Is(nt.BidLevel + 1, Suit.Spades))
            {
                rebid.HandShape[Suit.Hearts].Max = 3;
                rebid.HandShape[Suit.Spades].Min = 4;
                rebid.Description = "4+ spades";
                rebid.PartnersCall = c => ResponderRebid(c, nt, Suit.Spades);
			}
		}

        static void ResponderRebid(InterpretedBid rebid, NoTrump ntInfo, Suit openerMajor)
        {
            if (openerMajor == Suit.Unknown)
            {
                Show5CardMajor(rebid, ntInfo, Suit.Hearts);
                Show5CardMajor(rebid, ntInfo, Suit.Spades);
                if (rebid.Is(2, Suit.Unknown))
                {
                    rebid.SetPoints(ntInfo.ResponderInvitationalPoints);
                    rebid.PartnersCall = c => PlaceContract(c, ntInfo, Suit.Unknown); 
                }
                if (rebid.Is(3, Suit.Unknown))
                {
                    rebid.SetPoints(ntInfo.ResponderGamePoints);
                    rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
				}
                return;
            }
            
            // If we get to here then openerMajor is not Unknown - It's hearts or spades
            if (rebid.Is(2, Suit.Unknown))
            {
                // If we don't have 4+ in opener's major then NT invitation
                rebid.HandShape[openerMajor].Max = 3;
                rebid.SetPoints(ntInfo.ResponderInvitationalPoints);
                rebid.PartnersCall = c => PlaceContract(c, ntInfo, Suit.Unknown);
                return;
            }

            if (rebid.Is(3, Suit.Unknown))
            {
                rebid.SetPoints(ntInfo.ResponderGamePoints);
                rebid.HandShape[openerMajor].Max = 3;
                rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
                return;
            }

			if (rebid.Is(3, openerMajor))
            {
				rebid.HandShape[openerMajor].Min = 4;
                // TODO: Need to fix "dummy" point analysis.  This works most of the time in this situation but
                // it really needs to know the trump suit or can't evaluate correctly
				rebid.SetPoints(ntInfo.ResponderInvitationalPoints, BidPointType.Dummy);
				rebid.PartnersCall = c => PlaceContract(c, ntInfo, openerMajor);
				return;
			}

			if (rebid.Is(4, openerMajor))
			{
				rebid.HandShape[openerMajor].Min = 4;
				rebid.SetPoints(ntInfo.ResponderGamePoints, BidPointType.Dummy);
				rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
				return;
			}
		}

        // TODO: This function shows a 5-card major in the case of 5/4 majors.  It does NOT implement Smolen.
        // Perhaps we need to add conventions to the NoTrump class.  If Smolen is true then would bid the
        // other major here.  For now, natural bids.
		private static void Show5CardMajor(InterpretedBid call, NoTrump nt, Suit major)
		{
			if (call.Is(3, major))
			{
				call.SetPoints(nt.ResponderGameOrBetterPoints);
				call.HandShape[major].Min = 5;
				call.Description = $"Show 5 {major} and force to game";
				call.PartnersCall = c => OpenerRespondTo5CardMajor(c, nt, major);
			}
		}

        public static void OpenerRespondTo5CardMajor(InterpretedBid rebid, NoTrump ntInfo, Suit major)
        {
			if (rebid.Is(3, Suit.Unknown))
			{
				rebid.HandShape[major].Max = 2;
				rebid.SetPoints(ntInfo.OpenerPoints);   // TODO: Again what makes this match?
				rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
			}
			if (rebid.Is(4, major))
			{
				rebid.HandShape[major].Min = 3;
				rebid.SetPoints(ntInfo.OpenerPoints);   // TODO: Again what makes this match?
				rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
			}
		}

		// Invitation to game by responder.  Now place the contract
		static void PlaceContract(InterpretedBid rebid, NoTrump nt, Suit trumpSuit)
        {
            if (trumpSuit == Suit.Unknown)
            {
                if (rebid.Is(3, Suit.Unknown))
                {
                    rebid.SetPoints(nt.OpenerAcceptInvitePoints);
                    rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
                }
                if (rebid.Is(3, Suit.Spades))
                {
                    rebid.HandShape[Suit.Hearts].Min = 4;
                    rebid.HandShape[Suit.Spades].Min = 4;
                    rebid.SetPoints(nt.OpenerRejectInvitePoints);
                    rebid.PartnersCall = c => ReevaluateAsSpadeDummy(c, nt);
				}
                return;
            }
            if (rebid.Is(4, trumpSuit))
            {
                rebid.SetPoints(nt.OpenerAcceptInvitePoints);
                rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
            }
        }

        static void ReevaluateAsSpadeDummy(InterpretedBid rebid, NoTrump nt)
        {
            if (rebid.Is(4, Suit.Spades))
            {
                rebid.SetPoints(nt.ResponderGamePoints, BidPointType.Dummy);
                rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
            }
        }
    }
}