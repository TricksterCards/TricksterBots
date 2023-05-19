using System;
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
	public class StandardAmericanOpenRespond : Natural
	{
		public static new PrescribedBids DefaultBidderXXX()
		{
			var bidder = new StandardAmericanOpenRespond();
			return new PrescribedBids(bidder, bidder.Open);
		}

		private void Open(PrescribedBids pb)
		{
			pb.ConventionRules = new ConventionRule[]
			{
				ConventionRule(Role(PositionRole.Opener, 1))
			};
 
			pb.Bids = new BidRule[]
			{
				Nonforcing(Call.Pass, DefaultPriority - 100, Points(LessThanOpen)),

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


			};
			pb.Partner(InitialResponse); 
        }


		private void OpenerRebid(PrescribedBids pb)
        {
			pb.Bids = new List<BidRule>()
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

		// ***** RESPONSES

		static protected (int, int) RespondPass = (0, 5);
		static protected (int, int) Respond1Level = (6, 40);
		static protected (int, int) Raise1 = (6, 10);
		static protected (int, int) Respond1NT = (6, 10);
		static protected (int, int) NewSuit2Level = (12, 40);  // TODO: null??
		static protected (int, int) RaiseTo2NT = (11, 12);
		static protected (int, int) SlamInterest = (17, 40);
		static protected (int, int) LimitRaise = (11, 12);
		static protected (int, int) LimitRaiseOrBetter = (11, 40);
		static protected (int, int) RaiseTo3NT = (13, 16);
		static protected (int, int) Weak4Level = (0, 10);
		static protected (int, int) GameOrBetter = (13, 40);
		static protected (int, int) WeakJumpRaise = (0, 5);

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



		private void InitialResponse(PrescribedBids pb)
		{
			// We may be invoked because opener Passed.  If that's the case, bail now.
			pb.ConventionRules = new ConventionRule[]
			{
				ConventionRule(Role(PositionRole.Responder, 1))
			};
			pb.Redirects = new RedirectRule[]
			{
				Redirect(RespondTo1C, Partner(LastBid(1, Suit.Clubs)), RHO(Passed())),
				Redirect(RespondTo1D, Partner(LastBid(1, Suit.Diamonds)), RHO(Passed())),
				Redirect(RespondTo1H, Partner(LastBid(1, Suit.Hearts)), RHO(Passed())),
				Redirect(RespondTo1S, Partner(LastBid(1, Suit.Spades)), RHO(Passed())),

				Redirect(RespondToWeakOpen, Partner(BidAtLevel(2, 3, 4))),

				// TODO: First attempt at any interference.  For now only if interfere with 1S bid
				Redirect(RespondWithInt, RHO(DidBid()))
			};
		}

		public void RespondTo1C(PrescribedBids pb)
		{
			pb.Bids = new List<BidRule>()
			{
				Signoff(Call.Pass, 0, Points(RespondPass)),

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

				Nonforcing(1, Suit.Unknown, Points(Respond1NT), Balanced()),

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
			pb.Partner(OpenerRebid);
		}
		private void RespondTo1D(PrescribedBids pb)
		{
			pb.Bids = new BidRule[]
			{
				Signoff(Call.Pass, 0, Points(RespondPass)),

				// TODO: Only forcing if not a passed hand...
				Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(4), LongerOrEqualTo(Suit.Spades)),
				Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(5, 11), LongerThan(Suit.Spades)),

				Forcing(1, Suit.Spades, Points(Respond1Level), Shape(4), Shape(Suit.Hearts, 0, 3)),
				Forcing(1, Suit.Spades, Points(Respond1Level), Shape(5, 11), LongerOrEqualTo(Suit.Hearts)),

				Nonforcing(1, Suit.Unknown, Points(Respond1NT), Balanced(), LongestMajor(3)),


				Forcing(2, Suit.Clubs, Points(NewSuit2Level), Shape(5, 11), LongestMajor(3)),

				Invitational(2, Suit.Diamonds, DefaultPriority + 1, Points(Raise1), Shape(5, 11), LongestMajor(3)),

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
			pb.Partner(OpenerRebid);
		}
		private void RespondTo1H(PrescribedBids pb)
		{
			var bids = new List<BidRule>()
			{
				Signoff(Call.Pass, 0, Points(RespondPass)),

				Forcing(1, Suit.Spades, Points(Respond1Level), Shape(4, 11), Shape(Suit.Hearts, 0, 2)),
				Forcing(1, Suit.Spades, DummyPoints(Suit.Hearts, LimitRaise), Shape(4, 11), Shape(Suit.Hearts, 3)),
				Forcing(1, Suit.Spades, DummyPoints(Suit.Hearts, GameOrBetter), Shape(4, 11), Shape(Suit.Hearts, 3, 8)),

				Nonforcing(1, Suit.Unknown, Points(Respond1NT), Balanced()),

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

			pb.Bids = bids.Concat(NewMinorSuit2Level(Suit.Hearts));
			pb.Partner(OpenerRebid);
		}

		private void RespondTo1S(PrescribedBids pb)
		{
			var bids = new List<BidRule>()
			{
				Signoff(Call.Pass, Points(RespondPass)),

				Nonforcing(1, Suit.Unknown, Points(Respond1NT), Balanced()),

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
			pb.Bids = bids.Concat(NewMinorSuit2Level(Suit.Spades));
			pb.Partner(OpenerRebid);
		}

		private void RespondToWeakOpen(PrescribedBids pb)
		{
			pb.Bids = new BidRule[]
			{
				Signoff(4, Suit.Hearts, Fit(), RuleOf17()),
				Signoff(4, Suit.Hearts, Fit(10), PassEndsAuction(false)),
				Signoff(4, Suit.Spades, Fit(), RuleOf17()),
				Signoff(4, Suit.Spades, Fit(10), PassEndsAuction(false)),
			};
		}


		// TODO: THIS IS SUPER HACKED NOW TO JUST 
		private void RespondWithInt(PrescribedBids pb)
		{

			pb.Bids = new List<BidRule>()
			{
				Signoff(Call.Pass, 0, Points(RespondPass)),

				Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(4), LongerOrEqualTo(Suit.Spades)),
				Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(5, 11), LongerThan(Suit.Spades)),

				Forcing(1, Suit.Spades, Points(Respond1Level), Shape(4), Shape(Suit.Hearts, 0, 3)),
				Forcing(1, Suit.Spades, Points(Respond1Level), Shape(5, 11), LongerOrEqualTo(Suit.Hearts)),

				// TODO: Opponents stopped!  Maybe two rules, one at lower priority that bid this in the worst case...
				// Perhaps pass could be higher than that rule if we dont have 11 points, and dont have opps stopped
			
                Nonforcing(1, Suit.Unknown, DefaultPriority - 10, Points(Respond1NT), Balanced(), LongestMajor(3)),

				Invitational(2, Suit.Hearts, CueBid(false), Fit(), DummyPoints(Raise1), ShowsTrump()),
				Forcing(2, Suit.Hearts, CueBid(true), Fit(Suit.Clubs), DummyPoints(Suit.Clubs, LimitRaiseOrBetter), ShowsTrump()),
				Forcing(2, Suit.Hearts, CueBid(true), Fit(Suit.Diamonds), DummyPoints(Suit.Diamonds, LimitRaiseOrBetter), ShowsTrump()),


				Invitational(2, Suit.Spades, CueBid(false), Fit(), DummyPoints(Raise1), ShowsTrump()),
				Forcing(2, Suit.Spades, CueBid(true), Fit(Suit.Clubs), DummyPoints(Suit.Clubs, LimitRaiseOrBetter), ShowsTrump()),
				Forcing(2, Suit.Spades, CueBid(true), Fit(Suit.Diamonds), DummyPoints(Suit.Diamonds, LimitRaiseOrBetter), ShowsTrump()),
				Forcing(2, Suit.Spades, CueBid(true), Fit(Suit.Hearts), DummyPoints(Suit.Hearts, LimitRaiseOrBetter), ShowsTrump()),

				// TODO: Still need lots and lots more bid levels here.  But decent start...

				// TODO: Also needs opps stopped
				Invitational(2, Suit.Unknown, DefaultPriority - 10, Points(RaiseTo2NT), Balanced()),

				Nonforcing(3, Suit.Hearts, Fit(), DummyPoints(WeakJumpRaise), Shape(4)),

				// TODO: What is not balanced but do have opponents stopped.  Maybe remove balanced.....
				Signoff(3, Suit.Unknown, Points(RaiseTo3NT), OppsStopped(), Balanced()),

				// TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

				Signoff(4, Suit.Hearts, Fit(), DummyPoints(WeakJumpRaise), Shape(5, 8)),
			};
			// TODO: NEED TO RESPOND WITH INTERFERENCE..

			//this.NextConventionState = () => new NaturalOpenerRebid();
		}



	}
}
