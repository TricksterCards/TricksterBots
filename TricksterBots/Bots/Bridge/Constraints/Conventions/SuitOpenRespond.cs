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
	public class StandardAmericanOpenRespond : StandardAmerican
	{
		//	public static new PrescribedBids DefaultBidderXXX()
		//	{
		//			var bidder = new StandardAmericanOpenRespond();
		//		return new PrescribedBids(bidder, bidder.Open);
		//	}

		public static IEnumerable<BidRule> OpenSuit(PositionState _)
		{
			return new List<BidRule>
			{

				PartnerBids(1, Suit.Clubs, Bid.Pass, RespondTo1C),
                PartnerBids(1, Suit.Diamonds, Bid.Pass, RespondTo1D),
                PartnerBids(1, Suit.Hearts, Bid.Pass, RespondTo1H),
                PartnerBids(1, Suit.Spades, Bid.Pass, RespondTo1S),

                Nonforcing(1, Suit.Clubs, Points(Open1Suit), Shape(3), Shape(Suit.Diamonds, 0, 3), LongestMajor(4)),
				Nonforcing(1, Suit.Clubs, Points(Open1Suit), Shape(4, 11), LongerThan(Suit.Diamonds), LongestMajor(4)),

				Nonforcing(1, Suit.Diamonds, Points(Open1Suit), Shape(3), Shape(Suit.Clubs, 0, 2), LongestMajor(4)),
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
				Nonforcing(3, Suit.Clubs, Points(LessThanOpen), Shape(8)),

				Nonforcing(3, Suit.Diamonds, Points(LessThanOpen), Shape(7), GoodSuit()),
				Nonforcing(3, Suit.Hearts, Points(LessThanOpen), Shape(7), GoodSuit()),
				Nonforcing(3, Suit.Spades, Points(LessThanOpen), Shape(7), GoodSuit()),

				// 3NT rule(s) in NoTrump class.
				
                Nonforcing(4, Suit.Clubs, Points(LessThanOpen), Shape(8), DecentSuit()),
				Nonforcing(4, Suit.Diamonds, Points(LessThanOpen), Shape(8), DecentSuit()),
				Nonforcing(4, Suit.Hearts, Points(LessThanOpen), Shape(8), DecentSuit()),
				Nonforcing(4, Suit.Spades, Points(LessThanOpen), Shape(8), DecentSuit()),

				// TODO: Is this best way?  Need to prevent pass from invoking response. Go back to default...
				PartnerBids(Bid.Pass, new Bid(7, Suit.Unknown), (BidChoicesFactory)null),
				Nonforcing(Bid.Pass, Points(LessThanOpen)),

			};
		}


		private static IEnumerable<BidRule> OpenerRebid(PositionState _)
		{
			return new List<BidRule>()
			{
				DefaultPartnerBids(Bid.Double, ResponderRebid),

				// TODO: These seem silly.  Especially diamonds...
				Nonforcing(1, Suit.Diamonds, Shape(4, 11)),
				Nonforcing(1, Suit.Hearts, Shape(4, 11)),
				Nonforcing(1, Suit.Spades, Shape(4, 11)),



				// Responder changed suits and we have a fit.  Support at appropriate level.
                Nonforcing(2, Suit.Clubs,  Fit(), ShowsTrump(), Points(MinimumOpener)),
				Nonforcing(2, Suit.Diamonds, Fit(), ShowsTrump(), Points(MinimumOpener)),
				Nonforcing(2, Suit.Hearts, Fit(), ShowsTrump(), Points(MinimumOpener)),
				Nonforcing(2, Suit.Spades, Fit(), ShowsTrump(), Points(MinimumOpener)),


				Nonforcing(3, Suit.Clubs, Fit(), ShowsTrump(), Points(MediumOpener)),
				Nonforcing(3, Suit.Diamonds, Fit(), ShowsTrump(), Points(MediumOpener)),
				Nonforcing(3, Suit.Hearts, Fit(), ShowsTrump(), Points(MediumOpener)),
				Nonforcing(3, Suit.Spades, Fit(), ShowsTrump(), Points(MediumOpener)),

				// TODO: What about minors.  This is bad. Think we want to fall through to 3NT...
                //Nonforcing(4, Suit.Clubs, DefaultPriority + 10, Fit(), ShowsTrump(), Points(MediumOpener)),
                //Nonforcing(4, Suit.Diamonds, DefaultPriority + 10, Fit(), ShowsTrump(), Points(MediumOpener)),
                Nonforcing(4, Suit.Hearts, Fit(), ShowsTrump(), Points(MaximumOpener)),
				Nonforcing(4, Suit.Spades, Fit(), ShowsTrump(), Points(MaximumOpener)),


				// Show a new suit at an appropriate level...
	//			Nonforcing(2, Suit.Clubs, Balanced(false), Points(MinimumOpener), LongestUnbidSuit()),
    //            Nonforcing(2, Suit.Clubs, Balanced(false), Points(MinimumOpener), LongestUnbidSuit()),
                Nonforcing(2, Suit.Hearts, LastBid(1, Suit.Hearts, false), Balanced(false), Points(MinimumOpener), Shape(4, 6)),
        


				// Rebid a 6 card suit
				Nonforcing(2, Suit.Clubs, LastBid(1), Shape(6, 11), Points(MinimumOpener)),
				Nonforcing(2, Suit.Diamonds, LastBid(1), Shape(6, 11), Points(MinimumOpener)),
				Nonforcing(2, Suit.Hearts, LastBid(1), Shape(6, 11), Points(MinimumOpener)),
				Nonforcing(2, Suit.Spades, LastBid(1), Shape(6, 11), Points(MinimumOpener)),




				Nonforcing(3, Suit.Clubs, LastBid(1), Shape(6, 11), Points(MediumOpener)),
				Nonforcing(3, Suit.Diamonds, LastBid(1), Shape(6, 11), Points(MediumOpener)),
				Nonforcing(3, Suit.Hearts, LastBid(1), Shape(6, 11), Points(MediumOpener)),
				Nonforcing(3, Suit.Spades, LastBid(1), Shape(6, 11), Points(MediumOpener)),



				// Lowest priority if nothing else fits is bid NT
				Nonforcing(1, Suit.Unknown, Balanced(), Points(OpenerRebid1NT)),
				Nonforcing(2, Suit.Unknown, Balanced(), Points(OpenerRebid2NT)),
				// TODO: What about 3NT...

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
		static protected (int, int) ResponderMinimumHand = (6, 10);
		static protected (int, int) ResponderMediumHand = (11, 13);
		static protected (int, int) ResponderRedouble = (10, 40);
		static protected (int, int) ResponderRedoubleHCP = (10, 40);

		protected static BidRule[] NewMinorSuit2Level(Suit openersSuit)
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

		protected static BidRule[] NoTrumpResponses()
		{
			return new BidRule[]
			{
				Nonforcing(1, Suit.Unknown, Points(Respond1NT)),
				Nonforcing(2, Suit.Unknown, Points(RaiseTo2NT), Balanced()),
				Nonforcing(3, Suit.Unknown, Points(RaiseTo3NT), Balanced())
			};
        }

		/*
		private PrescribedBids InitialResponse()
		{
			// We may be invoked because opener Passed.  If that's the case, bail now.
			var pb = new PrescribedBids();
	
			pb.Redirects = new List<RedirectRule>
			{
				Redirect(RespondTo1C, Partner(LastBid(1, Suit.Clubs)), RHO(Passed())),
				Redirect(RespondTo1D, Partner(LastBid(1, Suit.Diamonds)), RHO(Passed())),
				Redirect(RespondTo1H, Partner(LastBid(1, Suit.Hearts)), RHO(Passed())),
				Redirect(RespondTo1S, Partner(LastBid(1, Suit.Spades)), RHO(Passed())),

				Redirect(RespondToWeakOpen, Partner(BidAtLevel(2, 3, 4))),

				// TODO: First attempt at any interference.  For now only if interfere with 1S bid
				Redirect(RespondWithInt, RHO(DidBid()))
			};
			return pb;
		}
		*/

		/*
		private static void AddInterferenceRules(PrescribedBids pb)
		{
			pb.RedirectIfRhoBid(RespondWithInt);
			pb.BidRules.Add(PartnerBids(Call.Redouble, ResponseAfterRedouble));
			pb.BidRules.Add(Forcing(Bid.Redouble, Points(ResponderRedouble), HighCardPoints(ResponderRedoubleHCP)));
		}
		*/



		private static IEnumerable<BidRule> RespondTo1C(PositionState _)
		{
			var bids = new List<BidRule>
			{
				DefaultPartnerBids(Bid.Double, OpenerRebid),
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


				Invitational(2, Suit.Clubs, Points(Raise1), Shape(5), LongestMajor(3)),

				Forcing(2, Suit.Diamonds, Points(SlamInterest), Shape(5, 11)),

				Forcing(2, Suit.Hearts, Points(SlamInterest), Shape(5, 11)),

				Forcing(2, Suit.Spades, Points(SlamInterest), Shape(5, 11)),


				Invitational(3, Suit.Clubs, Points(LimitRaise), Shape(5), LongestMajor(3)),

				Signoff(4, Suit.Clubs, Points(Weak4Level), Shape(6, 11)),

                // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                Signoff(4, Suit.Hearts, Points(Weak4Level), Shape(7, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),

				Signoff(4, Suit.Spades, Points(Weak4Level), Shape(7, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),

                Signoff(Bid.Pass, Points(RespondPass)),
            };
			bids.AddRange(NoTrumpResponses());
			return bids;
		}

		private static IEnumerable<BidRule> RespondTo1D(PositionState ps)
		{
			var bids = new List<BidRule>
			{
                DefaultPartnerBids(Bid.Double, OpenerRebid),

				// TODO: More formal redouble???
				Forcing(Bid.Redouble, Points((10, 100)), HighCardPoints((10, 100))),

                Invitational(3, Suit.Diamonds, DummyPoints(LimitRaise), Shape(5, 11), LongestMajor(3)),
                Invitational(2, Suit.Diamonds, Points(Raise1), Shape(5, 11), LongestMajor(2)),

				// TODO: Only forcing if not a passed hand...
				Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(4), LongerOrEqualTo(Suit.Spades)),
				Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(5, 11), LongerThan(Suit.Spades)),

				Forcing(1, Suit.Spades, Points(Respond1Level), Shape(4), Shape(Suit.Hearts, 0, 3)),
				Forcing(1, Suit.Spades, Points(Respond1Level), Shape(5, 11), LongerOrEqualTo(Suit.Hearts)),

//				Nonforcing(1, Suit.Unknown, Points(Respond1NT), Balanced(), LongestMajor(3)),


				Forcing(2, Suit.Clubs, Points(NewSuit2Level), Shape(5, 11), LongestMajor(3)),

				Forcing(2, Suit.Hearts, Points(SlamInterest), Shape(5, 11)),

				Forcing(2, Suit.Spades, Points(SlamInterest), Shape(5, 11)),

                // TODO: Really balanced?  This would only be the case for 4333 given current rules.  Maybe so...
                Invitational(2, Suit.Unknown, Points(RaiseTo2NT), LongestMajor(3), Balanced()),


				Signoff(3, Suit.Unknown, Points(RaiseTo3NT), LongestMajor(3)),

				Signoff(4, Suit.Diamonds, Points(Weak4Level), Shape(6, 11)),

                // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                Signoff(4, Suit.Hearts, Points(Weak4Level), Shape(7, 11)),

				Signoff(4, Suit.Spades, Points(Weak4Level), Shape(7, 11)),


                Signoff(Bid.Pass, Points(RespondPass)),
            };
			bids.AddRange(NoTrumpResponses());
			return bids;
		}
		private static IEnumerable<BidRule> RespondTo1H(PositionState ps)
		{
			var bids = new List<BidRule>
			{
                DefaultPartnerBids(Bid.Double, OpenerRebid),

                Invitational(2, Suit.Hearts, DummyPoints(Raise1), Shape(3, 8), ShowsTrump()),

                Invitational(3, Suit.Hearts,DummyPoints(LimitRaise), Shape(4, 8), ShowsTrump()),

                Forcing(1, Suit.Spades, Points(Respond1Level), Shape(4, 11), Shape(Suit.Hearts, 0, 2)),
				Forcing(1, Suit.Spades, DummyPoints(Suit.Hearts, LimitRaise), Shape(4, 11), Shape(Suit.Hearts, 3)),
				Forcing(1, Suit.Spades, DummyPoints(Suit.Hearts, GameOrBetter), Shape(4, 11), Shape(Suit.Hearts, 3, 8)),

				Nonforcing(1, Suit.Unknown, Points(Respond1NT), Balanced()),

                // Two level minor bids are handled by NewMinorSuit2Level...

				Forcing(2, Suit.Spades, Points(SlamInterest), Shape(5, 11)),

				Invitational(2, Suit.Unknown, Points(RaiseTo2NT), Balanced()),

				Signoff(3, Suit.Unknown, Points(RaiseTo3NT), LongestMajor(3)),


                // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                Signoff(4, Suit.Hearts, DummyPoints(Suit.Hearts, Weak4Level), Shape(5, 8)),

				Signoff(4, Suit.Spades, Points(Weak4Level), Shape(7, 11)),

                Signoff(Bid.Pass,Points(RespondPass)),

            };
			bids.AddRange(NewMinorSuit2Level(Suit.Hearts));
			bids.AddRange(NoTrumpResponses());
			return bids;
		}

		private static IEnumerable<BidRule> RespondTo1S(PositionState ps)
		{
			var bids = new List<BidRule>
			{
                DefaultPartnerBids(Bid.Double, OpenerRebid),

				// Highest priority is to show support...
                Invitational(3, Suit.Spades, DummyPoints(LimitRaise), Shape(4, 8), ShowsTrump()),
                Invitational(2, Suit.Spades, DummyPoints(Raise1), Shape(3, 8), ShowsTrump()),

				// TODO: Should Respond 1NT be lower priority or should raises be higher?
				Nonforcing(1, Suit.Unknown, Points(Respond1NT), Balanced()),

                // Two level minor bids are handled by NewMinorSuit2Level...
                // THIS IS HIGHER PRIORITY THAN SHOWING MINORS NO MATTER WHAT THE LENGTH...
				Forcing(2, Suit.Hearts, Points(NewSuit2Level), Shape(5, 11)),

				Invitational(2, Suit.Unknown, Points(RaiseTo2NT), Balanced()),


				Signoff(3, Suit.Unknown, Points(RaiseTo3NT), LongestMajor(3)),

                // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                Signoff(4, Suit.Hearts, Points(Weak4Level), Shape(7, 11)),

				Signoff(4, Suit.Spades, DummyPoints(Weak4Level), Shape(5, 8)),

				Signoff(Bid.Pass, Points(RespondPass)),

            };
			bids.AddRange(NewMinorSuit2Level(Suit.Spades));
			bids.AddRange(NoTrumpResponses());
			return bids;
		}

		private static BidRule[] RespondToWeakOpen()
		{
			return new BidRule[]
			{
				Signoff(4, Suit.Hearts, Fit(), RuleOf17()),
				Signoff(4, Suit.Hearts, Fit(10), PassEndsAuction(false)),
				Signoff(4, Suit.Spades, Fit(), RuleOf17()),
				Signoff(4, Suit.Spades, Fit(10), PassEndsAuction(false)),
				// TODO: Pass???

				// TODO: NT Bids
				// TODO: Minor bids???
			};
		}


		// TODO: THIS IS SUPER HACKED NOW TO JUST 
		private static IEnumerable<BidRule> RespondWithInt(PositionState ps)
		{
			var bids = new List<BidRule>
			{
		
				Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(4), LongerOrEqualTo(Suit.Spades)),
				Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(5, 11), LongerThan(Suit.Spades)),

				Forcing(1, Suit.Spades, Points(Respond1Level), Shape(4), Shape(Suit.Hearts, 0, 3)),
				Forcing(1, Suit.Spades, Points(Respond1Level), Shape(5, 11), LongerOrEqualTo(Suit.Hearts)),

				// TODO: Opponents stopped!  Maybe two rules, one at lower priority that bid this in the worst case...
				// Perhaps pass could be higher than that rule if we dont have 11 points, and dont have opps stopped
		
				Invitational(2, Suit.Hearts, CueBid(false), Fit(), DummyPoints(Raise1), ShowsTrump()),
				Forcing(2, Suit.Hearts, CueBid(true), Fit(Suit.Clubs), DummyPoints(Suit.Clubs, LimitRaiseOrBetter), ShowsTrump()),
				Forcing(2, Suit.Hearts, CueBid(true), Fit(Suit.Diamonds), DummyPoints(Suit.Diamonds, LimitRaiseOrBetter), ShowsTrump()),


				Invitational(2, Suit.Spades, CueBid(false), Fit(), DummyPoints(Raise1), ShowsTrump()),
				Forcing(2, Suit.Spades, CueBid(true), Fit(Suit.Clubs), DummyPoints(Suit.Clubs, LimitRaiseOrBetter), ShowsTrump()),
				Forcing(2, Suit.Spades, CueBid(true), Fit(Suit.Diamonds), DummyPoints(Suit.Diamonds, LimitRaiseOrBetter), ShowsTrump()),
				Forcing(2, Suit.Spades, CueBid(true), Fit(Suit.Hearts), DummyPoints(Suit.Hearts, LimitRaiseOrBetter), ShowsTrump()),

				// TODO: Still need lots and lots more bid levels here.  But decent start...

				Nonforcing(3, Suit.Hearts, Fit(), DummyPoints(WeakJumpRaise), Shape(4)),

		
				// TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

				Signoff(4, Suit.Hearts, Fit(), DummyPoints(WeakJumpRaise), Shape(5, 8)),


                Signoff(Bid.Pass, Points(RespondPass)),


            };
			// TODO: Need to have opponents stopped?  Maybe those bids go higher up ...
			bids.AddRange(NoTrumpResponses());

			return bids;
		}

		public static IEnumerable<BidRule> ResponderRebid(PositionState ps)
		{
			var bids = new List<BidRule>
			{

                Nonforcing(2, Suit.Clubs, Shape(6, 11), Points(ResponderMinimumHand)),
                Nonforcing(2, Suit.Diamonds, Shape(6, 11), Points(ResponderMinimumHand)),
                Nonforcing(2, Suit.Hearts, Shape(6, 11), Points(ResponderMinimumHand)),
                Nonforcing(2, Suit.Spades, Shape(6, 11), Points(ResponderMinimumHand)),


				// TODO: Make these dependent on pair points.
                Invitational(3, Suit.Clubs, Shape(6, 11), Points(ResponderMediumHand)),
                Invitational(3, Suit.Diamonds, Shape(6, 11), Points(ResponderMediumHand)),
                Invitational(3, Suit.Hearts, Shape(6, 11), Points(ResponderMediumHand)),
                Invitational(3, Suit.Spades, Shape(6, 11), Points(ResponderMediumHand))

			};

			bids.AddRange(Compete.CompBids(ps));
			return bids;
		}

	}
}
