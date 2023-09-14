﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class PairHasMinShape : DynamicConstraint
    {
        protected Suit? _suit;
        protected int _min;
        bool _desiredValue;
        public PairHasMinShape(Suit? suit, int min, bool desiredValue)
        {
            this._suit = suit;
            this._min = min;
            this._desiredValue = desiredValue;
        }

        // When do we conform? When our maxiumu length + partner's minimum are >= the desired min.
        // When this happens with the public summary it will often match.  When using the private 
        // hand summary it will be much more restricitive sinde Max= actual count of cards and if
        // partner has not shown any shape then it's just our shape that matter....
        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {
            if (GetSuit(_suit, call) is Suit suit)
            {
                (int Min, int Max) shape = hs.Suits[suit].GetShape();
                (int Min, int Max) partnerShape = ps.Partner.PublicHandSummary.Suits[suit].GetShape();
                return (shape.Max + partnerShape.Min >= _min) ? _desiredValue : !_desiredValue;
            }
            Debug.Fail("No suit specified for PairHasMinShape");
            return false;
        }
    }

    public class PairShowsMinShape : PairHasMinShape, IShowsState
    {
        public PairShowsMinShape(Suit? suit, int min, bool desiredValue) : base(suit, min, desiredValue) { }
        void IShowsState.ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
        {
            if (GetSuit(_suit, call) is Suit suit)
            {
                (int Min, int Max) shape = ps.PublicHandSummary.Suits[suit].GetShape();
                (int Min, int Max) partnerShape = ps.Partner.PublicHandSummary.Suits[suit].GetShape();
                // If we must have a minimum of _min cards then _min - partners.min must be our new minimum
                // shown.  
                int newMin = _min - partnerShape.Min;
                // Don't know exaclty what to do here if Min becomes > max
                // Will make sure range is always valid by taking max of shape.Max and newMin
                // Debug.Assert(newMin <= shape.Max);
                if (newMin > shape.Min)
                {
                    showHand.Suits[suit].ShowShape(newMin, Math.Max(newMin, shape.Max));
                }
            }
        }
    }
}
