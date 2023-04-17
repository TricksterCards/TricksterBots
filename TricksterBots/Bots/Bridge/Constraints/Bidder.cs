using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public abstract class Bidder
	{
		public BidConvention Convention { get; }

		public int DefaultPriority { get; }
		public abstract IEnumerable<BidRule> GetRules(PositionState positionState);

		public Bidder(BidConvention convention, int defaultPriority)
		{
			this.Convention = convention;
			this.DefaultPriority = defaultPriority;
		}

		public BidRule Forcing(int level, Suit suit, params Constraint[] constraints)
		{
			return Rule(level, suit, BidMessage.Forcing, DefaultPriority, constraints);
		}
		public BidRule Forcing(int level, Suit suit, int priority, params Constraint[] constraints)
		{
			return Rule(level, suit, BidMessage.Forcing, priority, constraints);
		}
		public BidRule Forcing(CallType callType, params Constraint[] constraints)
		{
			return Rule(callType, BidMessage.Forcing, DefaultPriority, constraints);
		}
		public BidRule Forcing(CallType callType, int priority, params Constraint[] constraints)
		{
			return Rule(callType, BidMessage.Forcing, priority, constraints);
		}

		// TODO: Need a non-forcing BidMessage...
		public BidRule NonForcing(int level, Suit suit, params Constraint[] constraints)
		{
			return Rule(level, suit, BidMessage.Invitational, DefaultPriority, constraints);
		}
		public BidRule NonForcing(int level, Suit suit, int priority, params Constraint[] constraints)
		{
			return Rule(level, suit, BidMessage.Invitational, priority, constraints);
		}
		public BidRule NonForcing(CallType callType, params Constraint[] constraints)
		{
			return Rule(callType, BidMessage.Invitational, DefaultPriority, constraints);
		}
		public BidRule NonForcing(CallType callType, int priority, params Constraint[] constraints)
		{
			return Rule(callType, BidMessage.Invitational, priority, constraints);
		}


		public BidRule Invitational(int level, Suit suit, params Constraint[] constraints)
		{
			return Rule(level, suit, BidMessage.Invitational, DefaultPriority, constraints);
		}
		public BidRule Invitational(int level, Suit suit, int priority, params Constraint[] constraints)
		{
			return Rule(level, suit, BidMessage.Invitational, priority, constraints);
		}
		public BidRule Invitational(CallType callType, params Constraint[] constraints)
		{
			return Rule(callType, BidMessage.Invitational, DefaultPriority, constraints);
		}
		public BidRule Invitational(CallType callType, int priority, params Constraint[] constraints)
		{
			return Rule(callType, BidMessage.Invitational, priority, constraints);
		}


		public BidRule Signoff(int level, Suit suit, params Constraint[] constraints)
		{
			return Rule(level, suit, BidMessage.Signoff, DefaultPriority, constraints);
		}
		public BidRule Signoff(int level, Suit suit, int priority, params Constraint[] constraints)
		{
			return Rule(level, suit, BidMessage.Signoff, priority, constraints);
		}
		public BidRule Signoff(CallType callType, params Constraint[] constraints)
		{
			return Rule(callType, BidMessage.Signoff, DefaultPriority, constraints);
		}
		public BidRule Signoff(CallType callType, int priority, params Constraint[] constraints)
		{
			return Rule(callType, BidMessage.Signoff, priority, constraints);
		}


		public BidRule Rule(int level, Suit suit, BidMessage message, int priority, params Constraint[] constraints)
		{
			return new BidRule(new Bid(level, suit, Convention, message), priority, constraints);
		}


		public BidRule Rule(CallType callType, BidMessage message, int priority, params Constraint[] constraints)
		{
			return new BidRule(new Bid(callType, Convention, message), priority, constraints);
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

		public static Constraint PreviousBid(Suit suit, bool desired = true) { return new BidHistory(suit, desired); }
		public static Constraint PreviousBid(int level, Suit suit, bool desired = true) { return new BidHistory(level, suit, desired); }

		public static Constraint PartnerBid(Suit suit, bool desired = true)
		{ return new PositionProxy(PositionProxy.RelativePosition.Partner, new BidHistory(suit, desired)); }
		public static Constraint PartnerBid(int level, Suit suit, bool desired = true)
		{ return new PositionProxy(PositionProxy.RelativePosition.Partner, new BidHistory(level, suit, desired)); }

		public static Constraint PartnerShape(Suit suit, int min, int max)
		{
			return new PositionProxy(PositionProxy.RelativePosition.Partner, new HasShape(suit, min, max));
		}


		public static Constraint Quality(SuitQuality min, SuitQuality max) {
			return new ShowsQuality(null, min, max);
		}
		public static Constraint Quality(Suit suit, SuitQuality min, SuitQuality max)
		{ return new ShowsQuality(suit, min, max); }


		public static Constraint BetterSuit(Suit better, Suit worse) { return new ShowsBetterSuit(better, worse, null, false); }

		public static Constraint BetterSuitThan(Suit worse) { return new ShowsBetterSuit(null, worse, null, false); }


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
			return new CompositeConstraint(new ShowsShape(Suit.Hearts, 0, max), new ShowsShape(Suit.Spades, 0, max));
		}

		// TODO: This should probably move to Natural..  But for now, this seems fine....
		/*
		public static BidRule[] HighLevelHugeHands = new BidRule[]
		{
			new BidRule(6, Suit.Clubs, BidConvention.None, 1000, Shape(12)),
			new BidRule(6, Suit.Diamonds, BidConvention.None, 1000, Shape(12)),
			new BidRule(6, Suit.Hearts, BidConvention.None, 1000, Shape(12)),
			new BidRule(6, Suit.Spades, BidConvention.None, 1000, Shape(12)),

			Signoff(7, Suit.Clubs, Shape(13)),
			Signoff(7, Suit.Diamonds, Shape(13)),
			Signoff(7, Suit.Hearts, Shape(13)),
			Signoff(7, Suit.Spades, Shape(13))
		}
		*/
	}
};


	}
}
