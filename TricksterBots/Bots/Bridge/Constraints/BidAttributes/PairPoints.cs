using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class PairHasPoints : Constraint // TODO: Needs to show state too.  But for now passive..
    {
        protected Suit? _suit;
        protected int _min;
        protected int _max;
        public PairHasPoints(Suit? suit, int min, int max)
        {
            this._suit = suit;
            this._min = min;
            this._max = max;
            Debug.Assert(max >= min);
        }

        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
        {
            // TODO: PRIORITY!!!
            // TODO: NEED TO DO SUIT POINTS.  STARTING ONLY FOR NOW...
            (int Min, int Max) points = hs.GetStartingPoints();
            (int Min, int Max) partnerPoints = ps.Partner.PublicHandSummary.GetStartingPoints();
            return (points.Max + partnerPoints.Min >= _min && points.Min + partnerPoints.Min <= _max);
        }
    }

    public class PairShowsPoints : PairHasPoints, IShowsState
    {
        public PairShowsPoints(Suit? suit, int min, int max) : base(suit, min, max) { }

        void IShowsState.ShowState(Bid bid, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
        {
            (int Min, int Max) partnerPoints = ps.Partner.PublicHandSummary.GetStartingPoints();
            showHand.ShowStartingPoints(Math.Max(_min - partnerPoints.Min, 0), Math.Max(_max - partnerPoints.Min, 0)); 
        }
    }
}
