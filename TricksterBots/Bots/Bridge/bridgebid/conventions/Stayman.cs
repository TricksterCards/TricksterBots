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
        public static void InitiateStayman(InterpretedBid response, NTFundamentals ntInfo)
        {
            // TODO: Do I worrry about interference here or has caller already done that?
            // TODO: Should this be an assert that the bid is xC?  Or do I check for it an just return
            // if it isn't
            if (response.Is(ntInfo.BidLevel + 1, Suit.Clubs))
            {
			    response.BidConvention = BidConvention.Stayman;
			    response.BidMessage = BidMessage.Forcing;
                response.Points.Min = ntInfo.ResponderInvitationalPoints.Min;
			    response.Description = "asking for a major";
			    response.Priority = 100; // always prefer Stayman over other bids when valid
                response.PartnersCall = c => OpenerRebid(c, ntInfo);
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
   

        public static void OpenerRebid(InterpretedBid rebid, NTFundamentals ntInfo)
        {
            if (rebid.Is(ntInfo.BidLevel + 1, Suit.Diamonds))
            {
                rebid.HandShape[Suit.Hearts].Max = 3;
                rebid.HandShape[Suit.Spades].Max = 3;
                rebid.Description = "No 4+ card major";
                rebid.PartnersCall = c => ResponderRebid(c, ntInfo, Suit.Unknown);
                return;
            }
            // TODO: How to bid if 5/4?  This just takes care of 4/4
            if (rebid.Is(ntInfo.BidLevel + 1, Suit.Hearts))
            {
                rebid.HandShape[Suit.Hearts].Min = 4;
                rebid.Description = "4+ hearts";
                rebid.PartnersCall = c => ResponderRebid(c, ntInfo, Suit.Hearts);
				return;
            }
            if (rebid.Is(ntInfo.BidLevel + 1, Suit.Spades))
            {
                rebid.HandShape[Suit.Hearts].Max = 3;
                rebid.HandShape[Suit.Spades].Min = 4;
                rebid.Description = "4+ spades";
                rebid.PartnersCall = c => ResponderRebid(c, ntInfo, Suit.Spades);
			}
		}

        static void ResponderRebid(InterpretedBid rebid, NTFundamentals ntInfo, Suit openerMajor)
        {
            if (openerMajor == Suit.Unknown)
            {
                // TODO: Bid 5-card suit...  But later
                if (rebid.Is(2, Suit.Unknown))
                {
                    rebid.SetHighCardPoints(ntInfo.ResponderInvitationalPoints);
                    rebid.PartnersCall = c => PlaceContract(c, ntInfo, Suit.Unknown); 
                    return; 
                }
                if (rebid.Is(3, Suit.Unknown))
                {
                    rebid.SetHighCardPoints(ntInfo.ResponderGamePoints);
                    rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
                    return;
				}
                return;
            }
            
            // If we get to here then openerMajor is not Unknown - It's hearts or spades
            if (rebid.Is(2, Suit.Unknown))
            {
                // If we don't have 4+ in opener's major then NT invitation
                rebid.HandShape[openerMajor].Max = 3;
                rebid.SetHighCardPoints(ntInfo.ResponderInvitationalPoints);
                rebid.PartnersCall = c => PlaceContract(c, ntInfo, Suit.Unknown);
                return;
            }

            if (rebid.Is(3, Suit.Unknown))
            {
                rebid.SetHighCardPoints(ntInfo.ResponderGamePoints);
                rebid.HandShape[openerMajor].Max = 3;
                rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
                return;
            }

			if (rebid.Is(3, openerMajor))
            {
				rebid.HandShape[openerMajor].Min = 4;
				rebid.SetHighCardPoints(ntInfo.ResponderInvitationalPoints);
                rebid.BidPointType = BidPointType.Dummy;    // TODO: This is BAD - needs trump suit
				rebid.PartnersCall = c => PlaceContract(c, ntInfo, openerMajor);
				return;
			}

			if (rebid.Is(4, openerMajor))
			{
				rebid.HandShape[openerMajor].Min = 4;
				rebid.SetHighCardPoints(ntInfo.ResponderGamePoints);
                rebid.BidPointType = BidPointType.Dummy;    // TODO: This is probably right
				rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
				return;
			}
		}

        // Invitaation to game by responder.  Now place the contract
        static void PlaceContract(InterpretedBid rebid, NTFundamentals ntInfo, Suit trumpSuit)
        {
            if (trumpSuit == Suit.Unknown)
            {
                if (rebid.Is(3, Suit.Unknown))
                {
                    rebid.SetHighCardPoints(ntInfo.OpenerAcceptInvitePoints);
                    rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
                    return;
                }
                if (rebid.Is(3, Suit.Spades))
                {
                    rebid.HandShape[Suit.Hearts].Min = 4;
                    rebid.HandShape[Suit.Hearts].Min = 4;
                    rebid.SetHighCardPoints(ntInfo.OpenerPoints.Min, ntInfo.OpenerAcceptInvitePoints.Min - 1);
                    rebid.PartnersCall = c => ReevaluateAsSpadeDummy(c, ntInfo);
				}

            }
            if (rebid.Is(4, trumpSuit))
            {
                rebid.SetHighCardPoints(ntInfo.OpenerAcceptInvitePoints);
                rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
            }
        }

        static void ReevaluateAsSpadeDummy(InterpretedBid rebid, NTFundamentals ntInfo)
        {
            if (rebid.Is(4, Suit.Spades))
            {
                rebid.SetHighCardPoints(ntInfo.ResponderGamePoints);
                rebid.BidPointType = BidPointType.Dummy;
                rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
            }
        }
    }
}