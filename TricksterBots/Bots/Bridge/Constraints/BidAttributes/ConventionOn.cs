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
            this.OnceAndDone = true;
        }

        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
        {
            if (ps.BiddingState.Conventions.ContainsKey(_convention))
            {
                var goodThrough = ps.BiddingState.Conventions[_convention];
                if (goodThrough.Call == Call.NotActed) { return false; }
                var contract = ps.BiddingState.Contract;
                if (goodThrough.IsPass) { return contract.By == ps.Partner && !contract.Doubled; }
                if (goodThrough.Call == Call.Double) { return contract.By == ps.Partner; }
                // TODO: Add redouble here ......
                return ps.IsValidNextBid(goodThrough);
            }
            return false;
        }
    }
}

