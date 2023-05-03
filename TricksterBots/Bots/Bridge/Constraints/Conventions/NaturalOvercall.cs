
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class NaturalOvercall : Natural
    {
        public static Bidder Overcall() { return new NaturalOvercall(); }
        public NaturalOvercall() : base()
        {
            this.BidRules = new BidRule[]
            {
                Nonforcing(CallType.Pass, DefaultPriority - 100, Points(LessThanOvercall)),

                Nonforcing(1, Suit.Diamonds, Points(Overcall1Level), Shape(5, 11)),
                Nonforcing(1, Suit.Hearts, Points(Overcall1Level), Shape(5, 11)),
                Nonforcing(1, Suit.Spades, Points(Overcall1Level), Shape(5, 11)),

                // TODO: NT Overcall needs to have suit stopped...

                Nonforcing(2, Suit.Clubs, CueBid(false), Points(OvercallStrong2Level), Shape(5, 11)),

                Nonforcing(2, Suit.Diamonds, Jump(0), CueBid(false), Points(OvercallStrong2Level), Shape(5, 11)),
                Nonforcing(2, Suit.Diamonds, Jump(1), CueBid(false), Points(OvercallWeak2Level), Shape(6), GoodSuit()),

                Nonforcing(2, Suit.Hearts, Jump(0), CueBid(false), Points(OvercallStrong2Level), Shape(5, 11)),
                Nonforcing(2, Suit.Hearts, Jump(1), CueBid(false), Points(OvercallWeak2Level), Shape(6), GoodSuit()),

                Nonforcing(2, Suit.Spades, Jump(0), CueBid(false), Points(OvercallStrong2Level), Shape(5, 11)),
                Nonforcing(2, Suit.Spades, Jump(1), CueBid(false), Points(OvercallWeak2Level), Shape(6), GoodSuit()),

                Nonforcing(3, Suit.Clubs, Jump(1), CueBid(false), Points(OvercallWeak3Level), Shape(7), DecentSuit()),
				Nonforcing(3, Suit.Diamonds, Jump(1, 2), CueBid(false), Points(OvercallWeak3Level), Shape(7), DecentSuit()),
				Nonforcing(3, Suit.Hearts, Jump(1, 2), CueBid(false), Points(OvercallWeak3Level), Shape(7), DecentSuit()),
				Nonforcing(3, Suit.Spades, Jump(1, 2), CueBid(false), Points(OvercallWeak3Level), Shape(7), DecentSuit()),


			};
            this.NextConventionState = () => new NaturalAdvance();
        }
    }

}
