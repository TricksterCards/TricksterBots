using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class ShowsTrump: Constraint, IShowsState
    {
        private Suit? _trumpSuit;
        public ShowsTrump(Suit? trumpSuit)
        {
            this._trumpSuit = trumpSuit;
        }
        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs, BiddingSummary bs)
        {
            return true;
        }

        void IShowsState.Update(Bid bid, PositionState ps, HandSummary hs, BiddingSummary bs)
        {
            bs.TrumpSuit = _trumpSuit;
        }
    }
}
