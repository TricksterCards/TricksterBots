using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;
using static TricksterBots.Bots.Bridge.ConventionRule;

namespace TricksterBots.Bots.Bridge
{

	public delegate Bidder BidderFactory();

	public abstract class Bidder
	{
		public Convention Convention { get; }

		public int DefaultPriority { get; }
		public IEnumerable<ConventionRule> ConventionRules { get; protected set; } = null;
		public IEnumerable<BidRule> BidRules { get; protected set; }

		public IEnumerable<RedirectRule> Redirects { get; protected set; } = null;

        public BidderFactory NextConventionState { get; protected set; }


        // Convention rules..
        public ConventionRule ConventionRule(params Constraint[] constraints)
		{
			return new ConventionRule(constraints);
		}


		// TODO: This is a bit of a hack - where to have the logic.
		public bool Applies(PositionState ps)
		{
			if (ConventionRules == null) { return true; }
			foreach (var rule in ConventionRules)
			{
				if (rule.Conforms(ps)) { return true; }
			}
			return false;
		}


        public Bidder(Convention convention, int defaultPriority)
		{
			this.Convention = convention;
			this.DefaultPriority = defaultPriority;
		}

		public BidRule Forcing(int level, Suit suit, params Constraint[] constraints)
		{
			return Rule(level, suit, BidForce.Forcing, DefaultPriority, constraints);
		}
		public BidRule Forcing(int level, Suit suit, int priority, params Constraint[] constraints)
		{
			return Rule(level, suit, BidForce.Forcing, priority, constraints);
		}
		public BidRule Forcing(CallType callType, params Constraint[] constraints)
		{
			return Rule(callType, BidForce.Forcing, DefaultPriority, constraints);
		}
		public BidRule Forcing(CallType callType, int priority, params Constraint[] constraints)
		{
			return Rule(callType, BidForce.Forcing, priority, constraints);
		}

		// TODO: Need a non-forcing BidMessage...
		public BidRule Nonforcing(int level, Suit suit, params Constraint[] constraints)
		{
			return Rule(level, suit, BidForce.Nonforcing, DefaultPriority, constraints);
		}
		public BidRule Nonforcing(int level, Suit suit, int priority, params Constraint[] constraints)
		{
			return Rule(level, suit, BidForce.Nonforcing, priority, constraints);
		}
		public BidRule Nonforcing(CallType callType, params Constraint[] constraints)
		{
			return Rule(callType, BidForce.Nonforcing, DefaultPriority, constraints);
		}
		public BidRule Nonforcing(CallType callType, int priority, params Constraint[] constraints)
		{
			return Rule(callType, BidForce.Nonforcing, priority, constraints);
		}


		public BidRule Invitational(int level, Suit suit, params Constraint[] constraints)
		{
			return Rule(level, suit, BidForce.Invitational, DefaultPriority, constraints);
		}
		public BidRule Invitational(int level, Suit suit, int priority, params Constraint[] constraints)
		{
			return Rule(level, suit, BidForce.Invitational, priority, constraints);
		}
		public BidRule Invitational(CallType callType, params Constraint[] constraints)
		{
			return Rule(callType, BidForce.Invitational, DefaultPriority, constraints);
		}
		public BidRule Invitational(CallType callType, int priority, params Constraint[] constraints)
		{
			return Rule(callType, BidForce.Invitational, priority, constraints);
		}


		public BidRule Signoff(int level, Suit suit, params Constraint[] constraints)
		{
			return Rule(level, suit, BidForce.Signoff, DefaultPriority, constraints);
		}
		public BidRule Signoff(int level, Suit suit, int priority, params Constraint[] constraints)
		{
			return Rule(level, suit, BidForce.Signoff, priority, constraints);
		}
		public BidRule Signoff(CallType callType, params Constraint[] constraints)
		{
			return Rule(callType, BidForce.Signoff, DefaultPriority, constraints);
		}
		public BidRule Signoff(CallType callType, int priority, params Constraint[] constraints)
		{
			return Rule(callType, BidForce.Signoff, priority, constraints);
		}


