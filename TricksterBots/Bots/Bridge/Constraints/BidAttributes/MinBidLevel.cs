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
        private int _level;
        private Suit _suit;

        public BidAvailable(int level, Suit suit)
        {
            Debug.Assert(level >= 1 && level <= 7);
            this._level = level;
            this._suit = suit;
            this.OnceAndDone = true;
        }
        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs, PairAgreements pa)
        {
            var contract = ps.BiddingState.GetContract();
            (bool Valid, int Jump) v = new Bid(_level, _suit, BidForce.Nonforcing).IsValid(ps, contract);
            return v.Valid;
        }
    }
}
