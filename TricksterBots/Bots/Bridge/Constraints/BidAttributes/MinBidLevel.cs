using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class BidAvailable : Constraint
    {
        private Bid _bid;
        private bool _desiredValue;

        public BidAvailable(int level, Suit suit, bool desiredValue)
        {
            this._bid = new Bid(level, suit);
            this.StaticConstraint = true;
            _desiredValue = desiredValue;   
        }
        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {
            return _desiredValue == ps.IsValidNextCall(_bid);
        }
    }
}
