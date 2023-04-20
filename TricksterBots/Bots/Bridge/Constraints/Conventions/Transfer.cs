using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;


namespace TricksterBots.Bots.Bridge
{
	internal class Transfer : Bidder
	{

		// TODO: BAD BAD BAD - Move to a common place.  Perhaps a NoTrump class that Stayman and Transfers derrive from.
		static private (int, int) NTLessThanInvite = (0, 7);
		static private (int, int) NTInvite = (8, 9);
		static private (int, int) NTInviteOrBetter = (8, 40);
		static private (int, int) NTGame = (10, 15);
		static private (int, int) NTSlamInterest = (16, 40);
		static private (int, int) LessThanSuperAccept = (15, 16);
		static private (int, int) SuperAccept = (17, 17);
		static private (int, int) GameIfSuperAccept = (7, 15);  // Responder will accept TODO: IS THIS RIGHT POINTS??? 
		static private (int, int) LessThanAcceptInvite = (15, 15);
		static private (int, int) AcceptInvite = (16, 17);
		static private (int, int) GameOrBetter = (10, 40);
		static private (int, int) NTOpen = (15, 17);

		public Transfer() : base(BidConvention.JacobyTransfer, 2000)
		{
		}

		public override IEnumerable<BidRule> GetRules(PositionState ps)
		{
			// TODO: Make sure no interference
			// TODO: Check to make sure transfers are enabled

			if (ps.Role == PositionRole.Responder && ps.Partner.LastBid.Is(1, Suit.Unknown) && ps.BidRound == 1)
			{
				return InitiateTransfer();
			}
			else if (ps.Role == PositionRole.Opener && ps.BidRound == 2)
			{
				return AcceptTransfer();
			}
			// TODO: THIS IS A BUG -- NEED CONVENTION ROUND.  SHOULD BE 2 HERE.  WHEN INITIATED SHOULD GO TO 1.
			// IF RESPONDER IS A PASSED HAND THEN Round will NOT equal 2!!!

			else if (ps.Role == PositionRole.Responder && ps.BidRound == 2)
			{
				return ExplainTransfer();
			}
			else if (ps.Role== PositionRole.Opener && ps.BidRound == 2)
			{
				return OpenerRebid();
			}
			return new BidRule[0];
		}

		public BidRule[] InitiateTransfer()
		{
			BidRule[] rules =
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
			return rules;
		}

		public BidRule[] AcceptTransfer()
		{
			BidRule[] rules =
			{
				NonForcing(2, Suit.Hearts, PartnerBid(2, Suit.Diamonds), Points(LessThanSuperAccept)),
				NonForcing(2, Suit.Hearts, PartnerBid(2, Suit.Diamonds), Points(SuperAccept), Shape(0, 3)),

				NonForcing(2, Suit.Spades, PartnerBid(2, Suit.Hearts), Points(LessThanSuperAccept)),
				NonForcing(2, Suit.Spades, PartnerBid(2, Suit.Hearts), Points(SuperAccept), Shape(0, 3)),

				NonForcing(3, Suit.Clubs, PartnerBid(2, Suit.Spades)),

				NonForcing(3, Suit.Hearts, PartnerBid(2, Suit.Diamonds), Points(SuperAccept), Shape(4, 5)),

				NonForcing(3, Suit.Spades, PartnerBid(2, Suit.Hearts), Points(SuperAccept), Shape(4, 5)),
			};
			return rules;
		}


