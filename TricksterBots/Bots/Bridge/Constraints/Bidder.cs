using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        public abstract IEnumerable<BidRule> GetRules(BidXXX xxx, Direction direction, BiddingSummary biddingSummary);

        public Bidder(BidConvention convention, int defaultPriority)
        {
            this.Convention = convention;
            this.DefaultPriority = defaultPriority;
        }
        public BidRule Rule(int level, Suit suit, params Constraint[] constraints)
        {
            return new BidRule(level, suit, Convention, DefaultPriority, constraints);
        }

		public BidRule Rule(int level, Suit suit, int priority, params Constraint[] constraints)
		{
			return new BidRule(level, suit, Convention, DefaultPriority, constraints);
		}

		public BidRule Rule(CallType callType, params Constraint[] constraints)
		{
			return new BidRule(callType, Convention, DefaultPriority, constraints);
		}

		public BidRule Rule(CallType callType, int priority, params Constraint[] constraints)
		{
			return new BidRule(callType, Convention, priority, constraints);
		}


		public static Constraint Points((int min, int max) range) { return new Points(range.min, range.max); }

        public static Constraint DummyPoints((int min, int max) range) { return new Points(null, range.min, range.max); }   

        public static Constraint DummyPoints(Suit? trumpSuit, (int min, int max) range) 
        {
            return new Points(trumpSuit, range.min, range.max);
		}

        public static Constraint Shape(int min) { return new Shape(min, min); }
        public static Constraint Shape(Suit suit, int count) { return new Shape(suit, count, count); }
        public static Constraint Shape(int min, int max) { return new Shape(min, max); }
        public static Constraint Shape(Suit suit, int min, int max) { return new Bridge.Shape(suit, min, max); }
        public static Constraint Balanced(bool desired = true) { return new Balanced(desired); }
        public static Constraint Flat(bool desired = true) { return new Flat(desired); }

        public static Constraint PartnerBid(Suit suit, bool desired = true) { return new PartnerBid(suit, desired); }
        public static Constraint PartnerBid(int level, Suit suit, bool desired = true) { return new PartnerBid(level, suit, desired); }

        public static Constraint PartnerShows(Suit suit, int count)
        {
            throw new NotImplementedException();    // TODO: Need PartnerShows constraint class..
        }

		public static Constraint PreviousBid(Suit suit, bool desired = true) { return new PreviousBid(suit, desired); }
		public static Constraint PreviousBid(int level, Suit suit, bool desired = true) { return new PreviousBid(level, suit, desired); }



		public static Constraint Quality(SuitQuality suitQuality) { return new Quality(suitQuality); }
        public static Constraint Quality(Suit suit, SuitQuality quality) { return new Quality(suit, quality);  }


        public static Constraint BetterSuit(Suit better, Suit worse) { return new BetterSuit(better, worse, null, false); }

        public static Constraint BetterSuitThan(Suit worse) { return new BetterSuit(null, worse, null, false);  }


        public static Constraint LongerThan(Suit shorter) { return new BetterSuit(null, shorter, shorter, true); }

        public static Constraint LongerOrEqualTo(Suit shorter) { return new BetterSuit(null, shorter, null, true); }
        public static Constraint Longer(Suit longer, Suit shorter) { return new BetterSuit(longer, shorter, shorter, true); }

        public static Constraint LongerOrEqual(Suit longer, Suit shorter) { return new BetterSuit(longer, shorter, longer, true);  }



        public static Constraint DummyPoints(Suit trumpSuit, (int min, int max) range)
        {
            return new Points(trumpSuit, range.min, range.max);
        }

        public static Constraint LongestMajor(int max)
        {
            return new CompositeConstraint(new Shape(Suit.Hearts, 0, max), new Shape(Suit.Spades, 0, max));
        }

        // TODO: This should probably move to Natural..  But for now, this seems fine....
		public static BidRule[] HighLevelHugeHands = new BidRule[]
        {
			new BidRule(6, Suit.Clubs, BidConvention.None, 1000, Shape(12)),
			new BidRule(6, Suit.Diamonds, BidConvention.None, 1000, Shape(12)),
			new BidRule(6, Suit.Hearts, BidConvention.None, 1000, Shape(12)),
			new BidRule(6, Suit.Spades, BidConvention.None, 1000, Shape(12)),

			new BidRule(7, Suit.Clubs, BidConvention.None, 1000, Shape(13)),
			new BidRule(7, Suit.Diamonds, BidConvention.None, 1000, Shape(13)),
			new BidRule(7, Suit.Hearts, BidConvention.None, 1000, Shape(13)),
			new BidRule(7, Suit.Spades, BidConvention.None, 1000, Shape(13)),
};


	}
}
