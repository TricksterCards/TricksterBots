using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
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
            this.OnceAndDone = true;
        }
        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs, PairAgreements pa)
        {
            var pairSummary = PairSummary.Opponents(ps);
            if (pairSummary.ShownSuits.Contains(bid.SuitIfNot(_suit)))
            {
                // It is a CueBid.  Return TRUE IFF we want a cuebid else false (we don't conform).
                return _desiredValue;
            }
            return !_desiredValue;
        }
    }
}
