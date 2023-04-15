using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public class NaturalOpen : Natural
	{
		public NaturalOpen() : base(PositionRole.Opener) { }

		public override IEnumerable<BidRule> GetRules(BidXXX xxx, Direction direction, BiddingSummary biddingSummary)
		{
			Debug.Assert(xxx.Role == PositionRole.Opener);
			if (xxx.Round == 1)
			{
				return Open();
			}
			else if (xxx.Round == 2)
			{
				return Rebid();
			}
			BidRule[] bids = new BidRule[0];
			return bids;
		}

		public BidRule[] Open() 
		{
			BidRule[] bids =
			{
				NonForcing(1, Suit.Clubs, Points(Open1Suit), Shape(3), Shape(Suit.Diamonds, 0, 3), LongestMajor(4)),
				NonForcing(1, Suit.Clubs, Points(Open1Suit), Shape(4, 11), LongerThan(Suit.Diamonds), LongestMajor(4)),

				NonForcing(1, Suit.Diamonds, Points(Open1Suit),Shape(3), Shape(Suit.Clubs, 0, 2), LongestMajor(4)),
				NonForcing(1, Suit.Diamonds, Points(Open1Suit), Shape(4, 11), LongerOrEqualTo(Suit.Clubs), LongestMajor(4)),

				NonForcing(1, Suit.Hearts, Points(Open1Suit), Shape(5, 11), LongerThan(Suit.Spades)),

				NonForcing(1, Suit.Spades, Points(Open1Suit), Shape(5, 11), LongerOrEqualTo(Suit.Hearts)),

				NonForcing(1, Suit.Unknown, DefaultPriority + 100, Points(Open1NT), Balanced()),

				// NOTE: Strong open will override this - 2C Conventional will always be possible so
				// this rule would be silly.
				//Rule(2, Suit.Clubs, Points(Open2Suit), Shape(6), Quality(SuitQuality.Good)),

				NonForcing(2, Suit.Diamonds, Points(Open2Suit), Shape(6), Quality(SuitQuality.Good)),

				NonForcing(2, Suit.Hearts, Points(Open2Suit), Shape(6), Quality(SuitQuality.Good)),

				NonForcing(2, Suit.Spades, Points(Open2Suit), Shape(6), Quality(SuitQuality.Good)),

				NonForcing(2, Suit.Unknown, DefaultPriority + 100, Points(Open2NT), Balanced()),

				NonForcing(3, Suit.Clubs, 0, Points(LessThanOpen), Shape(7), Quality(SuitQuality.Good)),

				NonForcing(3, Suit.Diamonds, 0, Points(LessThanOpen), Shape(7), Quality(SuitQuality.Good)),

				NonForcing(3, Suit.Hearts, 0, Points(LessThanOpen), Shape(7), Quality(SuitQuality.Good)),

				NonForcing(3, Suit.Spades, 0, Points(LessThanOpen), Shape(7), Quality(SuitQuality.Good)),

				NonForcing(6, Suit.Clubs, 1000, Shape(12)),
				NonForcing(6, Suit.Diamonds, 1000, Shape(12)),
				NonForcing(6, Suit.Hearts, 1000, Shape(12)),
				NonForcing(6, Suit.Spades, 1000, Shape(12)),

				NonForcing(7, Suit.Clubs, 1000, Shape(13)),
				NonForcing(7, Suit.Diamonds, 1000, Shape(13)),
				NonForcing(7, Suit.Hearts, 1000, Shape(13)),
				NonForcing(7, Suit.Spades, 1000, Shape(13)),
			};
			return bids;
		}

		public BidRule[] Rebid()
		{
			BidRule[] bids =
			{
				
			};
			return bids;
		}


	}
}
