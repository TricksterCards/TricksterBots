using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class RuleOf17 : Constraint
    {
        private Suit? _suit;

        public RuleOf17(Suit? suit)
        {
            _suit = suit;
        }

        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs, PairAgreements pa)
        {
            // Note that we use the Max points for this rule.  This means that for unknown hands we
            // will always conform (it's possible that rule of 17 will work) but for specific hands
            // we will eleminate the bid.
            var pts = hs.HighCardPoints.Max;
            pts += hs.Suits[bid.SuitIfNot(_suit)].Shape.Max;
            return pts >= 17;
        }
    }
}
