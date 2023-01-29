using System.CodeDom;
using System.Security.Cryptography;
using System.Threading;
using Trickster.cloud;
using TricksterBots.Bots;
using static TricksterBots.Bots.NoTrump;


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
                nt.Forcing(response, HandRange.ResponderGameInvitationalOrBetter, c => OpenerRebid(c, nt),
                    "asking for a major");
			    response.Priority = 100; // always prefer Stayman over other bids when valid
			    response.Validate = hand =>
			    {
					//  we should have 4H or 4S (any more and we'll use a transfer instead)
					// TODO: Need to take 6/4 into account?  5/4 will happen after opener denies 4-card major
					// Never bid stayman with 4333 - NOTE: This is not true with 4-way transfers
					// TODO: If using 4-way transfers we bid this no matter what with invitational points.  Spade
					// assumptions are not valid if hearts bid by opener if responder invites with 2NT.
   				    var counts = BasicBidding.CountsBySuit(hand);
					return ((!BasicBidding.Is4333(counts)) &&
                            (counts[Suit.Hearts] == 4 || counts[Suit.Spades] == 4));

			    };
		    }
        }
   

        public static void OpenerRebid(InterpretedBid rebid, NoTrump nt)
        {
            if (rebid.Is(nt.Level + 1, Suit.Diamonds))
            {
                rebid.HandShape[Suit.Hearts].Max = 3;
                rebid.HandShape[Suit.Spades].Max = 3;
                nt.Forcing(rebid, HandRange.OpenerAll, c => Responder2ndBid(c, nt, Suit.Diamonds), "No 4+ card major");
            }
            else if (rebid.Is(nt.Level + 1, Suit.Hearts))
            {
                rebid.HandShape[Suit.Hearts].Min = 4;
				nt.Forcing(rebid, HandRange.OpenerAll, c => Responder2ndBid(c, nt, Suit.Hearts), "4+hearts");
			}
            else if (rebid.Is(nt.Level + 1, Suit.Spades))
            {
                rebid.HandShape[Suit.Hearts].Max = 3;
                rebid.HandShape[Suit.Spades].Min = 4;
				nt.Forcing(rebid, HandRange.OpenerAll, c => Responder2ndBid(c, nt, Suit.Spades), "4+spades, less than 4 hearts");
			}
		}

        static void Responder2ndBid(InterpretedBid rebid, NoTrump nt, Suit openerBidSuit)
        {
            if (openerBidSuit == Suit.Diamonds)
            {
                foreach (Suit major in BasicBidding.MajorSuits)
                {
                    if (rebid.Is(2, major))
                    {
                        rebid.SetHandShape(major, 5);
                        nt.Invitational(rebid, HandRange.ResponderGameInvitational,
                            c => OpenerRespondTo5CardMajor(c, nt, major, HandRange.OpenerAcceptInvitation),
                            $"Show 5 {major} game invitational");
                    }
                    if (rebid.Is(3, nt.UseSmolen ? BasicBidding.OtherMajor(major) : major))
                    {
                        rebid.SetHandShape(major, 5);
                        nt.Forcing(rebid, HandRange.ResponderGameOrBetter,
                            c => OpenerRespondTo5CardMajor(c, nt, major, HandRange.OpenerAll),
                           $"Show 5 {major} and force to game");
                    }
                }
                if (rebid.Is(2, Suit.Unknown))
                {
                    nt.Invitational(rebid, HandRange.ResponderGameInvitational, c => PlaceContract(c, nt, Suit.Unknown, false));
                }
                else if (rebid.Is(3, Suit.Unknown))
                {
                    nt.Signoff(rebid, HandRange.ResponderGame);
                }
                else
                {
                    nt.InterpretSlamBids(rebid);
                }
            }
            else    // In all cases below opener has bid a major
            {
                if (rebid.Is(2, Suit.Unknown))
                {
                    // If we don't have 4+ in opener's major then NT invitation
                    rebid.HandShape[openerBidSuit].Max = 3;
                    nt.Invitational(rebid, HandRange.ResponderGameInvitational,
                        c => PlaceContract(c, nt, Suit.Unknown, false));
                }
                else if (rebid.Is(3, openerBidSuit))
                {
                    rebid.HandShape[openerBidSuit].Min = 4;
                    nt.Invitational(rebid, HandRange.ResponderGameInvitational,
                            c => PlaceContract(c, nt, openerBidSuit, false));
					// TODO: Need to fix "dummy" point analysis.  This works most of the time in this situation but
					// it really needs to know the trump suit or can't evaluate correctly
					rebid.BidPointType = BidPointType.Dummy;
				}
                else if (rebid.Is(3, Suit.Unknown))
                {
                    rebid.HandShape[openerBidSuit].Max = 3;
                    nt.NonForcing(rebid, HandRange.ResponderGame, c => PlaceContract(c, nt, Suit.Unknown, true));
                }
                else if (rebid.Is(4, openerBidSuit))
                {
                    rebid.HandShape[openerBidSuit].Min = 4;
                    nt.Signoff(rebid, HandRange.ResponderGame);
                    rebid.BidPointType = BidPointType.Dummy;        // TODO: Better dummy analysis...
				}
                // TODO: Slam bids.....  
            }
		}


        // Responder has shown 5/4 in the majors after opener denied a 4-card major.  Place the contract in game
        // in NT or responder's 5-card suit.
        public static void OpenerRespondTo5CardMajor(InterpretedBid rebid, NoTrump nt, Suit major, NoTrump.HandRange acceptRange)
        {
			// TODO: Interference.  We want to get to game here.  
			if (rebid.Is(2, Suit.Unknown))
			{
				rebid.HandShape[major].Max = 2;
				nt.Signoff(rebid, HandRange.OpenerMinimum);
			}
			else if (rebid.Is(3, Suit.Unknown))
			{
				rebid.HandShape[major].Max = 2;
				nt.Signoff(rebid, acceptRange);
			}
			else if (rebid.Is(4, major))
			{
				rebid.HandShape[major].Min = 3;
				nt.Signoff(rebid, acceptRange);
			}
		}

		// 2nd bid by opener in this sequence.  
		static void PlaceContract(InterpretedBid rebid, NoTrump nt, Suit trumpSuit, bool responderBidGame)
        {
            if (trumpSuit == Suit.Unknown)
            {
                if (rebid.Is(3, Suit.Unknown))
                {
                    nt.Signoff(rebid, HandRange.OpenerAcceptInvitation);
                }
                else if (rebid.Is(3, Suit.Spades))
                {
                    rebid.HandShape[Suit.Hearts].Min = 4;
                    rebid.HandShape[Suit.Spades].Min = 4;
                    nt.NonForcing(rebid, HandRange.OpenerMinimum, c => ReEvaluateAsSpadeDummy(c, nt));
				}  
                else if (rebid.Is(4, Suit.Spades))
				{
                    rebid.Priority = 100;   // Make sure this is bid instead of 3NT - TODO: Is this necessary?
					rebid.HandShape[Suit.Hearts].Min = 4;
					rebid.HandShape[Suit.Spades].Min = 4;
                    nt.NonForcing(rebid, responderBidGame ? HandRange.OpenerAll : HandRange.OpenerAcceptInvitation,
                        c => ReEvaluateAsSpadeDummy(c, nt));
				}
            }
            else if (rebid.Is(4, trumpSuit))
            {
                nt.Signoff(rebid, HandRange.OpenerAcceptInvitation);
            }
        }

        static void ReEvaluateAsSpadeDummy(InterpretedBid rebid, NoTrump nt)
        {
            if (rebid.Is(4, Suit.Spades))
            {
                nt.Signoff(rebid, HandRange.ResponderGame);
                rebid.BidPointType = BidPointType.Dummy;    // TODO: This is ugly.  Need to do better...
            }
            // TODO: Could upgrade to slam points here so need to go to slam.  Blackwood is valid at this point
            // since spades agreed on.  
        }
    }
}