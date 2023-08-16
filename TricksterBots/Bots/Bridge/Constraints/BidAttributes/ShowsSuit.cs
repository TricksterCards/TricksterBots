﻿using System;
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
        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {
            return true;
        }

        void IShowsState.ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
        {
            if (_showBidSuit &&
                GetSuit(null, call) is Suit suit)
            {
                showAgreements.Suits[suit].ShowLongHand(ps);
            }
            if (_suits != null)
            {
                foreach (var s in _suits)
                {
                    showAgreements.Suits[s].ShowLongHand(ps);
                }
            }
        }
    }
}
