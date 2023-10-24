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
    // This class is used for PairHasShownPoints and PairShowsPoints.  HasShown is a static constraint, while
    // the Shows class is a dynamic constaint that also shows state.  This class implements all of the logic, while
    // the two constraint class simply implement the approriate Constraint methods and delegate to this class.
    public class PairPoints
    {
        protected bool _useStartingPoints;
        protected bool _useAgreedStrain;
        protected Suit? _suit;
        protected int _min;
        protected int _max;
        public PairPoints(Suit? suit, int min, int max)
        {
            this._useStartingPoints = false;
            this._useAgreedStrain = false;
            this._suit = suit;
            this._min = min;
            this._max = max;
            Debug.Assert(max >= min);
        }

        public PairPoints(int min, int max)
        {
            this._useStartingPoints = false;
            this._useAgreedStrain = true;
            this._suit = null;
            this._min = min;
            this._max = max;
        }

        public Suit? GetSuit(PositionState ps, Suit? suit, Call call)
        {
            if (_useAgreedStrain)
            {
                return ps.PairState.Agreements.TrumpSuit;
            }
            return Constraint.GetSuit(suit, call);
        }

        public (int Min, int Max) GetPoints(Call call, PositionState ps, HandSummary hs)
        {
            var points = hs.StartingPoints;
            if (!_useStartingPoints && GetSuit(ps, _suit, call) is Suit suit)
            {
                if (ps.PairState.Agreements.Strains[Call.SuitToStrain(suit)].LongHand == ps)
                {
                    points = hs.Suits[suit].LongHandPoints;
                }
                else if (ps.PairState.Agreements.Strains[Call.SuitToStrain(suit)].Dummy == ps)
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

        public bool DynamicallyConforms(Call call, PositionState ps, HandSummary hs)
        {
            var positionPoints = GetPoints(call, ps, hs);
            var pointsPartner = GetPoints(call, ps.Partner, ps.Partner.PublicHandSummary);
            return (positionPoints.Max + pointsPartner.Min >= _min && positionPoints.Min + pointsPartner.Min <= _max);
        }

        public bool StaticallyConforms(Call call, PositionState ps)
        {
            var positionPoints = GetPoints(call, ps, ps.PublicHandSummary);
            var pointsPartner = GetPoints(call, ps.Partner, ps.Partner.PublicHandSummary);
            var minPoints = positionPoints.Min + pointsPartner.Min;
            return (minPoints >= _min && minPoints <= _max);
        }

        public void ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
        {
            var pointsThis = GetPoints(call, ps, ps.PublicHandSummary);
            var pointsPartner = GetPoints(call, ps.Partner, ps.Partner.PublicHandSummary);
            var suit = Constraint.GetSuit(_suit, call);
            int showMin = Math.Max(_min - pointsPartner.Min, 0);
            int showMax = Math.Max(_max - pointsPartner.Min, 0);
            if (this._useStartingPoints || suit == null || ps.PairState.Agreements.Strains[Call.SuitToStrain(suit)].LongHand == null)
            {
                showHand.ShowStartingPoints(showMin, showMax);
            }
            else if (ps.PairState.Agreements.Strains[Call.SuitToStrain(suit)].LongHand == ps)
            {
                showHand.Suits[(Suit)suit].ShowLongHandPoints(showMin, showMax);
            }
            else
            {
                showHand.Suits[(Suit)suit].ShowDummyPoints(showMin, showMax);
            }
        }

        /*
        public (int Min, int Max) GetPoints(Call call, PositionState ps, HandSummary hs)
        {
			var positionPoints = GetPoints(call, ps, hs);
			var pointsPartner = GetPoints(call, ps.Partner, ps.Partner.PublicHandSummary);
            return (positionPoints.Min + pointsPartner.Min,  positionPoints.Max + pointsPartner.Max);
		}
        
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
    }

	public class PairHasShownPoints : StaticConstraint
    {
		private PairPoints _pairPoints;
		public PairHasShownPoints(Suit? suit, int min, int max)
		{
			this._pairPoints = new PairPoints(suit, min, max);
		}

		public PairHasShownPoints(int min, int max)
		{
			this._pairPoints = new PairPoints(min, max);
		}

		public override bool Conforms(Call call, PositionState ps)
		{
			return _pairPoints.StaticallyConforms(call, ps);
		}
	}

    public class PairShowsPoints : DynamicConstraint, IShowsState
    {
        private PairPoints _pairPoints;
        public PairShowsPoints(Suit? suit, int min, int max)
        {
            this._pairPoints = new PairPoints(suit, min, max);
        }

        public PairShowsPoints(int min, int max)
        {
            this._pairPoints = new PairPoints(min, max);
        }

        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {
            return _pairPoints.DynamicallyConforms(call, ps, hs);
        }

        void IShowsState.ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
        {
            _pairPoints.ShowState(call, ps, showHand, showAgreements);
        }
    }
}
