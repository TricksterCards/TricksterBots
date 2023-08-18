using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;
using TricksterBots.Bots.Bridge;

namespace TricksterBots.Bots.Bridge
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
                DefaultPartnerBids(new Bid(1, Suit.Unknown), Respond.OppsOvercalled),

                PartnerBids(1, Suit.Clubs, Call.Pass, Respond.Club),
				PartnerBids(1, Suit.Diamonds, Call.Pass, Respond.Diamond),
				PartnerBids(1, Suit.Hearts, Call.Pass, Respond.Heart),
				PartnerBids(1, Suit.Spades, Call.Pass, Respond.Spade),

				Nonforcing(1, Suit.Clubs, Points(OneLevel), Shape(3), Shape(Suit.Diamonds, 0, 3), LongestMajor(4)),
				Nonforcing(1, Suit.Clubs, Points(OneLevel), Shape(4, 11), LongerThan(Suit.Diamonds), LongestMajor(4)),

				Nonforcing(1, Suit.Diamonds, Points(OneLevel), Shape(3), Shape(Suit.Clubs, 0, 2), LongestMajor(4)),
				Nonforcing(1, Suit.Diamonds, Points(OneLevel), Shape(4, 11), LongerOrEqualTo(Suit.Clubs), LongestMajor(4)),

				Nonforcing(1, Suit.Hearts, Points(OneLevel), Shape(5, 11), LongerThan(Suit.Spades)),

				Nonforcing(1, Suit.Spades, Points(OneLevel), Shape(5, 11), LongerOrEqualTo(Suit.Hearts)),
			};
		}

		public static IEnumerable<BidRule> OpenSuitWeak(PositionState _)
		{
			return new BidRule[]
			{
				DefaultPartnerBids(new Bid(4, Suit.Hearts), Respond.WeakOpen),

				// 2C can not be bid since strong opening.  Take care of great 6-card suits by bidding 3C
				Nonforcing(2, Suit.Diamonds, Points(Weak), Shape(6), GoodSuit()),
				Nonforcing(2, Suit.Hearts,   Points(Weak), Shape(6), GoodSuit()),
				Nonforcing(2, Suit.Spades,   Points(Weak), Shape(6), GoodSuit()),

				Nonforcing(3, Suit.Clubs,    Points(VeryWeak), Shape(6), ExcellentSuit()),
				Nonforcing(3, Suit.Clubs,    Points(VeryWeak), Shape(7), GoodSuit()),
				Nonforcing(3, Suit.Diamonds, Points(VeryWeak), Shape(7), GoodSuit()),
				Nonforcing(3, Suit.Hearts,   Points(VeryWeak), Shape(7), GoodSuit()),
				Nonforcing(3, Suit.Spades,   Points(VeryWeak), Shape(7), GoodSuit()),
				
                Nonforcing(4, Suit.Clubs,    Points(VeryWeak), Shape(8), DecentSuit()),
				Nonforcing(4, Suit.Diamonds, Points(VeryWeak), Shape(8), DecentSuit()),
				Nonforcing(4, Suit.Hearts,   Points(VeryWeak), Shape(8), DecentSuit()),
				Nonforcing(4, Suit.Spades,   Points(VeryWeak), Shape(8), DecentSuit()),

			};
		}


        public static IEnumerable<BidRule> Rebid(PositionState _)
		{
			return new List<BidRule>()
			{
				DefaultPartnerBids(Bid.Double, Respond.Rebid),

				// TODO: These seem silly.  Especially diamonds...
				Nonforcing(1, Suit.Diamonds, Shape(4, 11)),
				Nonforcing(1, Suit.Hearts, Shape(4, 11)),
				Nonforcing(1, Suit.Spades, Shape(4, 11)),



				// Responder changed suits and we have a fit.  Support at appropriate level.
                Nonforcing(2, Suit.Clubs,  Fit(), ShowsTrump(), Points(Minimum)),
				Nonforcing(2, Suit.Diamonds, Fit(), ShowsTrump(), Points(Minimum)),
				Nonforcing(2, Suit.Hearts, Fit(), ShowsTrump(), Points(Minimum)),
				Nonforcing(2, Suit.Spades, Fit(), ShowsTrump(), Points(Minimum)),


				Nonforcing(3, Suit.Clubs, Fit(), ShowsTrump(), Points(Medium)),
				Nonforcing(3, Suit.Diamonds, Fit(), ShowsTrump(), Points(Medium)),
				Nonforcing(3, Suit.Hearts, Fit(), ShowsTrump(), Points(Medium)),
				Nonforcing(3, Suit.Spades, Fit(), ShowsTrump(), Points(Medium)),

				// TODO: What about minors.  This is bad. Think we want to fall through to 3NT...
                //Nonforcing(4, Suit.Clubs, DefaultPriority + 10, Fit(), ShowsTrump(), Points(MediumOpener)),
                //Nonforcing(4, Suit.Diamonds, DefaultPriority + 10, Fit(), ShowsTrump(), Points(MediumOpener)),
                Nonforcing(4, Suit.Hearts, Fit(), ShowsTrump(), Points(Maximum)),
				Nonforcing(4, Suit.Spades, Fit(), ShowsTrump(), Points(Maximum)),


				// Show a new suit at an appropriate level...
	//			Nonforcing(2, Suit.Clubs, Balanced(false), Points(MinimumOpener), LongestUnbidSuit()),
    //            Nonforcing(2, Suit.Clubs, Balanced(false), Points(MinimumOpener), LongestUnbidSuit()),
                Nonforcing(2, Suit.Hearts, LastBid(1, Suit.Hearts, false), Balanced(false), Points(Minimum), Shape(4, 6)),
        


				// Rebid a 6 card suit
				Nonforcing(2, Suit.Clubs, Rebid(), Shape(6, 11), Points(Minimum)),
				Nonforcing(2, Suit.Diamonds, Rebid(), Shape(6, 11), Points(Minimum)),
				Nonforcing(2, Suit.Hearts, Rebid(), Shape(6, 11), Points(Minimum)),
				Nonforcing(2, Suit.Spades, Rebid(), Shape(6, 11), Points(Minimum)),




				Nonforcing(3, Suit.Clubs, Rebid(), Shape(6, 11), Points(Medium)),
				Nonforcing(3, Suit.Diamonds, Rebid(), Shape(6, 11), Points(Medium)),
				Nonforcing(3, Suit.Hearts, Rebid(), Shape(6, 11), Points(Medium)),
				Nonforcing(3, Suit.Spades, Rebid(), Shape(6, 11), Points(Medium)),



				// Lowest priority if nothing else fits is bid NT
				Nonforcing(1, Suit.Unknown, Balanced(), Points(Rebid1NT)),
				Nonforcing(2, Suit.Unknown, Balanced(), Points(Rebid2NT)),
				// TODO: What about 3NT...

            };
		}



	}
}
