using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{
    public class BidRound : Constraint
    {
        private int _bidRound;
        public BidRound(int round)
        {
            Debug.Assert(round > 0);
            this._bidRound = round;
            this.StaticConstraint = true;
        }

        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {
            return ps.BidRound == _bidRound;
        }
    }
}
