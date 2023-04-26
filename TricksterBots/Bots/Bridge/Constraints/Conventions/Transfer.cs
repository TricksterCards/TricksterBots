using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;


namespace TricksterBots.Bots.Bridge
{
	public class Transfer : Bidder
	{

		// TODO: BAD BAD BAD - Move to a common place.  Perhaps a NoTrump class that Stayman and Transfers derrive from.
		static protected (int, int) NTLessThanInvite = (0, 7);
		static protected (int, int) NTInvite = (8, 9);
		static protected (int, int) NTInviteOrBetter = (8, 40);
		static protected (int, int) NTGame = (10, 15);
		static protected (int, int) NTSlamInterest = (16, 40);
		static protected (int, int) LessThanSuperAccept = (15, 16);
		static protected (int, int) SuperAccept = (17, 17);
		static protected (int, int) GameIfSuperAccept = (7, 15);  // Responder will accept TODO: IS THIS RIGHT POINTS??? 
		static protected (int, int) LessThanAcceptInvite = (15, 15);
		static protected (int, int) AcceptInvite = (16, 17);
		static protected (int, int) GameOrBetter = (10, 40);
		static protected (int, int) NTOpen = (15, 17);

		public Transfer() : base(Convention.Transfer, 2000)
		{
		}
	}

	public class InitiateTransfer : Transfer
	{
		public InitiateTransfer() : base()
		{
			this.ConventionRules = new ConventionRule[]
			{
				ConventionRule(Role(PositionRole.Responder, 1), Partner(LastBid(1, Suit.Unknown)))
			};
			this.BidRules = new BidRule[]
			{
                // For weak hands, transfer to longest major.
                // For invitational hands, 5/5 transfer to hearts then bid spades
                // For game-going hands 5/5 transfer to spades then bid 3H
				Forcing(2, Suit.Diamonds, Points(NTLessThanInvite), Shape(Suit.Hearts, 5, 11), Better(Suit.Hearts, Suit.Spades)),
				Forcing(2, Suit.Diamonds, Points(NTInvite), Shape(Suit.Hearts, 5, 11), Shape(Suit.Spades, 0, 5)),
				Forcing(2, Suit.Diamonds, Points(GameOrBetter), Shape(Suit.Hearts, 5, 11), Shape(Suit.Spades, 0, 4)),

				Forcing(2, Suit.Hearts, Points(NTLessThanInvite), Shape(Suit.Spades, 5, 11), BetterOrEqual(Suit.Spades, Suit.Hearts)),
				Forcing(2, Suit.Hearts, Points(NTInvite), Shape(Suit.Spades, 5, 11), Shape(Suit.Hearts, 0, 4)),
				Forcing(2, Suit.Hearts, Points(GameOrBetter), Shape(Suit.Spades, 5, 11)),

				// TODO: Solid long minors are lots of tricks.  Need logic for those....

				Forcing(2, Suit.Spades, Points(NTLessThanInvite), Shape(Suit.Clubs, 6), Quality(Suit.Clubs, SuitQuality.Good, SuitQuality.Excellent)),
				Forcing(2, Suit.Spades, Points(NTLessThanInvite), Shape(Suit.Clubs, 7, 11)),
				Forcing(2, Suit.Spades, Points(NTLessThanInvite), Shape(Suit.Diamonds, 6), Quality(Suit.Diamonds, SuitQuality.Good, SuitQuality.Excellent)),
				Forcing(2, Suit.Spades, Points(NTLessThanInvite), Shape(Suit.Diamonds, 7, 11)),

			};
			this.NextConventionState = () => new AcceptTransfer();
		}
	}

	public class AcceptTransfer: Transfer
	{
		public AcceptTransfer() : base()
		{
			this.BidRules = new BidRule[]
			{
				Nonforcing(2, Suit.Hearts, Partner(LastBid(2, Suit.Diamonds)), Points(LessThanSuperAccept)),
				Nonforcing(2, Suit.Hearts, Partner(LastBid(2, Suit.Diamonds)), Points(SuperAccept), Shape(0, 3)),

				Nonforcing(2, Suit.Spades, Partner(LastBid(2, Suit.Hearts)), Points(LessThanSuperAccept)),
				Nonforcing(2, Suit.Spades, Partner(LastBid(2, Suit.Hearts)), Points(SuperAccept), Shape(0, 3)),

				Nonforcing(3, Suit.Clubs, Partner(LastBid(2, Suit.Spades))),

				Nonforcing(3, Suit.Hearts, ShowsTrump(), Partner(LastBid(2, Suit.Diamonds)), Points(SuperAccept), Shape(4, 5)),

				Nonforcing(3, Suit.Spades, ShowsTrump(), Partner(LastBid(2, Suit.Hearts)), Points(SuperAccept), Shape(4, 5)),
			};
			this.NextConventionState = () => new ExplainTransfer();
        }
	}

