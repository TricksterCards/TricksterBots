﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;


namespace TricksterBots.Bots.Bridge
{
	public class TransferBidder : NoTrumpBidder
	{
		public TransferBidder(NTType type) : base(type, Convention.Transfer, 1000) { }
	}

	public class InitiateTransfer : TransferBidder
	{ 


		public InitiateTransfer(NTType type) : base(type)
		{
			this.BidRules = new BidRule[]
			{
                // For weak hands, transfer to longest major.
                // For invitational hands, 5/5 transfer to hearts then bid spades
                // For game-going hands 5/5 transfer to spades then bid 3H
				Forcing(2, Suit.Diamonds, Points(ResponderRange.LessThanInvite), Shape(Suit.Hearts, 5, 11), Better(Suit.Hearts, Suit.Spades)),
				Forcing(2, Suit.Diamonds, Points(ResponderRange.Invite), Shape(Suit.Hearts, 5, 11), Shape(Suit.Spades, 0, 5)),
				Forcing(2, Suit.Diamonds, Points(ResponderRange.GameOrBetter), Shape(Suit.Hearts, 5, 11), Shape(Suit.Spades, 0, 4)),

				Forcing(2, Suit.Hearts, Points(ResponderRange.LessThanInvite), Shape(Suit.Spades, 5, 11), BetterOrEqual(Suit.Spades, Suit.Hearts)),
				Forcing(2, Suit.Hearts, Points(ResponderRange.Invite), Shape(Suit.Spades, 5, 11), Shape(Suit.Hearts, 0, 4)),
				Forcing(2, Suit.Hearts, Points(ResponderRange.GameOrBetter), Shape(Suit.Spades, 5, 11)),

				// TODO: Solid long minors are lots of tricks.  Need logic for those....

				Forcing(2, Suit.Spades, Points(ResponderRange.LessThanInvite), Shape(Suit.Clubs, 6), GoodSuit(Suit.Clubs)),
				Forcing(2, Suit.Spades, Points(ResponderRange.LessThanInvite), Shape(Suit.Clubs, 7, 11)),
				Forcing(2, Suit.Spades, Points(ResponderRange.LessThanInvite), Shape(Suit.Diamonds, 6), GoodSuit(Suit.Diamonds)),
				Forcing(2, Suit.Spades, Points(ResponderRange.LessThanInvite), Shape(Suit.Diamonds, 7, 11)),

			};
			this.NextConventionState = () => new AcceptTransfer(type);
		}
	}

	public class AcceptTransfer: TransferBidder
	{
		public AcceptTransfer(NTType type) : base(type)
		{
			this.BidRules = new BidRule[]
			{
				Nonforcing(2, Suit.Hearts, Partner(LastBid(2, Suit.Diamonds)), Points(OpenerRange.LessThanSuperAccept)),
				Nonforcing(2, Suit.Hearts, Partner(LastBid(2, Suit.Diamonds)), Points(OpenerRange.SuperAccept), Shape(0, 3)),

				Nonforcing(2, Suit.Spades, Partner(LastBid(2, Suit.Hearts)), Points(OpenerRange.LessThanSuperAccept)),
				Nonforcing(2, Suit.Spades, Partner(LastBid(2, Suit.Hearts)), Points(OpenerRange.SuperAccept), Shape(0, 3)),

				Nonforcing(3, Suit.Clubs, Partner(LastBid(2, Suit.Spades))),

				Nonforcing(3, Suit.Hearts, ShowsTrump(), Partner(LastBid(2, Suit.Diamonds)), Points(OpenerRange.SuperAccept), Shape(4, 5)),

				Nonforcing(3, Suit.Spades, ShowsTrump(), Partner(LastBid(2, Suit.Hearts)), Points(OpenerRange.SuperAccept), Shape(4, 5)),
			};
			this.NextConventionState = () => new ExplainTransfer(type);
        }
	}

	public class ExplainTransfer: TransferBidder
	{
		public ExplainTransfer(NTType type) : base(type)
		{
			this.BidRules = new BidRule[]
            {
                Signoff(CallType.Pass, DefaultPriority - 1, Points(ResponderRange.LessThanInvite)),

				// This can happen if we are 5/5 with invitational hand. Show Spades
				// TODDO: Higher prioiryt than other bids.  Seems reasonable...
				Invitational(2, Suit.Spades, DefaultPriority + 1, Points(ResponderRange.LessThanInvite), Shape(5, 11)),

                Invitational(2, Suit.Unknown, Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 5)),
                Invitational(2, Suit.Unknown, Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 5)),

                Signoff(3, Suit.Diamonds, Partner(LastBid(3, Suit.Clubs)), Shape(Suit.Diamonds, 6, 11)),

