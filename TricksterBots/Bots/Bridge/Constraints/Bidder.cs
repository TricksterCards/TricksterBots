using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class Bidder
    {
        public static Constraint Points((int min, int max) range) { return new Points(range.min, range.max); }

        public static Constraint Shape(int min) { return new Shape(min, min); }
        public static Constraint Shape(Suit suit, int min) { return new Shape(suit, min, min); }
        public static Constraint Shape(int min, int max) { return new Shape(min, max); }
        public static Constraint Shape(Suit suit, int min, int max) { return new Bridge.Shape(suit, min, max); }
        public static Constraint Balanced(bool desired = true) { return new Balanced(desired); }
        public static Constraint Flat(bool desired = true) { return new Flat(desired); }

        public static Constraint PartnerBid(Suit suit, bool desired = true) { return new PartnerBid(suit, desired); }
        public static Constraint PartnerBid(int level, Suit suit, bool desired = true) { return new PartnerBid(level, suit, desired); }


        public static Constraint Quality(SuitQuality suitQuality) { return new Quality(suitQuality); }
        public static Constraint Quality(Suit suit, SuitQuality quality) { return new Quality(suit, quality);  }


        public static Constraint DummyPoints(Suit trumpSuit, (int min, int max) range)
        {
            return new Points(trumpSuit, range.min, range.max);
        }

        public static Constraint LongestMajor(int max)
        {
            return new CompositeConstraint(new Shape(Suit.Hearts, 0, max), new Shape(Suit.Spades, 0, max));
        }


        public static BidRule[] HighLevelHugeHands = new BidRule[]
        {
                new BidRule(6, Suit.Clubs, 1000, Shape(12)),
                new BidRule(6, Suit.Diamonds, 1000, Shape(12)),
                new BidRule(6, Suit.Hearts, 1000, Shape(12)),
                new BidRule(6, Suit.Spades, 1000, Shape(12)),

                new BidRule(7, Suit.Clubs, 1000, Shape(13)),
                new BidRule(7, Suit.Diamonds, 1000, Shape(13)),
                new BidRule(7, Suit.Hearts, 1000, Shape(13)),
                new BidRule(7, Suit.Spades, 1000, Shape(13)),
         };

    }
}
