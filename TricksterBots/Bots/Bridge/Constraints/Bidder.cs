using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;
using static TricksterBots.Bots.Bridge.BidRule;

namespace TricksterBots.Bots.Bridge
{



	public abstract class Bidder
	{




		// Convention rules..
		//	public static ConventionRule ConventionRule(params Constraint[] constraints)
		//	{
		//		return new ConventionRule(constraints);
		//	}


		// TODO: ANYTHING THAT USED TO REFER TO THIS NEEDS TO USE A FACTORY...
		//	public static RedirectRule Redirect(PrescribeBidRules redirectTo)
		//	{
		//		return Redirect(redirectTo, new Constraint[0]);
		//	}

		//	public static RedirectRule Redirect(PrescribeBidRules redirectTo, params Constraint[] constraints)
		//	{
		//		return new RedirectRule(this, redirectTo, constraints);
		//	}

		// TODO: Is there ever a place for constraints on this???  Dont know...
		public static BidRule DefaultPartnerBids(Call goodThrough, BidRulesFactory brf)
		{
			return DefaultPartnerBids(goodThrough, (ps) => { return new BidChoices(ps, brf); });
		}

        public static BidRule DefaultPartnerBids(Call goodThrough, BidChoicesFactory bcf)
        {
            return _PartnerBids(null, goodThrough, bcf, new Constraint[0]);
        }


        public static BidRule PartnerBids(int level, Suit suit, Call goodThrough, BidRulesFactory partnerBidsFactory)
		{
			return PartnerBids(level, suit, goodThrough, partnerBidsFactory, new Constraint[0]);
		}

		public static BidRule PartnerBids(int level, Suit suit, Call goodThrough, BidRulesFactory brf, params Constraint[] constraints)
		{
			return _PartnerBids(new Bid(level, suit), goodThrough, (ps) => { return new BidChoices(ps, brf); }, constraints);
		}

		public static BidRule PartnerBids(int level, Suit suit, Call goodThrough, BidChoicesFactory choicesFactory)
		{
			return _PartnerBids(new Bid(level, suit), goodThrough, choicesFactory, new Constraint[0]);
		}

		public static BidRule PartnerBids(int level, Suit suit, Bid goodThrough, BidChoicesFactory choicesFactory, params Constraint[] constraints)
		{
			return _PartnerBids(new Bid(level, suit), goodThrough, choicesFactory, constraints);
		}

		public static BidRule PartnerBids(Call call, Call goodThrough, BidRulesFactory brf)
		{
			return _PartnerBids(call, goodThrough, (ps) => { return new BidChoices(ps, brf); }, new Constraint[0]);
		}

		private static BidRule _PartnerBids(Call call, Call goodThrough, BidChoicesFactory choicesFactory, params Constraint[] constraints)
		{
			return new PartnerBidRule(call, goodThrough, choicesFactory, constraints);
		}

		// TODO: THis is start of something for conventions... Flush it out...
		public static Call GoodThrough(PositionState ps, string systemKey)
		{
			return Call.Pass;
		}

		/*
		public static BidRule PartnerBids(Call call, PrescribedBidsFactory partnerBidsFactory)
		{
			return PartnerBids(call, partnerBidsFactory, new Constraint[0]);
		}

		public static BidRule PartnerBids(Call call, PrescribedBidsFactory partnerBidsFactory, params Constraint[] constraints)
		{
			return new PartnerBidRule(new Bid(call), partnerBidsFactory, constraints);
		}
		*/

		public static BidRule Forcing(int level, Suit suit, params Constraint[] constraints)
		{
			return Rule(level, suit, BidForce.Forcing, constraints);
		}


		// TODO: Add other flavors of this, but for now this works.
		public static BidRule Forcing(Call call, params Constraint[] constraints)
		{
			return Rule(call, BidForce.Forcing, constraints);
		}


		// TODO: Need a non-forcing BidMessage...
		public static BidRule Nonforcing(int level, Suit suit, params Constraint[] constraints)
		{
			return Rule(level, suit, BidForce.Nonforcing, constraints);
		}
		public static BidRule Nonforcing(Call call, params Constraint[] constraints)
		{
			return Rule(call, BidForce.Nonforcing, constraints);
		}



