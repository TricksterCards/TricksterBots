﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	namespace TricksterBots.Bots.Bridge
	{


		public class NaturalRespond : Natural
		{
			public NaturalRespond() : base()
			{
				this.Redirects = new RedirectRule[]
				{
					new RedirectRule(() => new RespondTo1C(), Partner(LastBid(1, Suit.Clubs)), RHO(Passed())),
					new RedirectRule(() => new RespondTo1D(), Partner(LastBid(1, Suit.Diamonds)), RHO(Passed())),
					new RedirectRule(() => new RespondTo1H(), Partner(LastBid(1, Suit.Hearts)), RHO(Passed())),
					new RedirectRule(() => new RespondTo1S(), Partner(LastBid(1, Suit.Spades)), RHO(Passed()))
				};
			}


			static protected (int, int) Respond1Level = (6, 40);
			static protected (int, int) Raise1 = (6, 10);
			static protected (int, int) Respond1NT = (6, 10);
			static protected (int, int) NewSuit2Level = (12, 40);  // TODO: null??
			static protected (int, int) RaiseTo2NT = (11, 12);
			static protected (int, int) SlamInterest = (17, 40);
			static protected (int, int) LimitRaise = (11, 12);
			static protected (int, int) RaiseTo3NT = (13, 15);
			static protected (int, int) Weak4Level = (0, 10);
			static protected (int, int) GameOrBetter = (13, 40);

            protected BidRule[] NewMinorSuit2Level(Suit openersSuit)
            {
                return new BidRule[]
                {
                    Forcing(2, Suit.Clubs, Points(NewSuit2Level), Shape(4, 5), Shape(Suit.Diamonds, 0, 4)),
                    Forcing(2, Suit.Clubs, Points(NewSuit2Level), Shape(6), Shape(Suit.Diamonds, 0, 5)),
                    Forcing(2, Suit.Clubs, Points(NewSuit2Level), Shape(7, 11)),
                    Forcing(2, Suit.Clubs, DummyPoints(openersSuit, LimitRaise), Shape(3), Shape(openersSuit, 3), Shape(Suit.Diamonds, 0, 3)),
                    Forcing(2, Suit.Clubs, DummyPoints(openersSuit, LimitRaise), Shape(4, 5), Shape(openersSuit, 3), Shape(Suit.Diamonds, 0, 4)),
                    Forcing(2, Suit.Clubs, DummyPoints(openersSuit, LimitRaise), Shape(6), Shape(openersSuit, 3)),
                    Forcing(2, Suit.Clubs, DummyPoints(openersSuit, GameOrBetter), Shape(3), Shape(openersSuit, 3, 11), Shape(Suit.Diamonds, 0, 3)),
                    Forcing(2, Suit.Clubs, DummyPoints(openersSuit, GameOrBetter), Shape(4, 5), Shape(openersSuit, 3, 11), Shape(Suit.Diamonds, 0, 4)),
                    Forcing(2, Suit.Clubs, DummyPoints(openersSuit, GameOrBetter), Shape(6, 11), Shape(openersSuit, 3, 11)),


                    Forcing(2, Suit.Diamonds, Points(NewSuit2Level), Shape(4), Shape(Suit.Clubs, 0, 3)),
                    Forcing(2, Suit.Diamonds, Points(NewSuit2Level), Shape(5), Shape(Suit.Clubs, 0, 5)),
                    Forcing(2, Suit.Diamonds, Points(NewSuit2Level), Shape(6), Shape(Suit.Clubs, 0, 6)),
                    Forcing(2, Suit.Diamonds, Points(NewSuit2Level), Shape(7, 11)),
                    Forcing(2, Suit.Diamonds, DummyPoints(openersSuit, LimitRaise), Shape(3), Shape(openersSuit, 3), Shape(Suit.Clubs, 0, 2)),
                    Forcing(2, Suit.Diamonds, DummyPoints(openersSuit, LimitRaise), Shape(4), Shape(openersSuit, 3), Shape(Suit.Clubs, 0, 3)),
                    Forcing(2, Suit.Diamonds, DummyPoints(openersSuit, LimitRaise), Shape(5), Shape(openersSuit, 3), Shape(Suit.Clubs, 0, 5)),
                    Forcing(2, Suit.Diamonds, DummyPoints(openersSuit, LimitRaise), Shape(6, 11), Shape(openersSuit, 3)),
                    Forcing(2, Suit.Diamonds, DummyPoints(openersSuit, GameOrBetter), Shape(3), Shape(openersSuit, 3, 11), Shape(Suit.Clubs, 0, 2)),
                    Forcing(2, Suit.Diamonds, DummyPoints(openersSuit, GameOrBetter), Shape(4), Shape(openersSuit, 3, 11), Shape(Suit.Clubs, 0, 3)),
                    Forcing(2, Suit.Diamonds, DummyPoints(openersSuit, GameOrBetter), Shape(5), Shape(openersSuit, 3, 11), Shape(Suit.Clubs, 0, 5)),
                    Forcing(2, Suit.Diamonds, DummyPoints(openersSuit, GameOrBetter), Shape(6, 11), Shape(openersSuit, 3, 11)),
				};
            }
        }

		public class RespondTo1C : NaturalRespond
		{

			public RespondTo1C() : base()
			{
				this.BidRules = new List<BidRule>()
				{
					Forcing(1, Suit.Diamonds, Points(Respond1Level), Shape(4, 5), LongestMajor(4)),
					Forcing(1, Suit.Diamonds, Points(Respond1Level), Shape(6), LongestMajor(5)),
					Forcing(1, Suit.Diamonds, Points(Respond1Level), Shape(7, 11), LongestMajor(6)),

					Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(4), Shape(Suit.Diamonds, 0, 3), Shape(Suit.Spades, 0, 4)),
					Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(5), Shape(Suit.Diamonds, 0, 5), Shape(Suit.Spades, 0, 4)),
					Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(6), Shape(Suit.Diamonds, 0, 6), Shape(Suit.Spades, 0, 5)),
					Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(7, 11)),

					Forcing(1, Suit.Spades, Points(Respond1Level), Shape(4), Shape(Suit.Diamonds, 0, 3), Shape(Suit.Hearts, 0, 3)),
					Forcing(1, Suit.Spades, Points(Respond1Level), Shape(5), Shape(Suit.Diamonds, 0, 5), Shape(Suit.Hearts, 0, 5)),
					Forcing(1, Suit.Spades, Points(Respond1Level), Shape(6, 11), Shape(Suit.Diamonds, 0, 6), Shape(Suit.Hearts, 0, 6)),

					NonForcing(1, Suit.Unknown, Points(Respond1NT), Balanced()),

					Invitational(2, Suit.Clubs, Points(Raise1), Shape(5), LongestMajor(3)),

					Forcing(2, Suit.Diamonds, Points(SlamInterest), Shape(5, 11)),

					Forcing(2, Suit.Hearts, Points(SlamInterest), Shape(5, 11)),

					Forcing(2, Suit.Spades, Points(SlamInterest), Shape(5, 11)),

                    // TODO: Really balanced?  This would only be the case for 4333 given current rules.  Maybe so...
                    Invitational(2, Suit.Unknown, Points(RaiseTo2NT), LongestMajor(3), Balanced()),

					Invitational(3, Suit.Clubs, Points(LimitRaise), Shape(5), LongestMajor(3)),

					Signoff(3, Suit.Unknown, Points(RaiseTo3NT), Balanced(), LongestMajor(3)),

					Signoff(4, Suit.Clubs, Points(Weak4Level), Shape(6, 11)),

                    // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                    Signoff(4, Suit.Hearts, Points(Weak4Level), Shape(7, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),

					Signoff(4, Suit.Spades, Points(Weak4Level), Shape(7, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),

				};
				// TODO: Need a next convention here.....  
			}
		}
		
		public class RespondTo1D : NaturalRespond
		{
			public RespondTo1D() : base()
			{
				this.BidRules = new BidRule[]
				{
					// TODO: Only forcing if not a passed hand...
					Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(4), LongerOrEqualTo(Suit.Spades)),
                    Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(5, 11), LongerThan(Suit.Spades)),

                    Forcing(1, Suit.Spades, Points(Respond1Level), Shape(4), Shape(Suit.Hearts, 0, 3)),
                    Forcing(1, Suit.Spades, Points(Respond1Level), Shape(5, 11), LongerOrEqualTo(Suit.Hearts)),

                    NonForcing(1, Suit.Unknown, Points(Respond1NT), Balanced(), LongestMajor(3)),


                    Forcing(2, Suit.Clubs, Points(NewSuit2Level), Shape(5, 11), LongestMajor(3)),

                    Forcing(2, Suit.Diamonds, Points(Raise1), Shape(5, 11), LongestMajor(3)),

                    Forcing(2, Suit.Hearts, Points(SlamInterest), Shape(5, 11)),

                    Forcing(2, Suit.Spades, Points(SlamInterest), Shape(5, 11)),

                    // TODO: Really balanced?  This would only be the case for 4333 given current rules.  Maybe so...
                    Invitational(2, Suit.Unknown, Points(RaiseTo2NT), LongestMajor(3), Balanced()),

                    Invitational(3, Suit.Diamonds, Points(LimitRaise), Shape(5, 11), LongestMajor(3)),

                    Signoff(3, Suit.Unknown, Points(RaiseTo3NT), LongestMajor(3)),

                    Signoff(4, Suit.Diamonds, 1, Points(Weak4Level), Shape(6, 11)),

                    // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                    Signoff(4, Suit.Hearts, 1, Points(Weak4Level), Shape(7, 11)),

                    Signoff(4, Suit.Spades, 1, Points(Weak4Level), Shape(7, 11)),

                };
			}
		}
		
		public class RespondTo1H : NaturalRespond
		{
			public RespondTo1H() : base()
			{
				var bids = new List<BidRule>()
				{
					// TODO: Only if planning to raise if we ha

					Forcing(1, Suit.Spades, Points(Respond1Level), Shape(4, 11), Shape(Suit.Hearts, 0, 2)),
					Forcing(1, Suit.Spades, DummyPoints(Suit.Hearts, LimitRaise), Shape(4, 11), Shape(Suit.Hearts, 3)),
					Forcing(1, Suit.Spades, DummyPoints(Suit.Hearts, GameOrBetter), Shape(4, 11), Shape(Suit.Hearts, 3, 8)),

					NonForcing(1, Suit.Unknown, Points(Respond1NT), Balanced()),

                    // Two level minor bids are handled by NewMinorSuit2Level...

                    Invitational(2, Suit.Hearts, DummyPoints(Raise1), Shape(3, 8)),

					Forcing(2, Suit.Spades, Points(SlamInterest), Shape(5, 11)),

					Invitational(2, Suit.Unknown, Points(RaiseTo2NT), Balanced()),

					Invitational(3, Suit.Hearts, DummyPoints(LimitRaise), Shape(4, 8)),

					Signoff(3, Suit.Unknown, Points(RaiseTo3NT), LongestMajor(3)),


                    // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                    Signoff(4, Suit.Hearts, DummyPoints(Suit.Hearts, Weak4Level), Shape(5, 8)),

					Signoff(4, Suit.Spades, Points(Weak4Level), Shape(7, 11)),
				};
				bids.Concat(NewMinorSuit2Level(Suit.Hearts));
				this.BidRules = bids;

            }
		}

		public class RespondTo1S : NaturalRespond
		{
			public RespondTo1S() : base() 
			{
				var bids = new List<BidRule>()
				{
                    NonForcing(1, Suit.Unknown, Points(Respond1NT), Balanced()),

                    // Two level minor bids are handled by NewMinorSuit2Level...
                    // THIS IS HIGHER PRIORITY THAN SHOWING MINORS NO MATTER WHAT THE LENGTH...
					Forcing(2, Suit.Hearts, Points(NewSuit2Level), Shape(5, 11)),

                    Invitational(2, Suit.Spades, DummyPoints(Raise1), Shape(3, 8)),

                    Invitational(2, Suit.Unknown, Points(RaiseTo2NT), Balanced()),

                    Invitational(3, Suit.Spades, DummyPoints(LimitRaise), Shape(4, 8)),

                    Signoff(3, Suit.Unknown, Points(RaiseTo3NT), LongestMajor(3)),

                    // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                    Signoff(4, Suit.Hearts, Points(Weak4Level), Shape(7, 11)),

                    Signoff(4, Suit.Spades, DummyPoints(Weak4Level), Shape(5, 8))
                };
				bids.Concat(NewMinorSuit2Level(Suit.Spades));
				this.BidRules = bids;
				// TODO: Next state here...
			}

		}

	}
}