				// Need to bid 3 hearts with 5/5 and game-going values.
				Invitational(3, Suit.Hearts, Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Hearts)), Shape(6, 11)),
                Forcing(3, Suit.Hearts, DefaultPriority + 1, Points(ResponderRange.GameOrBetter), Partner(LastBid(2, Suit.Spades)), Shape(5)),

                Invitational(3, Suit.Spades, Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Spades)), Shape(6, 11)),

                Signoff(3, Suit.Unknown, Points(ResponderRange.Game), Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 5)),
                Signoff(3, Suit.Unknown, Points(ResponderRange.Game), Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 5)),

                Signoff(4, Suit.Hearts, Points(ResponderRange.Game), Partner(LastBid(2, Suit.Hearts)), Shape(6, 11)),
                Signoff(4, Suit.Hearts, DefaultPriority + 1, Points(ResponderRange.GameIfSuperAccept), Partner(LastBid(3, Suit.Hearts))),

                Signoff(4, Suit.Spades, Points(ResponderRange.Game), Partner(LastBid(2, Suit.Spades)), Shape(6,11)),
                Signoff(4, Suit.Spades, DefaultPriority + 1, Points(ResponderRange.GameIfSuperAccept), Partner(LastBid(3, Suit.Spades)))

				// TODO: SLAM BIDDING.  REMEMBER RANGES NEED TO BE DIFFERENT IF SUPER ACCEPTED...
			};
			this.NextConventionState = () => new TransferOpenerRebid(type);
        }
    }
	
	public class TransferOpenerRebid: TransferBidder
	{
		public TransferOpenerRebid(NTType type) : base(type)
		{
			this.BidRules = new BidRule[]
            {
				// TODO: Make lower priority???  
				Signoff(CallType.Pass, DefaultPriority - 1, Points(OpenerRange.DontAcceptInvite)),

//Signoff(3, Suit.Hearts, Points(OpenerRange.DontAcceptInvite), LastBid(2, Suit.Hearts), Flat(false), Shape(3)),
                Signoff(3, Suit.Hearts, Points(OpenerRange.DontAcceptInvite), LastBid(2, Suit.Hearts), Shape(3, 5)),

  //              Signoff(3, Suit.Spades, Points(OpenerRange.DontAcceptInvite), LastBid(2, Suit.Spades), Flat(false), Shape(3)),
                Signoff(3, Suit.Spades, Points(OpenerRange.DontAcceptInvite), LastBid(2, Suit.Spades), Shape(3, 5)),

				// TODO: Perhaps priority makes this the last choice... Maybe not.   May need lots of exclusions...
				// TODO: Maybe if partner shows exactly 5 and we are flat then we bid to 3NT instead of accepting
				// even if we have three  What about 4 of them?  Probably safest in suit.... 
				Signoff(3, Suit.Unknown, DefaultPriority - 10, Points(OpenerRange.AcceptInvite)),

				// TODO: Really want to work off of "Partner Shows" instead of PartnerBid...
                Signoff(4, Suit.Hearts, Points(OpenerRange.AcceptInvite), Partner(LastBid(3, Suit.Hearts))),
                Signoff(4, Suit.Hearts, Points(OpenerRange.AcceptInvite), LastBid(2, Suit.Hearts), Partner(LastBid(2, Suit.Unknown)), Shape(3, 5)),
				Signoff(4, Suit.Hearts, LastBid(2, Suit.Hearts), Partner(LastBid(3, Suit.Unknown)), Shape(3, 5)),
                Signoff(4, Suit.Hearts, LastBid(2, Suit.Spades), Partner(LastBid(3, Suit.Hearts)), Shape(3, 5), BetterOrEqual(Suit.Hearts, Suit.Spades)),


                Signoff(4, Suit.Spades, Points(OpenerRange.AcceptInvite), Partner(LastBid(3, Suit.Spades))),
                Signoff(4, Suit.Spades, Points(OpenerRange.AcceptInvite), LastBid(2, Suit.Spades), Partner(LastBid(2, Suit.Unknown)), Shape(3, 5)),
				Signoff(4, Suit.Spades, LastBid(2, Suit.Spades), Partner(LastBid(3, Suit.Unknown)), Shape(3, 5)),
				Signoff(4, Suit.Spades, LastBid(2, Suit.Spades), Partner(LastBid(3, Suit.Spades)), Shape(3, 5), Better(Suit.Spades, Suit.Hearts))

				// TODO: SLAM BIDDING...
				// I Think here we will just defer to competative bidding.  Then ranges don't matter.  We just look for 
				// shown values and shapes.  By this point everything is pretty clear.  The only thing is do we have a shown
				// fit or is it a known fit.  Perhaps competative bidding can handle this...  
		
			};

        }
    }
}