		public BidRule[] ExplainTransfer()
		{
			BidRule[] rules =
			{
				Signoff(CallType.Pass, Points(NTLessThanInvite)),

				// This can happen if we are 5/5 with invitational hand. Show Spades
				// TODDO: Higher prioiryt than other bids.  Seems reasonable...
				Invitational(2, Suit.Spades, DefaultPriority + 1, Points(NTLessThanInvite), Shape(5, 11)),

				Invitational(2, Suit.Unknown, Points(NTInvite), PartnerBid(2, Suit.Hearts), Shape(Suit.Hearts, 5)),
				Invitational(2, Suit.Unknown, Points(NTInvite), PartnerBid(2, Suit.Spades), Shape(Suit.Spades, 5)),

				Signoff(3, Suit.Diamonds, PartnerBid(3, Suit.Clubs), Shape(Suit.Diamonds, 6, 11)),

				// Need to bid 3 hearts with 5/5 and game-going values.
				Invitational(3, Suit.Hearts, Points(NTInvite), PartnerBid(2, Suit.Hearts), Shape(6, 11)),
				Forcing(3, Suit.Hearts, DefaultPriority + 1, Points(GameOrBetter), PartnerBid(2, Suit.Spades), Shape(5)),

				Invitational(3, Suit.Spades, Points(NTInvite), PartnerBid(2, Suit.Spades), Shape(6, 11)),

				Signoff(3, Suit.Unknown, Points(NTGame), PartnerBid(2, Suit.Hearts), Shape(Suit.Hearts, 5)),
				Signoff(3, Suit.Unknown, Points(NTGame), PartnerBid(2, Suit.Spades), Shape(Suit.Spades, 5)),

				Signoff(4, Suit.Hearts, Points(NTGame), PartnerBid(2, Suit.Hearts), Shape(6, 11)),
				Signoff(4, Suit.Hearts, Points(GameIfSuperAccept), PartnerBid(3, Suit.Hearts)),

				Signoff(4, Suit.Spades, Points(NTGame), PartnerBid(2, Suit.Spades), Shape(6,11)),
				Signoff(4, Suit.Spades, Points(GameIfSuperAccept), PartnerBid(3, Suit.Spades))

				// TODO: SLAM BIDDING.  REMEMBER RANGES NEED TO BE DIFFERENT IF SUPER ACCEPTED...
			};
			return rules;
		}



		public BidRule[] OpenerRebid()
		{
			BidRule[] rules =
			{
				// TODO: Make lower priority???  
				Signoff(CallType.Pass, Points(LessThanAcceptInvite)),

				Signoff(3, Suit.Hearts, Points(LessThanAcceptInvite), PreviousBid(2, Suit.Hearts), Flat(false), Shape(3)),
				Signoff(3, Suit.Hearts, Points(LessThanAcceptInvite), PreviousBid(2, Suit.Hearts), Shape(4, 5)),

				Signoff(3, Suit.Spades, Points(LessThanAcceptInvite), PreviousBid(2, Suit.Spades), Flat(false), Shape(3)),
				Signoff(3, Suit.Spades, Points(LessThanAcceptInvite), PreviousBid(2, Suit.Spades), Shape(4, 5)),

				// TODO: Perhaps priority makes this the last choice... Maybe not.   May need lots of exclusions...
				// TODO: Maybe if partner shows exactly 5 and we are flat then we bid to 3NT instead of accepting
				// even if we have three  What about 4 of them?  Probably safest in suit.... 
				Signoff(3, Suit.Unknown, DefaultPriority - 10, Points(AcceptInvite)),


				Signoff(4, Suit.Hearts, Points(AcceptInvite), PartnerBid(3, Suit.Hearts)),
				Signoff(4, Suit.Hearts, Points(AcceptInvite), PreviousBid(2, Suit.Hearts), PartnerBid(2, Suit.Unknown), Shape(3, 5)),
				Signoff(4, Suit.Hearts, Points(NTOpen), PreviousBid(2, Suit.Spades), PartnerBid(3, Suit.Hearts), Shape(3, 5), BetterOrEqual(Suit.Hearts, Suit.Spades)),


				Signoff(4, Suit.Spades, Points(AcceptInvite), PartnerBid(3, Suit.Spades)),
				Signoff(4, Suit.Spades, Points(AcceptInvite), PreviousBid(2, Suit.Spades), PartnerBid(2, Suit.Unknown), Shape(3, 5)),
				Signoff(4, Suit.Hearts, Points(NTOpen), PreviousBid(2, Suit.Spades), PartnerBid(3, Suit.Hearts), Shape(3, 5), Better(Suit.Spades, Suit.Hearts)),

				// TODO: SLAM BIDDING...
				// I Think here we will just defer to competative bidding.  Then ranges don't matter.  We just look for 
				// shown values and shapes.  By this point everything is pretty clear.  The only thing is do we have a shown
				// fit or is it a known fit.  Perhaps competative bidding can handle this...  
		
			};
			return rules;
		}

	}
}
