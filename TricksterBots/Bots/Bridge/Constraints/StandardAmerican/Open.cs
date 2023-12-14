using System;
using System.Collections.Generic;


namespace BridgeBidding
{
	public class Open: StandardAmerican
	{
        public static (int, int) OneLevel = (13, 21);
        public static (int, int) Weak = (5, 12);  // AG Bid card says 5-10 but when you count length you get 11 or 12
		public static (int, int) VeryWeak = (3, 12);
        public static (int, int) DontOpen = (0, 12);

        public static (int, int) Rebid1NT = (13, 14);
        public static (int, int) Rebid2NT = (18, 19);

        public static (int, int) Minimum = (13, 16);
        public static (int, int) Medium = (17, 18);
        public static (int, int) Maximum = (19, 21);


		public static (int, int) PairGameInvite = (23, 24);
		public static (int, int) PairGame = (25, 31);

        public static new BidChoices GetBidChoices(PositionState ps)
        {
            var choices = new BidChoices(ps);

            choices.AddRules(Strong2Clubs.Open);
            choices.AddRules(NoTrump.Open);
            choices.AddRules(OpenSuit1Level);
            choices.AddRules(OpenSuitWeak);
            choices.AddRules(new BidRule[] { Nonforcing(Bid.Pass, Points(DontOpen)) });
            return choices;
        }

        public static IEnumerable<BidRule> OpenSuit1Level(PositionState _)
		{
			return new List<BidRule>
			{
				DefaultPartnerBids(Call.Double, Respond.OppsDoubled),
                DefaultPartnerBids(new Bid(2, Strain.NoTrump), Respond.OppsOvercalled),

                PartnerBids(1, Strain.Clubs, Call.Pass, Respond.Club),
				PartnerBids(1, Strain.Diamonds, Call.Pass, Respond.Diamond),
				PartnerBids(1, Strain.Hearts, Call.Pass, Respond.Heart),
				PartnerBids(1, Strain.Spades, Call.Pass, Respond.Spade),

				Nonforcing(1, Strain.Clubs, Points(OneLevel), Shape(3), Shape(Suit.Diamonds, 0, 3), LongestMajor(4)),
				Nonforcing(1, Strain.Clubs, Points(OneLevel), Shape(4, 11), LongerThan(Suit.Diamonds), LongestMajor(4)),

				Nonforcing(1, Strain.Diamonds, Points(OneLevel), Shape(3), Shape(Suit.Clubs, 0, 2), LongestMajor(4)),
				Nonforcing(1, Strain.Diamonds, Points(OneLevel), Shape(4, 11), LongerOrEqualTo(Suit.Clubs), LongestMajor(4)),

				Nonforcing(1, Strain.Hearts, Points(OneLevel), Shape(5, 11), LongerThan(Suit.Spades)),

				Nonforcing(1, Strain.Spades, Points(OneLevel), Shape(5, 11), LongerOrEqualTo(Suit.Hearts)),
			};
		}

		public static IEnumerable<BidRule> OpenSuitWeak(PositionState _)
		{
			return new BidRule[]
			{
				DefaultPartnerBids(new Bid(4, Strain.Hearts), Respond.WeakOpen),

				// 2C can not be bid since strong opening.  Take care of great 6-card suits by bidding 3C
				Nonforcing(2, Strain.Diamonds, Points(Weak), Shape(6), GoodSuit()),
				Nonforcing(2, Strain.Hearts,   Points(Weak), Shape(6), GoodSuit()),
				Nonforcing(2, Strain.Spades,   Points(Weak), Shape(6), GoodSuit()),

				Nonforcing(3, Strain.Clubs,    Points(VeryWeak), Shape(6), ExcellentSuit()),
				Nonforcing(3, Strain.Clubs,    Points(VeryWeak), Shape(7), GoodSuit()),
				Nonforcing(3, Strain.Diamonds, Points(VeryWeak), Shape(7), GoodSuit()),
				Nonforcing(3, Strain.Hearts,   Points(VeryWeak), Shape(7), GoodSuit()),
				Nonforcing(3, Strain.Spades,   Points(VeryWeak), Shape(7), GoodSuit()),
				
                Nonforcing(4, Strain.Clubs,    Points(VeryWeak), Shape(8), DecentSuit()),
				Nonforcing(4, Strain.Diamonds, Points(VeryWeak), Shape(8), DecentSuit()),
				Nonforcing(4, Strain.Hearts,   Points(VeryWeak), Shape(8), DecentSuit()),
				Nonforcing(4, Strain.Spades,   Points(VeryWeak), Shape(8), DecentSuit()),

			};
		}

		// ***** REBID ROUND 1

