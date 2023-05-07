using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{

    public class NaturalAdvance : Natural
    {
        
        public NaturalAdvance() : base()
        {
            // The Overcaller always specifies this class as the next state, but if they pass then it no longer applies
            // Bail if we are not the Advancer.
            this.ConventionRules = new ConventionRule[]
            {
                ConventionRule(Role(PositionRole.Advancer))
            };

            this.BidRules = new BidRule[]
            {
                Nonforcing(CallType.Pass, DefaultPriority - 100),   // TODO: What points?  What shape?

           
                Nonforcing(1, Suit.Hearts, DefaultPriority - 20, Points(AdvanceNewSuit1Level), Shape(5), GoodSuit()),
                Nonforcing(1, Suit.Hearts, DefaultPriority - 20, Points(AdvanceNewSuit1Level), Shape(6, 11)),

                Nonforcing(1, Suit.Spades, DefaultPriority - 20, Points(AdvanceNewSuit1Level), Shape(5), GoodSuit()),
                Nonforcing(1, Suit.Spades, DefaultPriority - 20, Points(AdvanceNewSuit1Level), Shape(6, 11)),

                // TODO: Need to have opps stopped?? What if not?  Then what?
                Nonforcing(1, Suit.Unknown, DefaultPriority - 10, Points(AdvanceTo1NT)),

                Nonforcing(2, Suit.Diamonds, Partner(HasShape(5, 11)), Shape(3, 8), DummyPoints(AdvanceRaise), ShowsTrump()),
                Nonforcing(2, Suit.Hearts, Partner(HasShape(5, 11)), Shape(3, 8), DummyPoints(AdvanceRaise), ShowsTrump()),
                Nonforcing(2, Suit.Spades, Partner(HasShape(5, 11)), Shape(3, 8), DummyPoints(AdvanceRaise), ShowsTrump())
            };
        }
    }

}
