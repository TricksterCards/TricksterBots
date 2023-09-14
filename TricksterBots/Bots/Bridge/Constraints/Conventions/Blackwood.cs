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
        private static (int, int) SlamOrBetter = (33, 100);
		private static (int, int) SmallSlam = (33, 35);
        private static (int, int) GrandSlam = (36, 100);

        public static IEnumerable<BidRule> InitiateConvention(PositionState ps)
		{
			return new BidRule[]
			{
				PartnerBids(4, Suit.Unknown, Call.Double, RespondAces),
				// TODO: Add DOPI and DEPO but for now just ignore double and punt on interference...
				Forcing(4, Suit.Unknown, AgreedStrainPoints(SlamOrBetter), AgreedAnySuit())
			}; 
		}
		public static IEnumerable<BidRule> RespondAces(PositionState ps)
		{
			return new BidRule[]
			{
				DefaultPartnerBids(goodThrough: Call.Double, PlaceContract),
				Forcing(5, Suit.Clubs, ShowsNoSuit(), Aces(0, 4)),
				Forcing(5, Suit.Diamonds, ShowsNoSuit(), Aces(1)),
				Forcing(5, Suit.Hearts, ShowsNoSuit(), Aces(2)),
				Forcing(5, Suit.Spades, ShowsNoSuit(), Aces(3)),
			};
		}
		// TODO: There needs to be somewhere that we ask for kings...
		public static IEnumerable<BidRule> PlaceContract(PositionState ps)
		{
			// If we are missing 2 or more aces return to trump suit if necessary (may just pass).
			// Otherwise 
			return new BidRule[]
			{
				Signoff(7, Suit.Clubs, AgreedStrain(), Points(GrandSlam), PairAces(4)),
				Signoff(7, Suit.Diamonds, AgreedStrain(), Points(GrandSlam), PairAces(4)),
				Signoff(7, Suit.Hearts, AgreedStrain(), Points(GrandSlam), PairAces(4)),
				Signoff(7, Suit.Spades, AgreedStrain(), Points(GrandSlam), PairAces(4)),

				Signoff(6, Suit.Clubs, AgreedStrain(), Points(SlamOrBetter), PairAces(3, 4)),
				Signoff(6, Suit.Diamonds, AgreedStrain(), Points(SlamOrBetter), PairAces(3, 4)),
				Signoff(6, Suit.Hearts, AgreedStrain(), Points(SlamOrBetter), PairAces(3, 4)),
				Signoff(6, Suit.Spades, AgreedStrain(), Points(SlamOrBetter), PairAces(3, 4)),

				Signoff(Call.Pass, ContractIsAgreedStrain(), PairAces(0, 1, 2)),

                Signoff(5, Suit.Diamonds, AgreedStrain(), PairAces(0, 1, 2)),
                Signoff(5, Suit.Hearts, AgreedStrain(), PairAces(0, 1, 2)),
                Signoff(5, Suit.Spades, AgreedStrain(), PairAces(0, 1, 2)),

                Signoff(6, Suit.Clubs,  Jump(0), AgreedStrain()),
                Signoff(6, Suit.Diamonds, Jump(0), AgreedStrain()),
                Signoff(6, Suit.Hearts, Jump(0), AgreedStrain()),

            };
		}

	}
}