        public static IEnumerable<BidRule> RebidPartnerChangedSuits(PositionState ps)
		{
			var bids = new List<BidRule>()
			{
				DefaultPartnerBids(Bid.Double, Respond.Rebid),

				//Nonforcing(1, Strain.Diamonds, Shape(4, 11)),
				Nonforcing(1, Strain.Hearts, Shape(4)),
				Nonforcing(1, Strain.Spades, Shape(4)),


				// Responder changed suits and we have a fit.  Support at appropriate level.
                Nonforcing(2, Strain.Clubs,  RaisePartner(), Points(Minimum)),
				Nonforcing(2, Strain.Diamonds, RaisePartner(), Points(Minimum)),
				Nonforcing(2, Strain.Hearts, RaisePartner(), Points(Minimum)),
				Nonforcing(2, Strain.Spades, RaisePartner(), Points(Minimum)),


				Nonforcing(3, Strain.Clubs, Fit(), ShowsTrump(), Points(Medium)),
				Nonforcing(3, Strain.Diamonds, Fit(), ShowsTrump(), Points(Medium)),
				Nonforcing(3, Strain.Hearts, Fit(), ShowsTrump(), Points(Medium)),
				Nonforcing(3, Strain.Spades, Fit(), ShowsTrump(), Points(Medium)),

				// TODO: What about minors.  This is bad. Think we want to fall through to 3NT...
                //Nonforcing(4, Strain.Clubs, DefaultPriority + 10, Fit(), ShowsTrump(), Points(MediumOpener)),
                //Nonforcing(4, Strain.Diamonds, DefaultPriority + 10, Fit(), ShowsTrump(), Points(MediumOpener)),
                Nonforcing(4, Strain.Hearts, Fit(), ShowsTrump(), Points(Maximum)),
				Nonforcing(4, Strain.Spades, Fit(), ShowsTrump(), Points(Maximum)),


				// Show a new suit at an appropriate level...
	//			Nonforcing(2, Strain.Clubs, Balanced(false), Points(MinimumOpener), LongestUnbidSuit()),
    //            Nonforcing(2, Strain.Clubs, Balanced(false), Points(MinimumOpener), LongestUnbidSuit()),
                Nonforcing(2, Strain.Hearts, LastBid(1, Strain.Hearts, false), Balanced(false), Points(Minimum), Shape(4, 6)),
        


				// Rebid a 6 card suit
				Nonforcing(2, Strain.Clubs, Rebid(), Shape(6, 11), Points(Minimum)),
				Nonforcing(2, Strain.Diamonds, Rebid(), Shape(6, 11), Points(Minimum)),
				Nonforcing(2, Strain.Hearts, Rebid(), Shape(6, 11), Points(Minimum)),
				Nonforcing(2, Strain.Spades, Rebid(), Shape(6, 11), Points(Minimum)),




				Nonforcing(3, Strain.Clubs, Rebid(), Shape(6, 11), Points(Medium)),
				Nonforcing(3, Strain.Diamonds, Rebid(), Shape(6, 11), Points(Medium)),
				Nonforcing(3, Strain.Hearts, Rebid(), Shape(6, 11), Points(Medium)),
				Nonforcing(3, Strain.Spades, Rebid(), Shape(6, 11), Points(Medium)),



				// Lowest priority if nothing else fits is bid NT
				Nonforcing(1, Strain.NoTrump, Balanced(), Points(Rebid1NT)),
				Nonforcing(2, Strain.NoTrump, Balanced(), Points(Rebid2NT)),
				// TODO: What about 3NT...

            };
			bids.AddRange(Compete.CompBids(ps));
			return bids;
		}

		public static IEnumerable<BidRule> RebidPartnerBidNT(PositionState ps, int level)
		{
			return RebidPartnerChangedSuits(ps);
			// TODO: Do something more here
		}


		public static IEnumerable<BidRule> RebidPartnerRaisedMinor(PositionState ps)
		{
			// TODO: More to do here...
			return Compete.CompBids(ps);
		}

		public static IEnumerable<BidRule> RebidPartnerRaisedMajor(PositionState ps)
		{
			var bids = new List<BidRule>()
			{
				PartnerBids(3, Strain.Hearts, new Bid(4, Strain.Diamonds), Respond.OpenerInvitedGame),
				PartnerBids(2, Strain.Spades, new Bid(4, Strain.Hearts), Respond.OpenerInvitedGame),

				Nonforcing(3, Strain.Hearts, Fit(), ShowsTrump(), PairPoints(PairGameInvite)),
				Nonforcing(3, Strain.Spades, Fit(), ShowsTrump(), PairPoints(PairGameInvite)),

                Nonforcing(4, Strain.Hearts, Fit(), ShowsTrump(), PairPoints(PairGame)),
				Nonforcing(4, Strain.Spades, Fit(), ShowsTrump(), PairPoints(PairGame)),

            };
			// Competative bids include Blackwood...
			bids.AddRange(Compete.CompBids(ps));
			return bids;
		}

	}
}