		public static BidRule Invitational(int level, Suit suit, params Constraint[] constraints)
		{
			return Rule(level, suit, BidForce.Invitational, constraints);
		}
		public static BidRule Invitational(Call call, params Constraint[] constraints)
		{
			return Rule(call, BidForce.Invitational, constraints);
		}
	

		public static BidRule Signoff(int level, Suit suit, params Constraint[] constraints)
		{
			return Rule(level, suit, BidForce.Signoff, constraints);
		}
		public static BidRule Signoff(Call call, params Constraint[] constraints)
		{
			return Rule(call, BidForce.Signoff, constraints);
		}


		public static BidRule Rule(int level, Suit suit, BidForce force, params Constraint[] constraints)
		{
			return Rule(new Bid(level, suit), force, constraints);
		}



//		public static BidRule Rule(Call call, BidForce force, params Constraint[] constraints)
	//	{
//			return Rule(new Bid(call), force, constraints);
//		}

		public static BidRule Rule(Call call, BidForce force, params Constraint[] constraints)
		{
			return new BidRule(call, force, constraints);
		}

		public static Constraint HighCardPoints(int min, int max)
		{ return new ShowsPoints(null, min, max, HasPoints.PointType.HighCard); }
		public static Constraint HighCardPoints((int min, int max) range)
		{
			return HighCardPoints(range.min, range.max);
		}

		public static Constraint Points(int min, int max)
		{
			return new ShowsPoints(null, min, max, HasPoints.PointType.Starting);
		}

		public static Constraint Points((int min, int max) range) {
			return Points(range.min, range.max); }

		public static Constraint DummyPoints(int min, int max)
		{
			// TODO: Rename this??? SuitPoints???  
			return new ShowsPoints(null, min, max, HasPoints.PointType.Suit);
		}
		public static Constraint DummyPoints((int min, int max) range)
		{
			return DummyPoints(range.min, range.max); 
		}

		public static Constraint DummyPoints(Suit? trumpSuit, (int min, int max) range)
		{
			// TODO: Rename this too????  SuitPoints
			return new ShowsPoints(trumpSuit, range.min, range.max, HasPoints.PointType.Suit);
		}

		public static Constraint Shape(int min) { return new ShowsShape(null, min, min); }
		public static Constraint Shape(Suit suit, int count) { return new ShowsShape(suit, count, count); }
		public static Constraint Shape(int min, int max) { return new ShowsShape(null, min, max); }
		public static Constraint Shape(Suit suit, int min, int max) { return new ShowsShape(suit, min, max); }
		public static Constraint Balanced(bool desired = true) { return new ShowsBalanced(desired); }
		public static Constraint Flat(bool desired = true) { return new ShowsFlat(desired); }

		public static Constraint LastBid(int level, Suit suit, bool desired = true)
		{
			return new BidHistory(0, new Bid(level, suit), desired);
		}

		public static Constraint Rebid(bool desired = true)
		{
			return new BidHistory(0, null, desired);
		}


        /*
		public static Constraint DidBid(bool desired = true)
		{
			return new BidHistory(0, Call.Bid, 0, false, null, desired);
		}

		
		public static Constraint BidAtLevel(int level, bool desired = true)
		{
			return new BidHistory(0, Call.Bid, level, false, null, desired);
		}

		public static Constraint BidAtLevel(params int[] levels)
		{
			Constraint constraint = null;
			foreach (var level in levels)
			{
				Constraint levelConstraint = BidAtLevel(level);
				if (constraint == null)
				{
					constraint = levelConstraint;
				}
				else
				{
					constraint = Or(constraint, levelConstraint);
				}
			}
			return constraint;
		}
		

        public static Constraint DidDouble(bool desired = true)
		{
			return new BidHistory(0, Call.Double, 0, false, null, desired);
		}


		public static Constraint Passed(bool desired = true)
		{
			return new BidHistory(0, Call.Pass, 0, false, null, desired);
		}
		*/

