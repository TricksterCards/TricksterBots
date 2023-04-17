using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class HasShape : Constraint
    {
        protected Suit? _suit = null;
        protected int _min;
        protected int _max;



        public HasShape(Suit? suit, int min, int max) : this(min, max)
        {
            Debug.Assert(min <= max && min >= 0 && max <= 13);
            this._suit = suit;
            this._min = min;
            this._max = max;
        }

        public override bool Conforms(Bid bid, Bridge.HandSummary handSummary, PositionState positionState)
        {
            (int Min, int Max) shape = handSummary.Suits[bid.SuitIfNot(_suit)].Shape;
			return (shape.Max >= _min && shape.Min <= _max);
		}

    }

	public class ShowsShape : HasShape, IShowsState
	{
        public ShowsShape(Suit? suit, int min, int max) : base(suit, min, max) { }

		public void UpdateState(Bid bid, ModifiableHandSummary handSummary, ModifiablePositionState positionState)
		{
            handSummary.ModifiableSuits[bid.SuitIfNot(_suit)].ShowShape(_min, _max);
		}
	}

}
