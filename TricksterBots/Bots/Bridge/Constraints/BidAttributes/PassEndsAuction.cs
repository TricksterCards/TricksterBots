using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BridgeBidding 
{
    public class PassEndsAuction : StaticConstraint
    {
        private bool _desiredValue;
        public PassEndsAuction(bool desiredValue) 
        {
            this._desiredValue = desiredValue;
        }

        public override bool Conforms(Call call, PositionState ps)
        {
            return (_desiredValue == ps.BiddingState.Contract.PassEndsAuction);
        }
    }
}
