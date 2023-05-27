using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class ShowsSuit : Constraint, IShowsState
    {
        private Suit[] _suits;
        private bool _showBidSuit;
        public ShowsSuit(bool showBidSuit, params Suit[] suits)
        {
            this._showBidSuit = showBidSuit;
            this._suits = suits;
        }
        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
        {
            return true;
        }

        void IShowsState.ShowState(Bid bid, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
        {
            if (_showBidSuit)
            {
                showAgreements.Suits[bid.SuitIfNot(null)].ShowLongHand(ps);
            }
            if (_suits != null)
            {
                foreach (var suit in _suits)
                {
                    showAgreements.Suits[suit].ShowLongHand(ps);
                }
            }
        }
    }
}
