
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
        public NaturalOvercall() : base()
        {
            this.ConventionRules = new ConventionRule[]
            {
                new ConventionRule(Role(PositionRole.Overcaller), BidRound(1))
            };
            this.BidRules = new BidRule[]
            {
                NonForcing(CallType.Pass, Points(LessThanOvercall)),

                NonForcing(1, Suit.Diamonds, Points(Overcall1Level), Shape(5, 11)),
                NonForcing(1, Suit.Hearts, Points(Overcall1Level), Shape(5, 11)),
                NonForcing(1, Suit.Spades, Points(Overcall1Level), Shape(5, 11))

            };
            this.NextConventionState = () => null;  // TODO: Advancer...
        }
    }

}