		//	public static Constraint PartnerBid(Suit suit, bool desired = true)
		//		{ return new PositionProxy(PositionProxy.RelativePosition.Partner, new BidHistory(suit, desired)); }
		//		public static Constraint PartnerBid(int level, Suit suit, bool desired = true)
		//		{ return new PositionProxy(PositionProxy.RelativePosition.Partner, new BidHistory(level, suit, desired)); }

		//	public static Constraint PartnerShape(int count)
		//	{
		//		return PartnerShape(null, count, count);
		//	}


		//	public static Constraint PartnerShape(Suit? suit, int min, int max)
		//	{
		//		return new PositionProxy(PositionProxy.RelativePosition.Partner, new HasShape(suit, min, max));
		//	}

		public static Constraint Partner(Constraint constraint)
		{
			return new PositionProxy(PositionProxy.RelativePosition.Partner, constraint);
		}

		public static Constraint RHO(Constraint constraint)
		{
			return new PositionProxy(PositionProxy.RelativePosition.RightHandOpponent, constraint);
		}

		public static Constraint HasShape(int count)
		{
			return HasShape(count, count);
		}

		public static Constraint HasMinShape(int count)
		{
			return HasMinShape(null, count);
		}

		public static Constraint HasMinShape(Suit? suit, int count)
		{
			return new HasMinShape(suit, count);
		}


		public static Constraint HasShape(int min, int max)
		{
			return new HasShape(null, min, max);
		}

		public static Constraint Quality(SuitQuality min, SuitQuality max) {
			return new ShowsQuality(null, min, max);
		}
		public static Constraint Quality(Suit suit, SuitQuality min, SuitQuality max)
		{ return new ShowsQuality(suit, min, max); }

		public static Constraint And(params Constraint[] constraints)
		{
			return new ConstraintGroup(constraints);
			/*
			if (constraints.Length > 0 || constraints[0] is IShowsState)
			{
				return new CompositeShowsState(CompositeConstraint.Operation.And, constraints);
			}
			return new CompositeConstraint(CompositeConstraint.Operation.And, constraints);
			*/
		}
/*
		public static Constraint Or(params Constraint[] constraints)
		{
			return new CompositeConstraint(CompositeConstraint.Operation.Or, constraints);
		}
*/
        public static Constraint ExcellentSuit(Suit? suit = null)
        { return new ShowsQuality(suit, SuitQuality.Excellent, SuitQuality.Solid); }


        public static Constraint GoodSuit(Suit? suit = null)
		{ return new ShowsQuality(suit, SuitQuality.Good, SuitQuality.Solid); }

		public static Constraint DecentSuit(Suit? suit = null)
		{ return new ShowsQuality(suit, SuitQuality.Decent, SuitQuality.Solid); }

		public static Constraint Better(Suit better, Suit worse) { return new ShowsBetterSuit(better, worse, worse, false); }

		public static Constraint BetterOrEqual(Suit better, Suit worse) { return new ShowsBetterSuit(better, worse, better, false); }

		public static Constraint BetterThan(Suit worse) { return new ShowsBetterSuit(null, worse, worse, false); }

		public static Constraint BetterOrEqualTo(Suit worse) { return new ShowsBetterSuit(null, worse, null, false); }


		public static Constraint LongerThan(Suit shorter) { return new ShowsBetterSuit(null, shorter, shorter, true); }

		public static Constraint LongerOrEqualTo(Suit shorter) { return new ShowsBetterSuit(null, shorter, null, true); }
		public static Constraint Longer(Suit longer, Suit shorter) { return new ShowsBetterSuit(longer, shorter, shorter, true); }

		public static Constraint LongerOrEqual(Suit longer, Suit shorter) { return new ShowsBetterSuit(longer, shorter, longer, true); }



		public static Constraint DummyPoints(Suit trumpSuit, (int min, int max) range)
		{
			// TODO: Perhaps rename this to SuitPoints?  Maybe not?  Really should determine long hand or not.  Think this through
			return new ShowsPoints(trumpSuit, range.min, range.max, HasPoints.PointType.Suit);
		}

		public static Constraint LongestMajor(int max)
		{
			return And(Shape(Suit.Hearts, 0, max), Shape(Suit.Spades, 0, max));
		}


