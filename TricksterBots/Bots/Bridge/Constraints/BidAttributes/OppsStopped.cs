using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{

    public class IsOppsStopped : Constraint
    {
        bool _desiredValue;
        public IsOppsStopped(bool desiredValue)
        {
            this._desiredValue = desiredValue;
        }

        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs, PairAgreements pa)
        {
            var ourSummary = new PairSummary(ps);
            var oppsSummary = PairSummary.Opponents(ps);
            foreach (var suit in oppsSummary.ShownSuits)
            {
                // Stopped could be null or true of false.  Only not stopped for sure if false...
                if (ourSummary.Suits[suit].Stopped == false && _desiredValue == true) { return false; }                    
            }
            return _desiredValue;
        }
    }

    // TODO: How to show that a suit is stopped???  If partner has not shown, and we 
}
