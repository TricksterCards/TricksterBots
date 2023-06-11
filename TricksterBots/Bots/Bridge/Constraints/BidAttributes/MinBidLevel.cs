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
        private bool _desiredValue;

        public BidAvailable(int level, Suit suit, bool desiredValue)
        {
            Debug.Assert(level >= 1 && level <= 7);
            this._level = level;
            this._suit = suit;
            this.OnceAndDone = true;
            _desiredValue = desiredValue;   
        }
        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
        {
            var contract = ps.BiddingState.GetContract();
            (bool Valid, int Jump) v = new Bid(_level, _suit).IsValid(ps, contract);
            return _desiredValue == v.Valid;
        }
    }
}