	public class ExplainTransfer: Transfer
	{
		public ExplainTransfer()
		{
			this.BidRules = new BidRule[]
            {
                Signoff(CallType.Pass, Points(NTLessThanInvite)),

				// This can happen if we are 5/5 with invitational hand. Show Spades
				// TODDO: Higher prioiryt than other bids.  Seems reasonable...
				Invitational(2, Suit.Spades, DefaultPriority + 1, Points(NTLessThanInvite), Shape(5, 11)),

                Invitational(2, Suit.Unknown, Points(NTInvite), Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 5)),
                Invitational(2, Suit.Unknown, Points(NTInvite), Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 5)),

                Signoff(3, Suit.Diamonds, Partner(LastBid(3, Suit.Clubs)), Shape(Suit.Diamonds, 6, 11)),

				// Need to bid 3 hearts with 5/5 and game-going values.
				Invitational(3, Suit.Hearts, Points(NTInvite), Partner(LastBid(2, Suit.Hearts)), Shape(6, 11)),
                Forcing(3, Suit.Hearts, DefaultPriority + 1, Points(GameOrBetter), Partner(LastBid(2, Suit.Spades)), Shape(5)),

                Invitational(3, Suit.Spades, Points(NTInvite), Partner(LastBid(2, Suit.Spades)), Shape(6, 11)),

                Signoff(3, Suit.Unknown, Points(NTGame), Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 5)),
                Signoff(3, Suit.Unknown, Points(NTGame), Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 5)),

                Signoff(4, Suit.Hearts, Points(NTGame), Partner(LastBid(2, Suit.Hearts)), Shape(6, 11)),
                Signoff(4, Suit.Hearts, Points(GameIfSuperAccept), Partner(LastBid(3, Suit.Hearts))),

                Signoff(4, Suit.Spades, Points(NTGame), Partner(LastBid(2, Suit.Spades)), Shape(6,11)),
                Signoff(4, Suit.Spades, Points(GameIfSuperAccept), Partner(LastBid(3, Suit.Spades)))

				// TODO: SLAM BIDDING.  REMEMBER RANGES NEED TO BE DIFFERENT IF SUPER ACCEPTED...
			};
			this.NextConventionState = () => new TransferOpenerRebid();
        }
    }
	
	public class TransferOpenerRebid: Transfer
	{
		public TransferOpenerRebid()
		{
			this.BidRules = new BidRule[]
            {
				// TODO: Make lower priority???  
				Signoff(CallType.Pass, DefaultPriority - 1, Points(LessThanAcceptInvite)),

                Signoff(3, Suit.Hearts, Points(LessThanAcceptInvite), LastBid(2, Suit.Hearts), Flat(false), Shape(3)),
                Signoff(3, Suit.Hearts, Points(LessThanAcceptInvite), LastBid(2, Suit.Hearts), Shape(4, 5)),

                Signoff(3, Suit.Spades, Points(LessThanAcceptInvite), LastBid(2, Suit.Spades), Flat(false), Shape(3)),
                Signoff(3, Suit.Spades, Points(LessThanAcceptInvite), LastBid(2, Suit.Spades), Shape(4, 5)),

				// TODO: Perhaps priority makes this the last choice... Maybe not.   May need lots of exclusions...
				// TODO: Maybe if partner shows exactly 5 and we are flat then we bid to 3NT instead of accepting
				// even if we have three  What about 4 of them?  Probably safest in suit.... 
				Signoff(3, Suit.Unknown, DefaultPriority - 10, Points(AcceptInvite)),


                Signoff(4, Suit.Hearts, Points(AcceptInvite), Partner(LastBid(3, Suit.Hearts))),
                Signoff(4, Suit.Hearts, Points(AcceptInvite), LastBid(2, Suit.Hearts), Partner(LastBid(2, Suit.Unknown)), Shape(3, 5)),
                Signoff(4, Suit.Hearts, Points(NTOpen), LastBid(2, Suit.Spades), Partner(LastBid(3, Suit.Hearts)), Shape(3, 5), BetterOrEqual(Suit.Hearts, Suit.Spades)),


                Signoff(4, Suit.Spades, Points(AcceptInvite), Partner(LastBid(3, Suit.Spades))),
                Signoff(4, Suit.Spades, Points(AcceptInvite), LastBid(2, Suit.Spades), Partner(LastBid(2, Suit.Unknown)), Shape(3, 5)),
                Signoff(4, Suit.Hearts, Points(NTOpen), LastBid(2, Suit.Spades), Partner(LastBid(3, Suit.Hearts)), Shape(3, 5), Better(Suit.Spades, Suit.Hearts))

				// TODO: SLAM BIDDING...
				// I Think here we will just defer to competative bidding.  Then ranges don't matter.  We just look for 
				// shown values and shapes.  By this point everything is pretty clear.  The only thing is do we have a shown
				// fit or is it a known fit.  Perhaps competative bidding can handle this...  
		
			};

        }
    }
}
