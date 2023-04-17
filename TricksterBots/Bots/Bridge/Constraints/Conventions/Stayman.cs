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
	public class Stayman : Bidder
	{

		// TODO: Move all of this stuff.  Perhaps into a NoTrump bidder...  Then inherit from that...



		static private (int, int) NTLessThanInvite = (0, 7);
		static private (int, int) NTInvite = (8, 9);
		static private (int, int) NTInviteOrBetter = (8, 40);
		static private (int, int) NTGame = (10, 15);
		static private (int, int) NTSlamInterest = (16, 40);
		static private (int, int) NTGameOrBetter = (10, 40);
		static private (int, int) NTAcceptInvite = (16, 17);
		static private (int, int) NTDontAcceptInvite = (15, 15);
		static private (int, int) NTOpen = (15, 17);


		public Stayman() : base(BidConvention.Stayman, 5000) { }
		public override IEnumerable<BidRule> GetRules(BidXXX xxx, Direction direction, BiddingSummary biddingSummary)
		{
			// TODO: Make sure no interference
			// TODO: Check to make sure stayman is enabled
			
			if (xxx.Role == PositionRole.Responder && xxx.PartnersBid.Is(1, Suit.Unknown) && xxx.Round == 1)
			{
				return InitiateStayman();
			}
			return new BidRule[0];
		}

		public BidRule[] InitiateStayman()
		{
			BidRule[] rules =
			{
				Forcing(2, Suit.Clubs, Points(NTInviteOrBetter), Shape(Suit.Hearts, 4), Flat(false)),
				Forcing(2, Suit.Clubs, Points(NTInviteOrBetter), Shape(Suit.Spades, 4), Flat(false)),
				Forcing(2, Suit.Clubs, Points(NTGameOrBetter), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 5)),
				Forcing(2, Suit.Clubs, Points(NTGameOrBetter), Shape(Suit.Hearts, 5), Shape(Suit.Spades, 4)),
				// TODO: The following rule is "Garbage Stayman"
				//Forcing(2, Suit.Clubs, Points(NTLessThanInvite), Shape(Suit.Diamonds, 4, 5), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 4)),
			};
			return rules;
		}

		public BidRule[] AnswerStayman()
		{
			BidRule[] rules =
			{
				// TODO: Are these bids truly forcing?  Not if garbage stayman...
				Forcing(2, Suit.Diamonds, Shape(Suit.Hearts, 0, 3), Shape(Suit.Spades, 0, 3)),

				// If we are 4-4 then hearts bid before spades.  Can't be 5-5 or wouldn't be balanced.
				Forcing(2, Suit.Hearts, Shape(4, 5), LongerOrEqualTo(Suit.Spades)),

				Forcing(2, Suit.Spades, Shape(4, 5), LongerThan(Suit.Hearts)),
			};
			return rules;
		}

		public BidRule[] ExplainStayman()
		{
			BidRule[] rules =
			{
				Invitational(2, Suit.Unknown, Points(NTInvite), PartnerBid(2, Suit.Diamonds)),
				Invitational(2, Suit.Unknown, Points(NTInvite), PartnerBid(2, Suit.Hearts), Shape(Suit.Hearts, 0, 3)),
				Invitational(2, Suit.Unknown, Points(NTInvite), PartnerBid(2, Suit.Spades), Shape(Suit.Spades, 0, 3)),


				Invitational(3, Suit.Hearts, DummyPoints(NTInvite), PartnerBid(2, Suit.Hearts), Shape(4, 5)),
				Forcing(3, Suit.Hearts, DefaultPriority + 10, Points(NTGameOrBetter), Shape(5), PartnerBid(2, Suit.Diamonds)),


				Invitational(3, Suit.Spades, DummyPoints(NTInvite), PartnerBid(2, Suit.Spades), Shape(4, 5)),
				Forcing(3, Suit.Spades, DefaultPriority + 10, Points(NTGameOrBetter), Shape(5), PartnerBid(2, Suit.Diamonds)),

				Signoff(3, Suit.Unknown, Points(NTGame), PartnerBid(2, Suit.Diamonds)),
				Signoff(3, Suit.Unknown, Points(NTGame), PartnerBid(2, Suit.Hearts), Shape(Suit.Hearts, 2, 3)),
				Signoff(3, Suit.Unknown, Points(NTGame), PartnerBid(2, Suit.Spades), Shape(Suit.Spades, 2, 3)),

				Signoff(4, Suit.Hearts, Points(NTGame), PartnerBid(2, Suit.Hearts), Shape(4, 5)),

				Signoff(4, Suit.Spades, Points(NTGame), PartnerBid(2, Suit.Spades), Shape(4, 5))
			};
			return rules;
		}
		public BidRule[] PlaceContract()
		{
			BidRule[] rules =
			{
				NonForcing(3, Suit.Spades, Points(NTDontAcceptInvite), Shape(4), PartnerShows(Suit.Spades, 4)),

				Signoff(3, Suit.Unknown, Points(NTAcceptInvite), PartnerBid(2, Suit.Unknown)),
				Signoff(3, Suit.Unknown, Points(NTOpen), PreviousBid(2, Suit.Diamonds), PartnerBid(3, Suit.Hearts),
							Shape(Suit.Hearts, 2)),
				Signoff(3, Suit.Unknown, Points(NTOpen), PreviousBid(2, Suit.Diamonds), PartnerBid(3, Suit.Spades),
							Shape(Suit.Spades, 2)),
			
				Signoff(4, Suit.Hearts, Points(NTAcceptInvite), PartnerBid(3, Suit.Hearts), Shape(4, 5)),
				Signoff(4, Suit.Hearts, Points(NTOpen), PreviousBid(2, Suit.Diamonds), PartnerBid(3, Suit.Hearts), Shape(3)),


				Signoff(4, Suit.Spades, Points(NTAcceptInvite), PartnerBid(3, Suit.Spades), Shape(4, 5)),
				Signoff(4, Suit.Spades, Points(NTOpen), PreviousBid(2, Suit.Diamonds), PartnerBid(3, Suit.Spades), Shape(3)),
			};
			return rules;
		}

		public BidRule[] CheckGmae()
		{
			BidRule[] rules =
			{
				// Points as dummy here...
				Signoff(4, Suit.Spades, DummyPoints(NTGame), PartnerBid(3, Suit.Spades), Shape(4, 5)),
			};
			return rules;
		}

	}
}
