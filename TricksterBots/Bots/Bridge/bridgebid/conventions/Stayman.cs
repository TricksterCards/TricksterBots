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
            // TODO: Double for stayman if 2C bid by opponenets.  Perhaps pass in boolean from NoTrump class...
            if (response.Is(nt.Level + 1, Suit.Clubs))
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
                    // TODO: If using 4-way transfers we bid this no matter what with invitational points.  Spade
                    // assumptions are not valid if hearts bid by opener if responder invites with 2NT.
			    };
		    }
        }
   

        public static void OpenerRebid(InterpretedBid rebid, NoTrump nt)
        {
            if (rebid.Is(nt.Level + 1, Suit.Diamonds))
            {
                rebid.HandShape[Suit.Hearts].Max = 3;
                rebid.HandShape[Suit.Spades].Max = 3;
                rebid.Description = "No 4+ card major";
                rebid.PartnersCall = c => Responder2ndBid(c, nt, Suit.Diamonds);
            }
            else if (rebid.Is(nt.Level + 1, Suit.Hearts))
            {
                rebid.HandShape[Suit.Hearts].Min = 4;
                rebid.Description = "4+ hearts";
                rebid.PartnersCall = c => Responder2ndBid(c, nt, Suit.Hearts);
            }
            else if (rebid.Is(nt.Level + 1, Suit.Spades))
            {
                rebid.HandShape[Suit.Hearts].Max = 3;
                rebid.HandShape[Suit.Spades].Min = 4;
                rebid.Description = "4+ spades, less than 4 hearts";
                rebid.PartnersCall = c => Responder2ndBid(c, nt, Suit.Spades);
			}
		}

        static void Responder2ndBid(InterpretedBid rebid, NoTrump nt, Suit openerBidSuit)
        {
            if (openerBidSuit == Suit.Diamonds)
            {
                foreach (Suit major in BasicBidding.MajorSuits)
                {
                    if (rebid.Is(3, nt.UseSmolen ? BasicBidding.OtherMajor(major) : major))
                    {
                        rebid.SetPoints(nt.ResponderGameOrBetterPoints);
                        rebid.SetHandShape(major, 5);
                        rebid.Description = $"Show 5 {major} and force to game";
                        rebid.PartnersCall = c => OpenerRespondTo5CardMajor(c, nt, major);
                    }
                }
                if (rebid.Is(2, Suit.Unknown))
                {
                    rebid.SetPoints(nt.ResponderInvitationalPoints);
                    rebid.PartnersCall = c => PlaceContract(c, nt, Suit.Unknown, false);
                }
                if (rebid.Is(3, Suit.Unknown))
                {
                    rebid.SetPoints(nt.ResponderGamePoints);
                    rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
                }
                // TODO: Slam bids here
            }
            else    // In all cases below opener has bid a major
            {
                if (rebid.Is(2, Suit.Unknown))
                {
                    // If we don't have 4+ in opener's major then NT invitation
                    rebid.HandShape[openerBidSuit].Max = 3;
                    rebid.SetPoints(nt.ResponderInvitationalPoints);
                    rebid.PartnersCall = c => PlaceContract(c, nt, Suit.Unknown, false);
                }
                else if (rebid.Is(3, openerBidSuit))
                {
                    rebid.HandShape[openerBidSuit].Min = 4;
                    // TODO: Need to fix "dummy" point analysis.  This works most of the time in this situation but
                    // it really needs to know the trump suit or can't evaluate correctly
                    rebid.SetPoints(nt.ResponderInvitationalPoints, BidPointType.Dummy);
                    rebid.PartnersCall = c => PlaceContract(c, nt, openerBidSuit, false);
                }
                else if (rebid.Is(3, Suit.Unknown))
                {
                    rebid.SetPoints(nt.ResponderGamePoints);
                    rebid.HandShape[openerBidSuit].Max = 3;
                    rebid.PartnersCall = c => PlaceContract(c, nt, Suit.Unknown, true);
                }
                else if (rebid.Is(4, openerBidSuit))
                {
                    rebid.HandShape[openerBidSuit].Min = 4;
                    rebid.SetPoints(nt.ResponderGamePoints, BidPointType.Dummy);
                    rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
                }
                // TODO: Slam bids.....  
            }
		}


        // Responder has shown 5/4 in the majors after opener denied a 4-card major.  Place the contract in game
        // in NT or responder's 5-card suit.
        public static void OpenerRespondTo5CardMajor(InterpretedBid rebid, NoTrump nt, Suit major)
        {
            // TODO: Interference.  We want to get to game here.  
            // We do not reject 4-card major if 4333 in this case since responder is 4/5.
			if (rebid.Is(3, Suit.Unknown))
			{
				rebid.HandShape[major].Max = 2;
				rebid.SetPoints(nt.OpenerPoints);   // TODO: Again what makes this match?
				rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
			}
			else if (rebid.Is(4, major))
			{
				rebid.HandShape[major].Min = 3;
				rebid.SetPoints(nt.OpenerPoints);   // TODO: Again what makes this match?
				rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
			}
		}

		// 2nd bid by opener in this sequence.  
		static void PlaceContract(InterpretedBid rebid, NoTrump nt, Suit trumpSuit, bool responderBidGame)
        {
            if (trumpSuit == Suit.Unknown)
            {
                if (rebid.Is(3, Suit.Unknown))
                {
                    rebid.SetPoints(nt.OpenerAcceptInvitePoints);
                    rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
                }
                else if (rebid.Is(3, Suit.Spades))
                {
                    rebid.HandShape[Suit.Hearts].Min = 4;
                    rebid.HandShape[Suit.Spades].Min = 4;
                    rebid.SetPoints(nt.OpenerRejectInvitePoints);
                    rebid.PartnersCall = c => ReEvaluateAsSpadeDummy(c, nt);
				}  
                else if (rebid.Is(4, Suit.Spades))
				{
                    // TODO: Set priority here so this bid is selected ahead of 3NT?  
                    rebid.Priority = 100;   // IS THIS A GOOD IDEA?  Tony?
					rebid.HandShape[Suit.Hearts].Min = 4;
					rebid.HandShape[Suit.Spades].Min = 4;
					rebid.SetPoints(responderBidGame ? nt.OpenerPoints : nt.OpenerAcceptInvitePoints);
					rebid.PartnersCall = c => ReEvaluateAsSpadeDummy(c, nt);
				}
            }
            else if (rebid.Is(4, trumpSuit))
            {
                rebid.SetPoints(nt.OpenerAcceptInvitePoints);
                rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
            }
        }

        static void ReEvaluateAsSpadeDummy(InterpretedBid rebid, NoTrump nt)
        {
            if (rebid.Is(4, Suit.Spades))
            {
                rebid.SetPoints(nt.ResponderGamePoints, BidPointType.Dummy);
                rebid.PartnersCall = CompetitiveAuction.PassOrCompete;
            }
            // TODO: Could upgrade to slam points here so need to go to slam.  Blackwood is valid at this point
            // since spades agreed on.  
        }
    }
}