using System;
using System.Collections.Generic;


namespace BridgeBidding
{
    public class Respond : StandardAmerican
    {

        static protected (int, int) RespondPass = (0, 5);
        static protected (int, int) Respond1Level = (6, 40);
        static protected (int, int) Raise1 = (6, 10);
        static protected (int, int) Respond1NT = (6, 10);
        static protected (int, int) NewSuit2Level = (11, 40);  
        static protected (int, int) RaiseTo2NT = (11, 12);
        static protected (int, int) SlamInterest = (17, 40);
        static protected (int, int) LimitRaise = (11, 12);
        static protected (int, int) LimitRaiseOrBetter = (11, 40);
        static protected (int, int) RaiseTo3NT = (13, 16);
        static protected (int, int) Weak4Level = (0, 10);
        static protected (int, int) GameOrBetter = (13, 40);
        static protected (int, int) WeakJumpRaise = (0, 8); // TODO: Consider HCP vs DummyPoints...  For now this works.
        static protected (int, int) MinimumHand = (6, 10);
        static protected (int, int) MediumHand = (11, 13);
        static protected (int, int) ResponderRedouble = (10, 40);
        static protected (int, int) ResponderRedoubleHCP = (10, 40);

        static protected (int, int) PairGame = (26, 31);


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

        private static BidRule[] RespondNT(int level, params Suit[] denies)
        {
            var rule = Invitational(level, Strain.NoTrump);
            if (level == 1)
            {
                foreach (Suit suit in denies)
                {
                    rule.AddConstraint(Shape(suit, 0, 3));
                }
                rule.AddConstraint(Points(Respond1NT));
            }
            else
            {
                // TODO: Is this right?  I think a 5+ card suit should always be bid...
                foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                {
                    rule.AddConstraint(Shape(suit, 0, 4));
                }
                rule.AddConstraint(Points(level == 2 ? RaiseTo2NT : RaiseTo3NT));
            }
            return new BidRule[]
            {
                PartnerBids(level, Strain.NoTrump, Bid.Double, p => Open.RebidPartnerBidNT(p, level)),
                rule
            };
        }

        protected static IEnumerable<BidRule> NoTrumpResponses(PositionState ps, params Suit[] denies)
        {
            var bids = new List<BidRule>();
			bids.AddRange(RespondNT(1, denies));
			bids.AddRange(RespondNT(2, denies));
			bids.AddRange(RespondNT(3, denies));
            return bids;
        }




