using System;
using System.Collections.Generic;
using System.Diagnostics;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class CueBid : Constraint
    {
        private Suit? _suit;
        private bool _desiredValue;
        public CueBid(Suit? suit, bool desiredValue)
        {
            this._suit = suit;
            this._desiredValue = desiredValue;
            this.StaticConstraint = true;
        }
        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {
            var suit = _suit;
            if (suit == null && call is Bid bid)
            {
                suit = bid.Suit;
            }
            if (suit == null)
            {
                Debug.Fail("No suit specified for cuebid");
                return false;
            }

            var pairSummary = PairSummary.Opponents(ps);
            if (pairSummary.ShownSuits.Contains((Suit)suit))
            {
                // It is a CueBid.  Return TRUE IFF we want a cuebid else false (we don't conform).
                return _desiredValue;
            }
            return !_desiredValue;
        }
    }
}
