using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;
using TricksterBots.Bots.Bridge.TricksterBots.Bots.Bridge;

namespace TricksterBots.Bots.Bridge
{
	public class NaturalOpen : Natural
	{
		public NaturalOpen() : base() 
		{
			this.ConventionRules = new ConventionRule[]
			{
				new ConventionRule(Role(PositionRole.Opener), BidRound(1))
			};
			this.BidRules = new BidRule[]
			{
				NonForcing(CallType.Pass, DefaultPriority - 100, Points(LessThanOpen)),

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

				NonForcing(2, Suit.Diamonds, Points(Open2Suit), Shape(6), Quality(SuitQuality.Good, SuitQuality.Solid)),

				NonForcing(2, Suit.Hearts, Points(Open2Suit), Shape(6), Quality(SuitQuality.Good, SuitQuality.Solid)),

				NonForcing(2, Suit.Spades, Points(Open2Suit), Shape(6), Quality(SuitQuality.Good, SuitQuality.Solid)),

				NonForcing(2, Suit.Unknown, DefaultPriority + 100, Points(Open2NT), Balanced()),

				NonForcing(3, Suit.Clubs, Points(LessThanOpen), Shape(7), Quality(SuitQuality.Good, SuitQuality.Solid)),

				NonForcing(3, Suit.Diamonds, Points(LessThanOpen), Shape(7), Quality(SuitQuality.Good, SuitQuality.Solid)),

				NonForcing(3, Suit.Hearts, Points(LessThanOpen), Shape(7), Quality(SuitQuality.Good, SuitQuality.Solid)),

				NonForcing(3, Suit.Spades, Points(LessThanOpen), Shape(7), Quality(SuitQuality.Good, SuitQuality.Solid)),

				NonForcing(6, Suit.Clubs, Shape(12)),
				NonForcing(6, Suit.Diamonds, Shape(12)),
				NonForcing(6, Suit.Hearts, Shape(12)),
				NonForcing(6, Suit.Spades, Shape(12)),

				NonForcing(7, Suit.Clubs, Shape(13)),
				NonForcing(7, Suit.Diamonds, Shape(13)),
				NonForcing(7, Suit.Hearts, Shape(13)),
				NonForcing(7, Suit.Spades, Shape(13)),
			};
			this.NextConventionState = () => new NaturalRespond();
        }

		public BidRule[] Opener2ndBid()
		{
			return new BidRule[] 
			{
				
			};
		}


	}
}