        // Responses to Open1C no interference
        public static IEnumerable<BidRule> Club(PositionState ps)
        {
            var bids = new List<BidRule>
            {
				DefaultPartnerBids(Bid.Double, Open.RebidPartnerChangedSuits),
				PartnerBids(2, Strain.Clubs, Bid.Double, Open.RebidPartnerRaisedMinor),
				PartnerBids(3, Strain.Clubs, Bid.Double, Open.RebidPartnerRaisedMinor),

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


                Invitational(2, Suit.Clubs, ShowsTrump(), Points(Raise1), Shape(5), LongestMajor(3)),

                // Slam interest
                Forcing(2, Suit.Spades, Points(SlamInterest), Shape(5, 11)),
				Forcing(2, Suit.Hearts, Points(SlamInterest), Shape(5, 11)),
				Forcing(2, Suit.Diamonds, Points(SlamInterest), Shape(5, 11)),

				Invitational(3, Suit.Clubs, ShowsTrump(), Points(LimitRaise), Shape(5), LongestMajor(3)),

                Signoff(4, Suit.Clubs, ShowsTrump(), Points(Weak4Level), Shape(6, 11)),

                // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                Signoff(4, Suit.Hearts, ShowsTrump(), Points(Weak4Level), Shape(7, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),

                Signoff(4, Suit.Spades, ShowsTrump(), Points(Weak4Level), Shape(7, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),

                Signoff(Bid.Pass, Points(RespondPass)),
            };
            bids.AddRange(NoTrumpResponses(ps, Suit.Diamonds, Suit.Hearts, Suit.Spades));
            return bids;
        }

        public static IEnumerable<BidRule> Diamond(PositionState ps)
        {
            var bids = new List<BidRule>
            {
				DefaultPartnerBids(Bid.Double, Open.RebidPartnerChangedSuits),
				PartnerBids(2, Strain.Diamonds, Bid.Double, Open.RebidPartnerRaisedMinor),
				PartnerBids(3, Strain.Diamonds, Bid.Double, Open.RebidPartnerRaisedMinor),

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
              //  Invitational(2, Suit.Unknown, Points(RaiseTo2NT), LongestMajor(3), Balanced()),


//				Signoff(3, Suit.Unknown, Points(RaiseTo3NT), LongestMajor(3)),

				Signoff(4, Suit.Diamonds, Points(Weak4Level), Shape(6, 11)),

                // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                Signoff(4, Suit.Hearts, Points(Weak4Level), Shape(7, 11)),

                Signoff(4, Suit.Spades, Points(Weak4Level), Shape(7, 11)),


                Signoff(Bid.Pass, Points(RespondPass)),
            };
            bids.AddRange(NoTrumpResponses(ps,Suit.Hearts, Suit.Spades));
            return bids;
        }
        public static IEnumerable<BidRule> Heart(PositionState ps)
        {
            var bids = new List<BidRule>
            {
				DefaultPartnerBids(Bid.Double, Open.RebidPartnerChangedSuits),
				PartnerBids(2, Strain.Hearts, Bid.Double, Open.RebidPartnerRaisedMajor),
				PartnerBids(3, Strain.Hearts, Bid.Double, Open.RebidPartnerRaisedMajor),
				PartnerBids(4, Strain.Hearts, Bid.Double, Open.RebidPartnerRaisedMajor),

                // TODO: Need higher priority bids showing spades when bid hand ---

				Invitational(2, Suit.Hearts, DummyPoints(Raise1), Shape(3, 8), ShowsTrump()),
                Invitational(3, Suit.Hearts,DummyPoints(LimitRaise), Shape(4, 8), ShowsTrump()),
				Signoff(4, Suit.Hearts, DummyPoints(Suit.Hearts, Weak4Level), Shape(5, 8)),

				Forcing(2, Suit.Spades, Points(SlamInterest), Shape(5, 11)),

                Forcing(1, Suit.Spades, Points(Respond1Level), Shape(4, 11), Shape(Suit.Hearts, 0, 2)),
                Forcing(1, Suit.Spades, DummyPoints(Suit.Hearts, LimitRaise), Shape(4, 11), Shape(Suit.Hearts, 3)),
                Forcing(1, Suit.Spades, DummyPoints(Suit.Hearts, GameOrBetter), Shape(4, 11), Shape(Suit.Hearts, 3, 8)),



                // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function


                Signoff(4, Suit.Spades, Points(Weak4Level), Shape(7, 11)),

                Signoff(Bid.Pass,Points(RespondPass)),

            };
            bids.AddRange(NewMinorSuit2Level(Suit.Hearts));
            bids.AddRange(NoTrumpResponses(ps, Suit.Spades));
            return bids;
        }

        public static IEnumerable<BidRule> Spade(PositionState ps)
        {
            var bids = new List<BidRule>
            {
                DefaultPartnerBids(Bid.Double, Open.RebidPartnerChangedSuits),
				PartnerBids(2, Strain.Spades, Bid.Double, Open.RebidPartnerRaisedMajor),
				PartnerBids(3, Strain.Spades, Bid.Double, Open.RebidPartnerRaisedMajor),
                PartnerBids(4, Strain.Spades, Bid.Double, Open.RebidPartnerRaisedMajor),

				// Highest priority is to show support...
                Invitational(3, Suit.Spades, DummyPoints(LimitRaise), Shape(4, 8), ShowsTrump()),
                Invitational(2, Suit.Spades, DummyPoints(Raise1), Shape(3, 8), ShowsTrump()),
				Signoff(4, Suit.Spades, DummyPoints(Weak4Level), Shape(5, 8)),

                // Two level minor bids are handled by NewMinorSuit2Level...
                // THIS IS HIGHER PRIORITY THAN SHOWING MINORS NO MATTER WHAT THE LENGTH...
				Forcing(2, Suit.Hearts, Points(NewSuit2Level), Shape(5, 11)),


                // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                Signoff(4, Suit.Hearts, Points(Weak4Level), Shape(7, 11)),


                Signoff(Bid.Pass, Points(RespondPass)),

            };
            bids.AddRange(NewMinorSuit2Level(Suit.Spades));
            bids.AddRange(NoTrumpResponses(ps));
            return bids;
        }

        public static IEnumerable<BidRule> WeakOpen(PositionState ps)
        {
            return new BidRule[]
            {
                // TODO: Artificial inquiry 2NT...
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
        public static BidChoices OppsOvercalled(PositionState ps)
        {
            var choices = new BidChoices(ps);
            // TODO:  Need to do better thann this for bid rules.
            choices.DefaultPartnerBids.AddFactory(Call.Double, (p) => { return new BidChoices(p, Compete.CompBids); });

            choices.AddRules(NegativeDouble.InitiateConvention);
            choices.AddRules(new BidRule[]
            {
                Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(4), LongerOrEqualTo(Suit.Spades)),
                Forcing(1, Suit.Hearts, Points(Respond1Level), Shape(5, 11), LongerThan(Suit.Spades)),

                Forcing(1, Suit.Spades, Points(Respond1Level), Shape(4), Shape(Suit.Hearts, 0, 3)),
                Forcing(1, Suit.Spades, Points(Respond1Level), Shape(5, 11), LongerOrEqualTo(Suit.Hearts)),

                // TODO: Perhaps show new 5+ card suit forcing here?  Only if not passed.

				// Now cuebid raises are next in priority - RaisePartner calls ShowTrump()
                Forcing(2, Suit.Diamonds, CueBid(), RaisePartner(Suit.Clubs), DummyPoints(Suit.Clubs, LimitRaiseOrBetter)),

                Forcing(2, Suit.Hearts, CueBid(), RaisePartner(Suit.Clubs), DummyPoints(Suit.Clubs, LimitRaiseOrBetter)),
                Forcing(2, Suit.Hearts, CueBid(), RaisePartner(Suit.Diamonds), DummyPoints(Suit.Diamonds, LimitRaiseOrBetter)),


                Forcing(2, Suit.Spades, CueBid(), RaisePartner(Suit.Clubs), DummyPoints(Suit.Clubs, LimitRaiseOrBetter)),
                Forcing(2, Suit.Spades, CueBid(), RaisePartner(Suit.Diamonds), DummyPoints(Suit.Diamonds, LimitRaiseOrBetter)),
                Forcing(2, Suit.Spades, CueBid(), RaisePartner(Suit.Hearts), DummyPoints(Suit.Hearts, LimitRaiseOrBetter)),


                // TODO: Weak jumps here take precedence over simple raise
               
				Nonforcing(3, Suit.Hearts, Fit(9), Jump(1), DummyPoints(WeakJumpRaise)),
                Nonforcing(3, Suit.Spades, Fit(9), Jump(1), DummyPoints(WeakJumpRaise)),


                // Now time for invitational bids.
                Invitational(2, Suit.Clubs, CueBid(false), RaisePartner(), DummyPoints(Raise1)),
                Invitational(2, Suit.Clubs, OppsStopped(false), CueBid(false), RaisePartner(fit: 7), DummyPoints(Raise1)),

                Invitational(2, Suit.Diamonds, CueBid(false), RaisePartner(), DummyPoints(Raise1)),
                Invitational(2, Suit.Diamonds, OppsStopped(false), CueBid(false), RaisePartner(fit: 7), DummyPoints(Raise1)),

                Invitational(2, Suit.Hearts, CueBid(false), RaisePartner(), DummyPoints(Raise1)),
                Invitational(2, Suit.Spades, CueBid(false), RaisePartner(), DummyPoints(Raise1)),

				// TODO: Still need lots and lots more bid levels here.  But decent start...
		
				// TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

				Signoff(4, Suit.Hearts, RaisePartner(raise: 3, fit: 10), DummyPoints(Weak4Level)),
                Signoff(4, Suit.Spades, RaisePartner(raise: 3, fit: 10), DummyPoints(Weak4Level)),

                Signoff(Bid.Pass, Points(RespondPass)),

            });
            // TODO: Need to have opponents stopped?  Maybe those bids go higher up ...
            choices.AddRules(NoTrumpResponses(ps));

            return choices;
        }

        static protected (int, int) RespondRedouble = (10, 40);
        static protected (int, int) RespondX1Level = (6, 9);
        static protected (int, int) RespondXJump = (0, 6);
        

        public static IEnumerable<BidRule> OppsDoubled(PositionState ps)
        {
            var bids = new List<BidRule>
            {
                Forcing(Call.Redouble, Points(RespondRedouble)),
				// TODO: Here we need to make all bids reflect that they are less than 10 points...

				Nonforcing(1, Suit.Hearts, Points(RespondX1Level), Shape(4), LongerOrEqualTo(Suit.Spades)),
                Nonforcing(1, Suit.Hearts, Points(RespondX1Level), Shape(5, 11), LongerThan(Suit.Spades)),

                Nonforcing(1, Suit.Spades, Points(RespondX1Level), Shape(4), Shape(Suit.Hearts, 0, 3)),
                Nonforcing(1, Suit.Spades, Points(RespondX1Level), Shape(5, 11), LongerOrEqualTo(Suit.Hearts)),

                Nonforcing(1, Suit.Diamonds, Shape(4, 11), Points(RespondX1Level)),

                //
                // If we have a good fie but a week hand then time to jump.
                //
                Nonforcing(3, Suit.Clubs,    Partner(HasShownSuit()), Fit(9), ShowsTrump(), Points(RespondXJump)),
                Nonforcing(3, Suit.Diamonds, Partner(HasShownSuit()), Fit(9), ShowsTrump(), Points(RespondXJump)),
                Nonforcing(3, Suit.Hearts,   Partner(HasShownSuit()), Fit(9), ShowsTrump(), Points(RespondXJump)),
                Nonforcing(3, Suit.Spades,   Partner(HasShownSuit()), Fit(9), ShowsTrump(), Points(RespondXJump)),

                Nonforcing(2, Suit.Clubs, Partner(HasShownSuit()), Fit(), Points(RespondX1Level)),
                Nonforcing(2, Suit.Clubs, Shape(5, 11), Points(RespondX1Level)),

                Nonforcing(2, Suit.Diamonds, Partner(HasShownSuit()), Fit(), Points(RespondX1Level)),
                Nonforcing(2, Suit.Diamonds, Jump(0), Shape(5, 11), Points(RespondX1Level)),

                Nonforcing(2, Suit.Hearts, Partner(HasShownSuit()), Fit(), Points(RespondX1Level)),
                Nonforcing(2, Suit.Hearts, Jump(0), Shape(5, 11), Points(RespondX1Level)),

                Nonforcing(2, Suit.Spades, Partner(HasShownSuit()), Fit(), Points(RespondX1Level)),

				// TODO: Perhaps higer priority than raise of a minor???
                Nonforcing(1, Strain.NoTrump, Points(RespondX1Level)),

                Signoff(Bid.Pass, Points(RespondPass))

            };

            return bids;
        }

        public static IEnumerable<BidRule> Rebid(PositionState ps)
        {
            var bids = new List<BidRule>
            {

                Nonforcing(2, Suit.Clubs, Shape(6, 11), Points(MinimumHand)),
                Nonforcing(2, Suit.Diamonds, Shape(6, 11), Points(MinimumHand)),
                Nonforcing(2, Suit.Hearts, Shape(6, 11), Points(MinimumHand)),
                Nonforcing(2, Suit.Spades, Shape(6, 11), Points(MinimumHand)),


				// TODO: Make these dependent on pair points.
                Invitational(3, Suit.Clubs, Shape(6, 11), Points(MediumHand)),
                Invitational(3, Suit.Diamonds, Shape(6, 11), Points(MediumHand)),
                Invitational(3, Suit.Hearts, Shape(6, 11), Points(MediumHand)),
                Invitational(3, Suit.Spades, Shape(6, 11), Points(MediumHand))

            };
            bids.AddRange(Compete.CompBids(ps));
            return bids;
        }

        public static IEnumerable<BidRule> OpenerInvitedGame(PositionState ps)
        {
            var bids = new List<BidRule>()
            {
                Signoff(4, Suit.Hearts, Fit(), PairPoints(PairGame)),
                Signoff(4, Suit.Spades, Fit(), PairPoints(PairGame))
            };
            // TODO: Competative bids here too?  Seems silly since restricted raise
            return bids;
        }
    }
}
