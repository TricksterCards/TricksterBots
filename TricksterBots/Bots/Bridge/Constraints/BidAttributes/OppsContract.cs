using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TricksterBots.Bots.Bridge
{
    public class OppsContract : Constraint
    {
        bool _desired;
        public OppsContract(bool desired)
        {
            _desired = desired;
            this.OnceAndDone = true;
        }
        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
        {
            return _desired == ps.IsOpponentsContract;
        }
    }
}