		public BidRule Rule(int level, Suit suit, BidForce force, int priority, params Constraint[] constraints)
		{
			return new BidRule(new Bid(level, suit, force), priority, constraints);
		}


		public BidRule Rule(CallType callType, BidForce force, int priority, params Constraint[] constraints)
		{
			return new BidRule(new Bid(callType, force), priority, constraints);
		}


		public static Constraint Points((int min, int max) range) { return new ShowsPoints(null, range.min, range.max); }

		public static Constraint DummyPoints((int min, int max) range) { return new ShowsPoints(null, range.min, range.max); }

		public static Constraint DummyPoints(Suit? trumpSuit, (int min, int max) range)
		{
			return new ShowsPoints(trumpSuit, range.min, range.max);
		}

		public static Constraint Shape(int min) { return new ShowsShape(null, min, min); }
		public static Constraint Shape(Suit suit, int count) { return new ShowsShape(suit, count, count); }
		public static Constraint Shape(int min, int max) { return new ShowsShape(null, min, max); }
		public static Constraint Shape(Suit suit, int min, int max) { return new ShowsShape(suit, min, max); }
		public static Constraint Balanced(bool desired = true) { return new ShowsBalanced(desired); }
		public static Constraint Flat(bool desired = true) { return new ShowsFlat(desired); }

		public static Constraint LastBid(int level, Suit suit, bool desired = true) {
			return new BidHistory(CallType.Bid, level, suit, desired); }

		public static Constraint DidBid(bool desired = true)
		{
			return new BidHistory(CallType.Bid, 0, Suit.Unknown, desired);
		}

		public static Constraint DidDouble(bool desired = true)
		{
			return new BidHistory(CallType.Double, 0, Suit.Unknown, desired);
		}


		public static Constraint Passed(bool desired = true)
		{
			return new BidHistory(CallType.Pass, 0, Suit.Unknown, desired);
		}

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
			return new HasShape(null, count, count);
		}

		public static Constraint Quality(SuitQuality min, SuitQuality max) {
			return new ShowsQuality(null, min, max);
		}
		public static Constraint Quality(Suit suit, SuitQuality min, SuitQuality max)
		{ return new ShowsQuality(suit, min, max); }


		public static Constraint Better(Suit better, Suit worse) { return new ShowsBetterSuit(better, worse, worse, false); }

		public static Constraint BetterOrEqual(Suit better, Suit worse) { return new ShowsBetterSuit(better, worse, better, false);  }

		public static Constraint BetterThan(Suit worse) { return new ShowsBetterSuit(null, worse, worse, false); }

		public static Constraint BetterOrEqualTo(Suit worse) { return new ShowsBetterSuit(null, worse, null, false);  }


		public static Constraint LongerThan(Suit shorter) { return new ShowsBetterSuit(null, shorter, shorter, true); }

		public static Constraint LongerOrEqualTo(Suit shorter) { return new ShowsBetterSuit(null, shorter, null, true); }
		public static Constraint Longer(Suit longer, Suit shorter) { return new ShowsBetterSuit(longer, shorter, shorter, true); }

		public static Constraint LongerOrEqual(Suit longer, Suit shorter) { return new ShowsBetterSuit(longer, shorter, longer, true); }



		public static Constraint DummyPoints(Suit trumpSuit, (int min, int max) range)
		{
			return new ShowsPoints(trumpSuit, range.min, range.max);
		}

		public static Constraint LongestMajor(int max)
		{
			return new CompositeShowsState(new ShowsShape(Suit.Hearts, 0, max), new ShowsShape(Suit.Spades, 0, max));
		}

		// TODO: This should probably move to Natural..  But for now, this seems fine....



		// THIS IS ALL CONVENTION RULE STUFF
		public static Constraint Role(PositionRole role, int round = 0)
		{
			return new Role(role, round);
		}
		
		public static Constraint BidRound(int round)
		{
			return new BidRound(round);
		}
		public static Constraint OffIfRhoBid()
		{
			// TODO: Need to implement this one... 
			// Need to improve BidHistory class...
			throw new NotImplementedException();
		}

		public static Constraint ShowsTrump(Suit? trumpSuit = null)
		{
			return new ShowsTrump(trumpSuit);
		}


	}
};

