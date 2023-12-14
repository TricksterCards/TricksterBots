using System;
using System.Collections.Generic;


namespace BridgeBidding
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
				bids.Add(Forcing(4, Strain.NoTrump, ShowsTrump(suit), PairPoints(suit, SlamOrBetter)));
				bids.Add(PartnerBids(4, Strain.NoTrump, Call.Double, RespondAces));
				// TODO: Add DOPI and DEPO but for now just ignore double and punt on interference...
			}
			return bids;
		}
		public static IEnumerable<BidRule> RespondAces(PositionState ps)
		{
			return new BidRule[]
			{
				DefaultPartnerBids(goodThrough: Call.Double, PlaceContract),
				Forcing(5, Strain.Clubs, ShowsNoSuit(), Aces(0, 4)),
				Forcing(5, Strain.Diamonds, ShowsNoSuit(), Aces(1)),
				Forcing(5, Strain.Hearts, ShowsNoSuit(), Aces(2)),
				Forcing(5, Strain.Spades, ShowsNoSuit(), Aces(3)),
			};
		}
		// TODO: There needs to be somewhere that we ask for kings...
		public static IEnumerable<BidRule> PlaceContract(PositionState ps)
		{
			// If we are missing 2 or more aces return to trump suit if necessary (may just pass).
			// Otherwise 
			return new BidRule[]
			{
				PartnerBids(5, Strain.NoTrump, Call.Double, RespondKings),
				Forcing(5, Strain.NoTrump, PairAces(4), PairPoints(GrandSlam)),

				Signoff(6, Strain.Clubs, AgreedStrain(), PairPoints(SlamOrBetter), PairAces(3, 4)),
				Signoff(6, Strain.Diamonds, AgreedStrain(), PairPoints(SlamOrBetter), PairAces(3, 4)),
				Signoff(6, Strain.Hearts, AgreedStrain(), PairPoints(SlamOrBetter), PairAces(3, 4)),
				Signoff(6, Strain.Spades, AgreedStrain(), PairPoints(SlamOrBetter), PairAces(3, 4)),

				Signoff(Call.Pass, ContractIsAgreedStrain(), PairAces(0, 1, 2)),

                Signoff(5, Strain.Diamonds, AgreedStrain(), PairAces(0, 1, 2)),
                Signoff(5, Strain.Hearts, AgreedStrain(), PairAces(0, 1, 2)),
                Signoff(5, Strain.Spades, AgreedStrain(), PairAces(0, 1, 2)),

                Signoff(6, Strain.Clubs,  Jump(0), AgreedStrain()),
                Signoff(6, Strain.Diamonds, Jump(0), AgreedStrain()),
                Signoff(6, Strain.Hearts, Jump(0), AgreedStrain()),
            };
		}

		public static IEnumerable<BidRule> RespondKings(PositionState ps)
		{
			return new BidRule[]
			{
				DefaultPartnerBids(goodThrough: Call.Double, TryGrandSlam),
				Forcing(6, Strain.Clubs, ShowsNoSuit(), Kings(0, 4)),
				Forcing(6, Strain.Diamonds, ShowsNoSuit(), Kings(1)),
				Forcing(6, Strain.Hearts, ShowsNoSuit(), Kings(2)),
				Forcing(6, Strain.Spades, ShowsNoSuit(), Kings(3)),
			};
		}

		public static IEnumerable<BidRule> TryGrandSlam(PositionState ps)
		{
			return new BidRule[]
			{
				Signoff(7, Strain.Clubs, AgreedStrain(), PairPoints(GrandSlam), PairAces(4), PairKings(4)),
				Signoff(7, Strain.Diamonds, AgreedStrain(), PairPoints(GrandSlam), PairAces(4), PairKings(4)),
				Signoff(7, Strain.Hearts, AgreedStrain(), PairPoints(GrandSlam), PairAces(4), PairKings(4)),
				Signoff(7, Strain.Spades, AgreedStrain(), PairPoints(GrandSlam), PairAces(4), PairKings(4)),

				Signoff(Call.Pass, ContractIsAgreedStrain()),

				Signoff(6, Strain.Diamonds, AgreedStrain()),
				Signoff(6, Strain.Hearts, AgreedStrain()),
				Signoff(6, Strain.Spades, AgreedStrain()),

				// We may have no choice but to go to 7.  Perhaps bid 6NT?  Otherwise gotta go 7 clubs->hearts
				Signoff(7, Strain.Clubs, AgreedStrain()),
				Signoff(7, Strain.Diamonds, AgreedStrain()),
				Signoff(7, Strain.Hearts, AgreedStrain())
			};
		}

	}
}
