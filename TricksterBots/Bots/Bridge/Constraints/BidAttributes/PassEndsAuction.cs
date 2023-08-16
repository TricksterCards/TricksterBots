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
            this.StaticConstraint = true;
        }

        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {
            return (_desiredValue == ps.BiddingState.Contract.PassEndsAuction);
        }
    }
}