		public static Constraint Role(PositionRole role, int round = 0, bool desiredValue = true)
		{
			return new Role(role, round, desiredValue);
		}

		public static Constraint BidRound(int round)
		{
			return new BidRound(round);
		}

		

		public static Constraint ShowsTrump(Strain? trumpStrain = null)
		{
			return new ShowsTrump(trumpStrain);
		}

		public static Constraint ShowsTrump(Suit? trumpSuit)
		{
			if (trumpSuit == null) { return new ShowsTrump(null); }
			return new ShowsTrump(Call.SuitToStrain(trumpSuit));
		}

		public static Constraint Jump(params int[] jumpLevels)
		{
			return new JumpBid(jumpLevels);
		}



		public static Constraint CueBid(bool desiredValue = true)
		{
			return CueBid(null, desiredValue);
		}

		public static Constraint CueBid(Suit? suit, bool desiredValue = true)
		{
			return new CueBid(suit, desiredValue);
		}

		// Perhaps rename this.  Perhaps move this to takeout...
		public static Constraint TakeoutSuit(Suit? suit = null)
		{
			return And(new TakeoutSuit(suit), CueBid(false));
		}

		// TOOD: These are temporary for now.  But need to think them through.  
		public static Constraint Fit(int count = 8, Suit? suit = null, bool desiredValue = true)
		{
			return new PairShowsMinShape(suit, count, desiredValue);
		}

		public static Constraint Fit(Suit suit, bool desiredValue = true)
		{
			return Fit(8, suit, desiredValue);
		}

		public static Constraint Fit(bool desiredValue)
		{
			return Fit(8, null, desiredValue);
		}

		public static Constraint PairPoints((int Min, int Max) range)
		{
			return PairPoints(null, range);
		}

		public static Constraint PairPoints(Suit? suit, (int Min, int Max) range)
		{
			return new PairShowsPoints(suit, range.Min, range.Max);
		}

		// For this to be true, the partner must have shown the suit, AND this position must have 
		// at least minSupport cards in support
		//	public Constraint CanSupport(bool desiredValue = true, int minSupport = 3)
		//		{ 
		//			throw new NotImplementedException(); 
		//	}

		public static Constraint OppsStopped(bool desired = true)
		{
			// TODO: THIS SHOULD REALLY SHOW OPPS STOPPED TOO......
			return new ShowsOppsStopped(desired);
		}



		public static Constraint PassEndsAuction(bool desiredValue = true)
		{
			return new PassEndsAuction(desiredValue);
		}

		public static Constraint BidAvailable(int level, Suit suit, bool desiredValue = true)
		{ return new BidAvailable(level, suit, desiredValue); }


		public static Constraint RuleOf17(Suit? suit = null)
		{
			return new RuleOf17(suit);
		}

		public static Constraint Break(bool isStatic, string name)
		{
			return new Break(isStatic, name);
		}


		public static Constraint ShowsSuit(Suit suit)
		{
			return new ShowsSuit(true, suit);
		}
		public static Constraint ShowsSuits(params Suit[] suits)
		{
			return new ShowsSuit(false, suits);
		}

		public static Constraint HasShownSuit(Suit? suit = null)
		{
			return new HasShownSuit(suit);
		}

		public static Constraint ShowsSuit()
		{
			return new ShowsSuit(true, null);
		}
		public static Constraint ShowsNoSuit()
		{
			return new ShowsSuit(false, null);
		}

		public static Constraint OppsContract(bool desired = true)
		{ 
			return new OppsContract(desired); 
		}

		public static Constraint ConventionOn(string convention)
		{
			return new ConventionOn(convention);
		}



		// THE FOLLOWING CONSTRAINTS ARE GROUPS OF CONSTRAINTS
        public static Constraint RaisePartner(Suit? suit = null, int raise = 1, int fit = 8)
        {
            return And(Fit(fit, suit), Partner(HasShownSuit(suit)), Jump(raise - 1), ShowsTrump(suit));
        }
        public static Constraint RaisePartner(int level)
        {
            return RaisePartner(null, level);
        }
		public static Constraint RaisePartner(Suit suit)
		{
			return RaisePartner(suit, 1);
		}

    }
};

