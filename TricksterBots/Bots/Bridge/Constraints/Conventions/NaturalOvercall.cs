
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
                Nonforcing(CallType.Pass, Points(LessThanOvercall)),

                Nonforcing(1, Suit.Diamonds, Points(Overcall1Level), Shape(5, 11)),
                Nonforcing(1, Suit.Hearts, Points(Overcall1Level), Shape(5, 11)),
                Nonforcing(1, Suit.Spades, Points(Overcall1Level), Shape(5, 11)),

                // TODO: NT Overcall needs to have suit stopped...

                // TODO: Takeout X here?  Maybe in takeout X convention...  Probably that...
                // TODO: NEED TO IMPLEMENT IsJump() and make both weak and strong work here
               // Nonforcing(2, Suit.Diamonds, Shape(6, 11), Points(OvercallWeak2Level), IsJump(1), Quality(SuitQuality.Good, SuitQuality.Solid))
                Nonforcing(2, Suit.Clubs, Points(OvercallStrong2Level), Shape(5, 11)),
                Nonforcing(2, Suit.Diamonds, Points(OvercallStrong2Level), Shape(5, 11)),
                Nonforcing(2, Suit.Hearts, Points(OvercallStrong2Level), Shape(5, 11)),
                Nonforcing(2, Suit.Spades, Points(OvercallStrong2Level), Shape(5, 11))
            };
        }
    }

}
