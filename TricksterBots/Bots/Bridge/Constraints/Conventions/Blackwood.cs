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
        private static (int, int) SlamOrBetter = (32, 100);
		private static (int, int) SmallSlam = (32, 35);
        private static (int, int) GrandSlam = (36, 100);

        public static IEnumerable<BidRule> InitiateConvention(PositionState ps)
		{
			var bids = new List<BidRule>();
			Strain? strain = ps.PairState.Agreements.AgreedStrain;
			if (strain == null)
			{
				strain = ps.PairState.Agreements.LastShownStrain;
			}
			if (strain != null && Call.StrainToSuit((Strain)strain) is Suit suit)
			{
				bids.Add(Forcing(4, Suit.Unknown, ShowsTrump(suit), PairPoints(suit, SlamOrBetter)));
				bids.Add(PartnerBids(4, Suit.Unknown, Call.Double, RespondAces));
				// TODO: Add DOPI and DEPO but for now just ignore double and punt on interference...
			}
			return bids;
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
				PartnerBids(5, Suit.Unknown, Call.Double, RespondKings),
				Forcing(5, Suit.Unknown, PairAces(4), PairPoints(GrandSlam)),

				Signoff(6, Suit.Clubs, AgreedStrain(), PairPoints(SlamOrBetter), PairAces(3, 4)),
				Signoff(6, Suit.Diamonds, AgreedStrain(), PairPoints(SlamOrBetter), PairAces(3, 4)),
				Signoff(6, Suit.Hearts, AgreedStrain(), PairPoints(SlamOrBetter), PairAces(3, 4)),
				Signoff(6, Suit.Spades, AgreedStrain(), PairPoints(SlamOrBetter), PairAces(3, 4)),

				Signoff(Call.Pass, ContractIsAgreedStrain(), PairAces(0, 1, 2)),

                Signoff(5, Suit.Diamonds, AgreedStrain(), PairAces(0, 1, 2)),
                Signoff(5, Suit.Hearts, AgreedStrain(), PairAces(0, 1, 2)),
                Signoff(5, Suit.Spades, AgreedStrain(), PairAces(0, 1, 2)),

                Signoff(6, Suit.Clubs,  Jump(0), AgreedStrain()),
                Signoff(6, Suit.Diamonds, Jump(0), AgreedStrain()),
                Signoff(6, Suit.Hearts, Jump(0), AgreedStrain()),
            };
		}

		public static IEnumerable<BidRule> RespondKings(PositionState ps)
		{
			return new BidRule[]
			{
				DefaultPartnerBids(goodThrough: Call.Double, TryGrandSlam),
				Forcing(6, Suit.Clubs, ShowsNoSuit(), Kings(0, 4)),
				Forcing(6, Suit.Diamonds, ShowsNoSuit(), Kings(1)),
				Forcing(6, Suit.Hearts, ShowsNoSuit(), Kings(2)),
				Forcing(6, Suit.Spades, ShowsNoSuit(), Kings(3)),
			};
		}

		public static IEnumerable<BidRule> TryGrandSlam(PositionState ps)
		{
			return new BidRule[]
			{
				Signoff(7, Suit.Clubs, AgreedStrain(), PairPoints(GrandSlam), PairAces(4), PairKings(4)),
				Signoff(7, Suit.Diamonds, AgreedStrain(), PairPoints(GrandSlam), PairAces(4), PairKings(4)),
				Signoff(7, Suit.Hearts, AgreedStrain(), PairPoints(GrandSlam), PairAces(4), PairKings(4)),
				Signoff(7, Suit.Spades, AgreedStrain(), PairPoints(GrandSlam), PairAces(4), PairKings(4)),

				Signoff(Call.Pass, ContractIsAgreedStrain()),

				Signoff(6, Suit.Diamonds, AgreedStrain()),
				Signoff(6, Suit.Hearts, AgreedStrain()),
				Signoff(6, Suit.Spades, AgreedStrain()),

				// We may have no choice but to go to 7.  Perhaps bid 6NT?  Otherwise gotta go 7 clubs->hearts
				Signoff(7, Suit.Clubs, AgreedStrain()),
				Signoff(7, Suit.Diamonds, AgreedStrain()),
				Signoff(7, Suit.Hearts, AgreedStrain())
			};
		}

	}
}
