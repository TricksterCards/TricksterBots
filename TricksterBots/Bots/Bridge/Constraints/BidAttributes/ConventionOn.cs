using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class ConventionOn : Constraint
    {
        private string _convention;
        public ConventionOn(string convention)
        {
            this._convention = convention;
            this.StaticConstraint = true;
        }

        public override bool Conforms(Call _ignored, PositionState ps, HandSummary hs)
        {
            if (ps.BiddingState.Conventions.ContainsKey(_convention))
            {
                var goodThrough = ps.BiddingState.Conventions[_convention];
                if (goodThrough == null) { return false; }
                var contract = ps.BiddingState.Contract;
                if (goodThrough.Equals(Call.Pass)) { return contract.LastBidBy == ps.Partner && !contract.Doubled; }
                if (goodThrough.Equals(Call.Double)) { return contract.LastBidBy == ps.Partner; }
                // TODO: Add redouble here ......
                return contract.IsValid(goodThrough, ps);
            }
            return false;
        }
    }
}

