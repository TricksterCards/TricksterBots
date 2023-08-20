using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class HasShownSuit : Constraint
    {
        Suit? _suit;
        public HasShownSuit(Suit? suit)
        {
            this._suit = suit;
            this.StaticConstraint = true;
        }
        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {
            if (GetSuit(_suit, call) is Suit suit)
            {
                return ps.PairState.Agreements.Suits[suit].LongHand == ps;
            }
            Debug.Fail("No suit for call in HasShownSuit constraint.");
            return false;
        }
    }
}
