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
        }

        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs, PairAgreements pa)
        {
            return ps.BidRound == _bidRound;
        }
    }
}
