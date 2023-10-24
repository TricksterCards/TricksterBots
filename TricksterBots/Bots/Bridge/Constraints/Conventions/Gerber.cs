using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public class Gerber : Bidder
	{
		// TODO: These values are wrong.  31 points seems too low....
		private static (int, int) SlamOrBetter = (31, 100);
		private static (int, int) SmallSlam = (31, 35);
		private static (int, int) GrandSlam = (36, 100);

		public static IEnumerable<BidRule> InitiateConvention(PositionState ps)
		{
			var bids = new List<BidRule>();
			// TODO: This is kind of a hack.  But for now just look for any 1 or 2 level NT bid.
			if (ps.Partner.LastCall is Call partnerCall && partnerCall is Bid partnerBid &&
				partnerBid.Strain == Strain.NoTrump && (partnerBid.Level <= 3))
			{
				bids.Add(PartnerBids(4, Suit.Clubs, Call.Double, RespondAces));
				bids.Add(Forcing(4, Suit.Clubs, PairPoints(SlamOrBetter)));
			}
			return bids;
		}
		public static IEnumerable<BidRule> RespondAces(PositionState ps)
		{
			return new BidRule[]
			{
				DefaultPartnerBids(goodThrough: Call.Double, PlaceContract),
				Forcing(4, Suit.Diamonds, ShowsNoSuit(), Aces(0, 4)),
				Forcing(4, Suit.Hearts, ShowsNoSuit(), Aces(1)),
				Forcing(4, Suit.Spades, ShowsNoSuit(), Aces(2)),
				Forcing(4, Suit.Unknown, ShowsNoSuit(), Aces(3)),
			};
		}
		// TODO: There needs to be somewhere that we ask for kings...
		public static IEnumerable<BidRule> PlaceContract(PositionState ps)
		{
			// TODO: Need to ask about kings..... 
			return new BidRule[]
			{
				Signoff(7, Suit.Unknown, PairPoints(GrandSlam), PairAces(4)),
				Signoff(6, Suit.Unknown, PairAces(3, 4)),
				Signoff(4, Suit.Unknown, PairAces(0, 1, 2))
			};
		}

	}
}
