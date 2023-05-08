﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;
using TricksterBots.Bots.Bridge;

namespace TricksterBots.Bots.Bridge
{
	public class NaturalOpen : Natural
	{
		public static Bidder Open() { return new NaturalOpen(); }

		public NaturalOpen() : base() 
		{
			this.ConventionRules = new ConventionRule[]
			{
				new ConventionRule(Role(PositionRole.Opener), BidRound(1))
			};
			this.BidRules = new BidRule[]
			{
				Nonforcing(CallType.Pass, DefaultPriority - 100, Points(LessThanOpen)),

				Nonforcing(1, Suit.Clubs, Points(Open1Suit), Shape(3), Shape(Suit.Diamonds, 0, 3), LongestMajor(4)),
				Nonforcing(1, Suit.Clubs, Points(Open1Suit), Shape(4, 11), LongerThan(Suit.Diamonds), LongestMajor(4)),

				Nonforcing(1, Suit.Diamonds, Points(Open1Suit),Shape(3), Shape(Suit.Clubs, 0, 2), LongestMajor(4)),
				Nonforcing(1, Suit.Diamonds, Points(Open1Suit), Shape(4, 11), LongerOrEqualTo(Suit.Clubs), LongestMajor(4)),

				Nonforcing(1, Suit.Hearts, Points(Open1Suit), Shape(5, 11), LongerThan(Suit.Spades)),

				Nonforcing(1, Suit.Spades, Points(Open1Suit), Shape(5, 11), LongerOrEqualTo(Suit.Hearts)),

				// 1NT rule(s) in NoTrump class.

				// NOTE: Strong open will override this - 2C Conventional will always be possible so
				// this rule would be silly.
				//Rule(2, Suit.Clubs, Points(Open2Suit), Shape(6), Quality(SuitQuality.Good)),

				Nonforcing(2, Suit.Diamonds, Points(Open2Suit), Shape(6), GoodSuit()),

				Nonforcing(2, Suit.Hearts, Points(Open2Suit), Shape(6), GoodSuit()),

				Nonforcing(2, Suit.Spades, Points(Open2Suit), Shape(6), GoodSuit()),

				// 2NT rule(s) in NoTrump class.
			
				Nonforcing(3, Suit.Clubs, Points(LessThanOpen), Shape(7), GoodSuit()),
				Nonforcing(3, Suit.Diamonds, Points(LessThanOpen), Shape(7), GoodSuit()),
				Nonforcing(3, Suit.Hearts, Points(LessThanOpen), Shape(7), GoodSuit()),
				Nonforcing(3, Suit.Spades, Points(LessThanOpen), Shape(7), GoodSuit()),

				// 3NT rule(s) in NoTrump class.
				
                Nonforcing(4, Suit.Clubs, Points(LessThanOpen), Shape(8), DecentSuit()),
                Nonforcing(4, Suit.Diamonds, Points(LessThanOpen), Shape(8), DecentSuit()),
                Nonforcing(4, Suit.Hearts, Points(LessThanOpen), Shape(8), DecentSuit()),
                Nonforcing(4, Suit.Spades, Points(LessThanOpen), Shape(8), DecentSuit()),


                Nonforcing(6, Suit.Clubs, Shape(12)),
				Nonforcing(6, Suit.Diamonds, Shape(12)),
				Nonforcing(6, Suit.Hearts, Shape(12)),
				Nonforcing(6, Suit.Spades, Shape(12)),

				Nonforcing(7, Suit.Clubs, Shape(13)),
				Nonforcing(7, Suit.Diamonds, Shape(13)),
				Nonforcing(7, Suit.Hearts, Shape(13)),
				Nonforcing(7, Suit.Spades, Shape(13)),
			};
			SetPartnerBidder(() => new NaturalRespond());
        }




	}
    public class NaturalOpenerRebid : Natural
    {
        public NaturalOpenerRebid() : base()
        {
			this.BidRules = new List<BidRule>()
			{
				Nonforcing(1, Suit.Diamonds, Shape(4, 11)),
				Nonforcing(1, Suit.Hearts, Shape(4, 11)),
				Nonforcing(1, Suit.Spades, Shape(4, 11)),

				Nonforcing(1, Suit.Unknown, DefaultPriority - 10, Balanced(), Points(OpenerRebid1NT)),

				// All the possible rebids of a suit.
				Nonforcing(2, Suit.Clubs, LastBid(1), Shape(6, 11), Points(MinimumOpener)),
				Nonforcing(2, Suit.Diamonds, LastBid(1), Shape(6, 11), Points(MinimumOpener)),
				Nonforcing(2, Suit.Hearts, LastBid(1), Shape(6, 11), Points(MinimumOpener)),
				Nonforcing(2, Suit.Spades, LastBid(1), Shape(6, 11), Points(MinimumOpener))



			};
        }

    }
}
