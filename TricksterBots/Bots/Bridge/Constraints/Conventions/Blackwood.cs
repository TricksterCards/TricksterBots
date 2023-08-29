using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public class Blackwood : Bidder
	{
		public static IEnumerable<BidRule> InitiateConvention(PositionState ps)
		{
			return new BidRule[]
			{
				DefaultPartnerBids(Call.Double, RespondAces),
				// TODO: Add DOPI and DEPO but for now just ignore double and punt on interference...
				Forcing(4, Suit.Unknown, PairPoints((33, 100)))
			}; 
		}
		public static IEnumerable<BidRule> RespondAces(PositionState ps)
		{
			return new BidRule[]
			{
				DefaultPartnerBids(Call.Double, PlaceContract),
				Forcing(4, Suit.Clubs, ShowsNoSuit(), Aces(0, 4)),
				Forcing(4, Suit.Diamonds, ShowsNoSuit(), Aces(1)),
				Forcing(4, Suit.Hearts, ShowsNoSuit(), Aces(2)),
				Forcing(4, Suit.Spades, ShowsNoSuit(), Aces(3)),
			};
		}
		public static IEnumerable<BidRule> PlaceContract(PositionState ps)
		{
			// If we are missing 2 or more aces return to trump suit if necessary (may just pass).
			// Otherwise 
			return new BidRule[]
			{

				// TODO: Do something here....
			};
		}

	}
}
