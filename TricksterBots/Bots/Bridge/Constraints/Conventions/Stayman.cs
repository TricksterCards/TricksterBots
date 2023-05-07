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
		public StaymanBidder(NTType type) : base(type, Convention.Stayman, 1000) { }
	}

	public class InitiateStayman: StaymanBidder
	{
		public InitiateStayman(NTType type) : base(type)
		{
			this.BidRules = new BidRule[]
			{
                Forcing(2, Suit.Clubs, Points(ResponderRange.InviteOrBetter), Shape(Suit.Hearts, 4), Flat(false)),
                Forcing(2, Suit.Clubs, Points(ResponderRange.InviteOrBetter), Shape(Suit.Spades, 4), Flat(false)),
                Forcing(2, Suit.Clubs, Points(ResponderRange.GameOrBetter), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 5)),
                Forcing(2, Suit.Clubs, Points(ResponderRange.GameOrBetter), Shape(Suit.Hearts, 5), Shape(Suit.Spades, 4)),
				// TODO: The following rule is "Garbage Stayman"
				//Forcing(2, Suit.Clubs, Points(NTLessThanInvite), Shape(Suit.Diamonds, 4, 5), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 4)),
			};
			this.NextConventionState = () => new AnswerStayman(type);
		}
	}
	public class AnswerStayman : StaymanBidder
	{
        public AnswerStayman(NTType type) : base(type)
		{ 
            this.BidRules = new BidRule[]
            {
				// TODO: Are these bids truly forcing?  Not if garbage stayman...
				Forcing(2, Suit.Diamonds, Shape(Suit.Hearts, 0, 3), Shape(Suit.Spades, 0, 3)),

				// If we are 4-4 then hearts bid before spades.  Can't be 5-5 or wouldn't be balanced.
				Forcing(2, Suit.Hearts, Shape(4, 5), LongerOrEqualTo(Suit.Spades)),

                Forcing(2, Suit.Spades, Shape(4, 5), LongerThan(Suit.Hearts)),
            };
            this.NextConventionState = () => new ExplainStayman(type);
        }
    }

	public class ExplainStayman : StaymanBidder
	{
		public ExplainStayman(NTType type) : base(type)
		{
            this.BidRules = new BidRule[]
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

                Signoff(3, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Game), Partner(LastBid(2, Suit.Diamonds))),
                Signoff(3, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Game), Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 0, 3)),
                Signoff(3, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Game), Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 0, 3)),

                Signoff(4, Suit.Hearts, ShowsTrump(), DummyPoints(ResponderRange.Game), Partner(LastBid(2, Suit.Hearts)), Shape(4, 5)),

                Signoff(4, Suit.Spades, ShowsTrump(), DummyPoints(ResponderRange.Game), Partner(LastBid(2, Suit.Spades)), Shape(4, 5))
            };
            this.NextConventionState = () => new PlaceContract(type);
        }
	}

	public class PlaceContract : StaymanBidder
	{
		public PlaceContract(NTType type) : base(type)
		{
			this.BidRules = new BidRule[]
			{
				// These rules deal with a 5/4 invitational that we want to reject.  Leave contract in bid suit
				// if we have 3.  Otherwise put in NT
				Signoff(CallType.Pass, DefaultPriority + 10, Points(OpenerRange.DontAcceptInvite), 
									Fit(Suit.Hearts), Partner(LastBid(2, Suit.Hearts))),
                Signoff(CallType.Pass, DefaultPriority + 10, Points(OpenerRange.DontAcceptInvite),
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
			this.NextConventionState = () => new CheckSpadeGame(type);
        }
	}

	public class CheckSpadeGame : StaymanBidder
	{
		public CheckSpadeGame(NTType type) : base(type)
		{
			this.BidRules = new BidRule[]
			{
				Signoff(4, Suit.Spades, ShowsTrump(), DummyPoints(ResponderRange.Game), Partner(LastBid(3, Suit.Spades)), Shape(4, 5))
			};
		}
	}

}
