using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class PairHasPoints : Constraint // TODO: Needs to show state too.  But for now passive..
    {
        protected bool _useStartingPoints;
        protected Suit? _suit;
        protected int _min;
        protected int _max;
        public PairHasPoints(Suit? suit, int min, int max)
        {
            this._useStartingPoints = false;
            this._suit = suit;
            this._min = min;
            this._max = max;
            Debug.Assert(max >= min);
        }

        public PairHasPoints(int min, int max)
        {
            this._useStartingPoints = true;
            this._suit = null;
            this._min = min;
            this._max = max;    
        }


        protected (int Min, int Max) GetPoints(Bid bid, PositionState ps, HandSummary hs)
        {
            var points = hs.StartingPoints;
            if (!_useStartingPoints)
            {
                var suit = bid.SuitIfNot(_suit);
                if (ps.PairAgreements.Suits[suit].LongHand == ps)
                {
                    points = hs.Suits[suit].LongHandPoints;
                }
                else if (ps.PairAgreements.Suits[suit].Dummy == ps)
                {
                    points = hs.Suits[suit].DummyPoints;
                }
            }
            if (points == null)
            {
                points = hs.Points;
            }
            return (points == null) ? (0, 100) : ((int, int))points;
        }
        /*
        protected (int MinThis, int MaxThis, int MinPartner, int MaxPartner) GetPoints(Bid bid, PositionState ps, HandSummary hs)
        {
            // Assume we will have to use starting points.  Override if appropriate
            var thisPoints = hs.GetStartingPoints();
            var partnerPoints = ps.Partner.PublicHandSummary.GetStartingPoints();
            if (!this._useStartingPoints)
            {
                var suit = bid.SuitIfNot(_suit);
                if (ps.PairAgreements.Suits[suit].LongHand != null)
                {
                    (int Min, int Max)? suitThisPoints = null;
                    (int Min, int Max)? suitPartnerPoints = null;
                    if (ps.PairAgreements.Suits[suit].LongHand == ps)
                    {
                        suitThisPoints = hs.Suits[suit].LongHandPoints;
                        suitPartnerPoints = ps.Partner.PublicHandSummary.Suits[suit].DummyPoints;
                    }
                    else
                    {
                        suitThisPoints = hs.Suits[suit].DummyPoints;
                        suitPartnerPoints = ps.PublicHandSummary.Suits[suit].LongHandPoints;
                    }
                    if (suitThisPoints != null) { thisPoints = ((int, int))suitThisPoints; }
                    if (suitPartnerPoints != null) { partnerPoints = ((int, int))suitPartnerPoints; }
                }
            }
            return (thisPoints.Min, thisPoints.Max, partnerPoints.Min, partnerPoints.Max);
        }
        */
        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
        {
            var pointsThis = GetPoints(bid, ps, hs);
            var pointsPartner = GetPoints(bid, ps.Partner, ps.Partner.PublicHandSummary);
            return (pointsThis.Max + pointsPartner.Min >= _min && pointsThis.Min + pointsPartner.Min <= _max);
        }
    }

    public class PairShowsPoints : PairHasPoints, IShowsState
    {
        public PairShowsPoints(Suit? suit, int min, int max) : base(suit, min, max) { }

        void IShowsState.ShowState(Bid bid, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
        {
            var pointsThis = GetPoints(bid, ps, ps.PublicHandSummary);
            var pointsPartner = GetPoints(bid, ps.Partner, ps.Partner.PublicHandSummary);
            var suit = bid.SuitIfNot(_suit);
            int showMin = Math.Max(_min - pointsPartner.Min, 0);
            int showMax = Math.Max(_max - pointsPartner.Min, 0);
            if (this._useStartingPoints || ps.PairAgreements.Suits[suit].LongHand == null)
            {
                showHand.ShowStartingPoints(showMin, showMax);
            }
            else if (ps.PairAgreements.Suits[suit].LongHand == ps)
            {
                showHand.Suits[suit].ShowLongHandPoints(showMin, showMax);
            }
            else
            {
                showHand.Suits[suit].ShowDummyPoints(showMin, showMax);
            }
        }
    }
}
