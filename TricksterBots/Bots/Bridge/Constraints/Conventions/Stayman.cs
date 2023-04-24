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
	public class BidderThingie : Bidder
	{
		public BidderThingie(BidConvention convention, int defaultPriority) : base(convention, defaultPriority) { }	

		public ConventionRule[] RedirectRules { get; protected set; }
		
		// TODO: Perhaps if no convention rules then we just say it does conform.
		public bool Conforms(Bid nextLegalBid, PositionState ps)
		{
			if (ConventionRules == null) { return true; }
			foreach (var rule in ConventionRules)
			{
				if (rule.Conforms(nextLegalBid, ps))
				{
					return true;
				}
			}
			return false;
		}

		//public BidRule[] Rules { get; protected set; }


		public BidConvention? Redirect { get; protected set; }



	}





	public class Stayman : Bidder
	{

		// TODO: Move all of this stuff.  Perhaps into a NoTrump bidder...  Then inherit from that...



		static protected (int, int) NTLessThanInvite = (0, 7);
		static protected (int, int) NTInvite = (8, 9);
		static protected (int, int) NTInviteOrBetter = (8, 40);
		static protected (int, int) NTGame = (10, 15);
		static protected (int, int) NTSlamInterest = (16, 40);
		static protected (int, int) NTGameOrBetter = (10, 40);
		static protected (int, int) NTAcceptInvite = (16, 17);
		static protected (int, int) NTDontAcceptInvite = (15, 15);
		static protected (int, int) NTOpen = (15, 17);


		public Stayman() : base(BidConvention.Stayman, 5000)
		{
		}

	}
	public class InitiateStayman: Stayman
	{
		public InitiateStayman() : base()
		{
			this.ConventionRules = new ConventionRule[]
			{
				ConventionRule(Role(PositionRole.Responder, 1), Partner(LastBid(1, Suit.Unknown)))
			};
			this.BidRules = new BidRule[]
			{
                Forcing(2, Suit.Clubs, Points(NTInviteOrBetter), Shape(Suit.Hearts, 4), Flat(false)),
                Forcing(2, Suit.Clubs, Points(NTInviteOrBetter), Shape(Suit.Spades, 4), Flat(false)),
                Forcing(2, Suit.Clubs, Points(NTGameOrBetter), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 5)),
                Forcing(2, Suit.Clubs, Points(NTGameOrBetter), Shape(Suit.Hearts, 5), Shape(Suit.Spades, 4)),
				// TODO: The following rule is "Garbage Stayman"
				//Forcing(2, Suit.Clubs, Points(NTLessThanInvite), Shape(Suit.Diamonds, 4, 5), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 4)),
			};
			this.NextConventionState = () => new AnswerStayman();
			var redirectRules = new ConventionRule[]
			{
				// IDEA IS THAT THE REDIRECT ONLY HAPPENS IFF ConventionRules are satisfied.  Then if redirect is true the bidder rules
				// will NOT be used and a redirect will happen...
				ConventionRule(RHO(DidBid()))
			};
		}
	}
	public class AnswerStayman : Stayman
	{
        public AnswerStayman() : base()
		{ 
            this.BidRules = new BidRule[]
            {
				// TODO: Are these bids truly forcing?  Not if garbage stayman...
				Forcing(2, Suit.Diamonds, Shape(Suit.Hearts, 0, 3), Shape(Suit.Spades, 0, 3)),

				// If we are 4-4 then hearts bid before spades.  Can't be 5-5 or wouldn't be balanced.
				Forcing(2, Suit.Hearts, Shape(4, 5), LongerOrEqualTo(Suit.Spades)),

                Forcing(2, Suit.Spades, Shape(4, 5), LongerThan(Suit.Hearts)),
            };
            this.NextConventionState = () => new ExplainStayman();
        }
    }

	public class ExplainStayman : Stayman
	{
		public ExplainStayman() : base()
		{
            this.BidRules = new BidRule[]
            {
                Invitational(2, Suit.Unknown, ShowsTrump(), Points(NTInvite), Partner(LastBid(2, Suit.Diamonds))),
                Invitational(2, Suit.Unknown, ShowsTrump(), Points(NTInvite), Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 0, 3)),
                Invitational(2, Suit.Unknown, ShowsTrump(), Points(NTInvite), Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 0, 3)),


                Invitational(3, Suit.Hearts, ShowsTrump(), DummyPoints(NTInvite), Partner(LastBid(2, Suit.Hearts)), Shape(4, 5)),
                Forcing(3, Suit.Hearts, DefaultPriority + 10, Points(NTGameOrBetter), Shape(5), Partner(LastBid(2, Suit.Diamonds))),


                Invitational(3, Suit.Spades, ShowsTrump(), DummyPoints(NTInvite), Partner(LastBid(2, Suit.Spades)), Shape(4, 5)),
                Forcing(3, Suit.Spades, DefaultPriority + 10, Points(NTGameOrBetter), Shape(5), Partner(LastBid(2, Suit.Diamonds))),

                Signoff(3, Suit.Unknown, ShowsTrump(), Points(NTGame), Partner(LastBid(2, Suit.Diamonds))),
                Signoff(3, Suit.Unknown, ShowsTrump(), Points(NTGame), Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 0, 3)),
                Signoff(3, Suit.Unknown, ShowsTrump(), Points(NTGame), Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 0, 3)),

                Signoff(4, Suit.Hearts, ShowsTrump(), DummyPoints(NTGame), Partner(LastBid(2, Suit.Hearts)), Shape(4, 5)),

                Signoff(4, Suit.Spades, ShowsTrump(), DummyPoints(NTGame), Partner(LastBid(2, Suit.Spades)), Shape(4, 5))
            };
            this.NextConventionState = () => new PlaceContract();
        }
	}

	public class PlaceContract : Stayman
	{
		public PlaceContract() : base()
		{
			this.BidRules = new BidRule[]
            {
                NonForcing(3, Suit.Spades, Points(NTDontAcceptInvite), Shape(4), Partner(HasShape(4))),

                Signoff(3, Suit.Unknown, Points(NTAcceptInvite), Partner(LastBid(2, Suit.Unknown))),
                Signoff(3, Suit.Unknown, Points(NTOpen), LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Hearts)),
                            Shape(Suit.Hearts, 2)),
                Signoff(3, Suit.Unknown, Points(NTOpen), LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Spades)),
                            Shape(Suit.Spades, 2)),

                Signoff(4, Suit.Hearts, Points(NTAcceptInvite), Partner(LastBid(3, Suit.Hearts)), Shape(4, 5)),
                Signoff(4, Suit.Hearts, Points(NTOpen), LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Hearts)), Shape(3)),


                Signoff(4, Suit.Spades, Points(NTAcceptInvite), Partner(LastBid(3, Suit.Spades)), Shape(4, 5)),
                Signoff(4, Suit.Spades, Points(NTAcceptInvite), Partner(HasShape(4)), Shape(4, 5)),
                Signoff(4, Suit.Spades, Points(NTOpen), LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Spades)), Shape(3)),
            };
			this.NextConventionState = () => new CheckSpadeGame();
        }
	}

	public class CheckSpadeGame : Stayman
	{
		public CheckSpadeGame() : base()
		{
			this.BidRules = new BidRule[]
			{
				Signoff(4, Suit.Spades, ShowsTrump(), DummyPoints(NTGame), Partner(LastBid(3, Suit.Spades)), Shape(4, 5))
			};
		}
	}

}
