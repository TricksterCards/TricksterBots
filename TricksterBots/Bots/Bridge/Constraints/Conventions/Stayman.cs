using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class StaymanBidder : OneNoTrumpBidder
	{
        private StaymanBidder(NTType type) : base(type, Convention.Stayman, 1000)
        {
        }

        public static PrescribedBids InitiateConvention(NTType type)
        {
            var staymanBidder = new StaymanBidder(type);
            return new PrescribedBids(staymanBidder, staymanBidder.Initiate);
        }

		public void Initiate(PrescribedBids pb)
		{
	        pb.Bids = new BidRule[]
			{
                Forcing(2, Suit.Clubs, Points(ResponderRange.InviteOrBetter), Shape(Suit.Hearts, 4), Flat(false)),
                Forcing(2, Suit.Clubs, Points(ResponderRange.InviteOrBetter), Shape(Suit.Spades, 4), Flat(false)),
                Forcing(2, Suit.Clubs, Points(ResponderRange.GameOrBetter), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 5)),
                Forcing(2, Suit.Clubs, Points(ResponderRange.GameOrBetter), Shape(Suit.Hearts, 5), Shape(Suit.Spades, 4)),
				// TODO: The following rule is "Garbage Stayman"
				//Forcing(2, Suit.Clubs, Points(NTLessThanInvite), Shape(Suit.Diamonds, 4, 5), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 4)),
			};
            pb.Partner(Answer);

		}
        public void Answer(PrescribedBids pb)
		{ 
            pb.Bids = new BidRule[]
            {
				// TODO: Are these bids truly forcing?  Not if garbage stayman...
				Forcing(2, Suit.Diamonds, Shape(Suit.Hearts, 0, 3), Shape(Suit.Spades, 0, 3)),

				// If we are 4-4 then hearts bid before spades.  Can't be 5-5 or wouldn't be balanced.
				Forcing(2, Suit.Hearts, Shape(4, 5), LongerOrEqualTo(Suit.Spades)),

                Forcing(2, Suit.Spades, Shape(4, 5), LongerThan(Suit.Hearts)),
            };
            pb.Partner(Explain);
        }

        public void Explain(PrescribedBids pb)
        {
            pb.Bids = new BidRule[]
            {
				// These show invitational 5/4
                Invitational(2, Suit.Hearts, Points(ResponderRange.Invite), Shape(5), Partner(LastBid(2, Suit.Diamonds))),
                Invitational(2, Suit.Spades, Points(ResponderRange.Invite), Shape(5), Partner(LastBid(2, Suit.Diamonds))),

                Invitational(2, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Diamonds))),
                Invitational(2, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 0, 3)),
                Invitational(2, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 0, 3)),


                Invitational(3, Suit.Hearts, ShowsTrump(), DummyPoints(ResponderRange.Invite), Partner(LastBid(2, Suit.Hearts)), Shape(4, 5)),
                Forcing(3, Suit.Hearts, DefaultPriority + 10, Points(ResponderRange.GameOrBetter), Shape(5), Partner(LastBid(2, Suit.Diamonds))),


                Invitational(3, Suit.Spades, ShowsTrump(), DummyPoints(ResponderRange.Invite), Partner(LastBid(2, Suit.Spades)), Shape(4, 5)),
                Forcing(3, Suit.Spades, DefaultPriority + 10, Points(ResponderRange.GameOrBetter), Shape(5), Partner(LastBid(2, Suit.Diamonds))),

                // TODO: After changeover is done an tests are working again, change all of these rules to simply
                // Signoff(3, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Game), Fit(Suit.Hearts, false), Fit(Suit.Spades, false)),
                Signoff(3, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Game), Partner(LastBid(2, Suit.Diamonds))),
                Signoff(3, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Game), Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 0, 3)),
                Signoff(3, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Game), Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 0, 3)),

                Signoff(4, Suit.Hearts, ShowsTrump(), DummyPoints(ResponderRange.Game), Partner(LastBid(2, Suit.Hearts)), Shape(4, 5)),

                Signoff(4, Suit.Spades, ShowsTrump(), DummyPoints(ResponderRange.Game), Partner(LastBid(2, Suit.Spades)), Shape(4, 5))
            };
            pb.Partner(PlaceContract);
        }


	    public void PlaceContract(PrescribedBids pb)
        {
			pb.Bids = new BidRule[]
			{
				// These rules deal with a 5/4 invitational that we want to reject.  Leave contract in bid suit
				// if we have 3.  Otherwise put in NT
				Signoff(Call.Pass, DefaultPriority + 10, Points(OpenerRange.DontAcceptInvite), 
									Fit(Suit.Hearts), Partner(LastBid(2, Suit.Hearts))),
                Signoff(Call.Pass, DefaultPriority + 10, Points(OpenerRange.DontAcceptInvite),
                                    Fit(Suit.Spades), Partner(LastBid(2, Suit.Spades))),

                Signoff(2, Suit.Unknown, Points(OpenerRange.DontAcceptInvite)),

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



                Signoff(4, Suit.Hearts, Points(OpenerRange.AcceptInvite), Partner(LastBid(3, Suit.Hearts)), Shape(4, 5)),
                Signoff(4, Suit.Hearts, LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Hearts)), Shape(3)),


                Signoff(4, Suit.Spades, Points(OpenerRange.AcceptInvite), Partner(LastBid(3, Suit.Spades)), Shape(4, 5)),
                Signoff(4, Suit.Spades, Points(OpenerRange.AcceptInvite), Fit()),
				Signoff(4, Suit.Spades, Partner(LastBid(3, Suit.Unknown)), Fit()),
                Signoff(4, Suit.Spades, LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Spades)), Shape(3)),
            };
            pb.Partner(new Bid(3, Suit.Spades, BidForce.Nonforcing), CheckSpadeGame);
        }
    	public void CheckSpadeGame(PrescribedBids pb)
        { 
			pb.Bids = new BidRule[]
			{
				Signoff(4, Suit.Spades, ShowsTrump(), DummyPoints(ResponderRange.Game), Partner(LastBid(3, Suit.Spades)), Shape(4, 5))
			};
		}
	}


    //*********************************************************************************************

    // TODO: Maybe move thse 2NT stayman...
    public class Stayman2NT: TwoNoTrumpBidder
    {
        private Stayman2NT() : base(Convention.Stayman, 1000) { }

        public static PrescribedBids InitiateConvention()
        {
            var staymanBidder = new Stayman2NT();
            return new PrescribedBids(staymanBidder, staymanBidder.Initiate);
        }

        public void Initiate(PrescribedBids pb) 
        {
            pb.Bids = new BidRule[]
            {
                Forcing(3, Suit.Clubs, RespondGame, Shape(Suit.Hearts, 4), Flat(false)),
                Forcing(3, Suit.Clubs, RespondGame, Shape(Suit.Spades, 4), Flat(false)),
                Forcing(3, Suit.Clubs, RespondGame, Shape(Suit.Hearts, 4), Shape(Suit.Spades, 5)),
                Forcing(3, Suit.Clubs, RespondGame, Shape(Suit.Hearts, 5), Shape(Suit.Spades, 4)),
				// TODO: The following rule is "Garbage Stayman"
				//Forcing(2, Suit.Clubs, Points(NTLessThanInvite), Shape(Suit.Diamonds, 4, 5), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 4)),
			};
            pb.Partner(Answer);
        }
        public void Answer(PrescribedBids pb)
        {
            pb.Bids = new BidRule[]
            {

                Forcing(3, Suit.Diamonds, Shape(Suit.Hearts, 0, 3), Shape(Suit.Spades, 0, 3)),

				// If we are 4-4 then hearts bid before spades.  Can't be 5-5 or wouldn't be balanced.
				Forcing(3, Suit.Hearts, Shape(4, 5), LongerOrEqualTo(Suit.Spades)),
                Forcing(3, Suit.Spades, Shape(4, 5), LongerThan(Suit.Hearts)),
            };
            pb.Partner(ResponderRebid);
        }
        public void ResponderRebid(PrescribedBids pb)
        {
            pb.Bids = new BidRule[]
            {
                Forcing(3, Suit.Hearts, DefaultPriority + 10, Shape(5), Partner(LastBid(3, Suit.Diamonds))),
                Forcing(3, Suit.Spades, DefaultPriority + 10, Shape(5), Partner(LastBid(3, Suit.Diamonds))),

                Signoff(3, Suit.Unknown, Fit(Suit.Hearts, false), Fit(Suit.Spades, false)),
                Signoff(4, Suit.Hearts, Fit()),
                Signoff(4, Suit.Spades, Fit()),
            };
            pb.Partner(OpenerRebid);
        }
    
        public void OpenerRebid(PrescribedBids pb)
        { 
            pb.Bids = new BidRule[]
            {
                Signoff(3, Suit.Unknown, Fit(Suit.Hearts, false), Fit(Suit.Spades, false)),
                Signoff(4, Suit.Hearts, Fit()),
                Signoff(4, Suit.Spades, Fit()),
            };
        }
    }

}
