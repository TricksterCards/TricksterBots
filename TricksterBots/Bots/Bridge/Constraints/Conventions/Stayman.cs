using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class StaymanBidder : OneNoTrumpBidder
	{
 
        public StaymanBidder(NTType type) : base(type) { }

        public static BidRulesFactory InitiateConvention(NTType type)
        {
            return new StaymanBidder(type).Initiate;
        }

		private IEnumerable<BidRule> Initiate(PositionState ps)
		{
            Bid bid = ps.RightHandOpponent.GetBidHistory(0).IsBid ? Bid.Double : new Bid(2, Suit.Clubs);
            return new BidRule[] {
                DefaultPartnerBids(Bid.Double, Answer),
                Forcing(bid, Points(ResponderRange.InviteOrBetter), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 0, 4), Flat(false), ShowsNoSuit()),
                Forcing(bid, Points(ResponderRange.InviteOrBetter), Shape(Suit.Spades, 4), Shape(Suit.Hearts, 0, 4), Flat(false), ShowsNoSuit()),
                Forcing(bid, Points(ResponderRange.GameOrBetter), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 5), ShowsNoSuit()),
                Forcing(bid, Points(ResponderRange.GameOrBetter), Shape(Suit.Hearts, 5), Shape(Suit.Spades, 4), ShowsNoSuit())
                // TODO: The following rule is "Garbage Stayman"
                ///	Forcing(2, Suit.Clubs, Points((0, 7)), Shape(Suit.Diamonds, 4, 5), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 4)),
            };
            // TODO: Need to add rules for garbage stayman if that is on, and for 4-way transfers if that is on...
		}

       
        public IEnumerable<BidRule> Answer(PositionState ps)
		{
            return new BidRule[] {
                DefaultPartnerBids(Bid.Double, Explain),
				// TODO: Are these bids truly forcing?  Not if garbage stayman...
				Forcing(2, Suit.Diamonds, Shape(Suit.Hearts, 0, 3), Shape(Suit.Spades, 0, 3), ShowsNoSuit()),

				// If we are 4-4 then hearts bid before spades.  Can't be 5-5 or wouldn't be balanced.
				Forcing(2, Suit.Hearts, Shape(4, 5), LongerOrEqualTo(Suit.Spades)),

                Forcing(2, Suit.Spades, Shape(4, 5), LongerThan(Suit.Hearts))
            };
        }

        public IEnumerable<BidRule> Explain(PositionState _)
        {
            return new BidRule[] {
                DefaultPartnerBids(Bid.Double, PlaceContract), 
                // If we have a 5 card suit and want to go to game then show that suit.
                Forcing(3, Suit.Spades, Points(ResponderRange.GameOrBetter), Shape(5), Partner(LastBid(2, Suit.Diamonds))),
                Forcing(3, Suit.Hearts, Points(ResponderRange.GameOrBetter), Shape(5), Partner(LastBid(2, Suit.Diamonds))),


				// These show invitational 5/4
                Invitational(2, Suit.Hearts, Points(ResponderRange.Invite), Shape(5), Partner(LastBid(2, Suit.Diamonds))),
                Invitational(2, Suit.Spades, Points(ResponderRange.Invite), Shape(5), Partner(LastBid(2, Suit.Diamonds))),

                Invitational(2, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Diamonds))),
                Invitational(2, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 0, 3)),
                Invitational(2, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 0, 3)),


                Invitational(3, Suit.Hearts, ShowsTrump(), DummyPoints(ResponderRange.Invite), Partner(LastBid(2, Suit.Hearts)), Shape(4, 5)),
                Invitational(3, Suit.Spades, ShowsTrump(), DummyPoints(ResponderRange.Invite), Partner(LastBid(2, Suit.Spades)), Shape(4, 5)),


                // Prioritize suited contracts over 3NT bid by placing these rules first...
                Signoff(4, Suit.Hearts, ShowsTrump(), DummyPoints(ResponderRange.Game), Partner(LastBid(2, Suit.Hearts)), Shape(4, 5)),
                Signoff(4, Suit.Spades, ShowsTrump(), DummyPoints(ResponderRange.Game), Partner(LastBid(2, Suit.Spades)), Shape(4, 5)), 

                // TODO: After changeover is done an tests are working again, change all of these rules to simply
                // Signoff(3, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Game), Fit(Suit.Hearts, false), Fit(Suit.Spades, false)),
                Signoff(3, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Game), Partner(LastBid(2, Suit.Diamonds))),
                Signoff(3, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Game), Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 0, 3)),
                Signoff(3, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Game), Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 0, 3)),

            };
        }


        public IEnumerable<BidRule> PlaceContract(PositionState _)
        {
            return new BidRule[] {
				// These rules deal with a 5/4 invitational that we want to reject.  Leave contract in bid suit
				// if we have 3.  Otherwise put in NT
				Signoff(Bid.Pass, Points(OpenerRange.DontAcceptInvite),
                                    Fit(Suit.Hearts), Partner(LastBid(2, Suit.Hearts))),
                Signoff(Bid.Pass, Points(OpenerRange.DontAcceptInvite),
                                    Fit(Suit.Spades), Partner(LastBid(2, Suit.Spades))),

                Signoff(2, Suit.Unknown, Points(OpenerRange.DontAcceptInvite)),

                PartnerBids(3, Suit.Spades, Bid.Double, CheckSpadeGame),
                Nonforcing(3, Suit.Spades, Points(OpenerRange.DontAcceptInvite), Shape(4), Partner(HasShape(4))),

                Signoff(3, Suit.Unknown, Points(OpenerRange.AcceptInvite), Partner(LastBid(2, Suit.Unknown))),
                Signoff(3, Suit.Unknown, LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Hearts)),
                            Shape(Suit.Hearts, 2)),
                Signoff(3, Suit.Unknown, Points(OpenerRange.AcceptInvite), LastBid(2, Suit.Diamonds),
                                    Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 2)),
                Signoff(3, Suit.Unknown,  LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Spades)),
                            Shape(Suit.Spades, 2)),
                Signoff(3, Suit.Unknown, Points(OpenerRange.AcceptInvite), LastBid(2, Suit.Diamonds),
                        Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 2)),



                Signoff(4, Suit.Hearts, Points(OpenerRange.AcceptInvite), Fit()),
               //TODO: I think above rule ocvers itl.. Signoff(4, Suit.Hearts, LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Hearts)), Shape(3)),


                Signoff(4, Suit.Spades, Points(OpenerRange.AcceptInvite), Partner(LastBid(3, Suit.Spades)), Fit()),
                Signoff(4, Suit.Spades, Points(OpenerRange.AcceptInvite), Fit()),
                Signoff(4, Suit.Spades, Partner(LastBid(3, Suit.Unknown)), Fit()),
                Signoff(4, Suit.Spades, LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Spades)), Shape(3))
            };
        }

    	public IEnumerable<BidRule> CheckSpadeGame(PositionState _)
        {
            return new BidRule[] {
                Signoff(4, Suit.Spades, ShowsTrump(), DummyPoints(ResponderRange.Game), Partner(LastBid(3, Suit.Spades)), Shape(4, 5))
            };
		}
	}


    //*********************************************************************************************

    // TODO: Maybe move thse 2NT stayman...
    public class Stayman2NT: TwoNoTrumpBidder
    {

        public static IEnumerable<BidRule> InitiateConvention(PositionState ps) 
        {
            Bid bid = ps.RightHandOpponent.GetBidHistory(0).IsBid ? Bid.Double : new Bid(3, Suit.Clubs);
            return new BidRule[] {
                PartnerBids(bid, Bid.Double, Answer),
                Forcing(bid, RespondGame, Shape(Suit.Hearts, 4), Flat(false)),
                Forcing(bid, RespondGame, Shape(Suit.Spades, 4), Flat(false)),
                Forcing(bid, RespondGame, Shape(Suit.Hearts, 4), Shape(Suit.Spades, 5)),
                Forcing(bid, RespondGame, Shape(Suit.Hearts, 5), Shape(Suit.Spades, 4))
                // TODO: The following rule is "Garbage Stayman"
                //Forcing(2, Suit.Clubs, Points(NTLessThanInvite), Shape(Suit.Diamonds, 4, 5), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 4)),
            };
        }
        public static IEnumerable<BidRule> Answer(PositionState _)
        {
            return new BidRule[] {
                DefaultPartnerBids(Bid.Double, ResponderRebid),

                Forcing(3, Suit.Diamonds, Shape(Suit.Hearts, 0, 3), Shape(Suit.Spades, 0, 3)),

                // If we are 4-4 then hearts bid before spades.  Can't be 5-5 or wouldn't be balanced.
                Forcing(3, Suit.Hearts, Shape(4, 5), LongerOrEqualTo(Suit.Spades)),
                Forcing(3, Suit.Spades, Shape(4, 5), LongerThan(Suit.Hearts))
            };
        }

        public static IEnumerable<BidRule> ResponderRebid(PositionState _)
        {
            return new BidRule[] {
                DefaultPartnerBids(Bid.Double, OpenerRebid),

                Forcing(3, Suit.Hearts, Shape(5), Partner(LastBid(3, Suit.Diamonds))),
                Forcing(3, Suit.Spades, Shape(5), Partner(LastBid(3, Suit.Diamonds))),

                Signoff(3, Suit.Unknown, Fit(Suit.Hearts, false), Fit(Suit.Spades, false)),
                Signoff(4, Suit.Hearts, Fit()),
                Signoff(4, Suit.Spades, Fit())
            };
        }
    
        public static IEnumerable<BidRule> OpenerRebid(PositionState _)
        { 
            return new BidRule[] {
                Signoff(3, Suit.Unknown, Fit(Suit.Hearts, false), Fit(Suit.Spades, false)),
                Signoff(4, Suit.Hearts, Fit()),
                Signoff(4, Suit.Spades, Fit())
            };
        }
    }

}
