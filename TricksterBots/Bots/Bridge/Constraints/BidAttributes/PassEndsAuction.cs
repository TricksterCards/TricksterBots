using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{
    public class PassEndsAuction : Constraint
    {
        private bool _desiredValue;
        public PassEndsAuction(bool desiredValue) 
        {
            this._desiredValue = desiredValue;
            this.OnceAndDone = true;
        }

        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
        {
            return (_desiredValue == ps.BiddingState.PassEndsAuction());
        }
    }
}
