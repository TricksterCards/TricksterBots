using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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



        public HasShape(Suit? suit, int min, int max) 
        {
            Debug.Assert(min <= max && min >= 0 && max <= 13);
            this._suit = suit;
            this._min = min;
            this._max = max;
        }

        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
        {
            (int Min, int Max) shape = hs.Suits[bid.SuitIfNot(_suit)].GetShape();
			return (shape.Max >= _min && shape.Min <= _max);
		}

    }

	public class ShowsShape : HasShape, IShowsState
	{
        public ShowsShape(Suit? suit, int min, int max) : base(suit, min, max) { }

	    void IShowsState.ShowState(Bid bid, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showArgeements)
		{
            showHand.Suits[bid.SuitIfNot(_suit)].ShowShape(_min, _max);
		}

    }



    public class HasMinShape: Constraint
    {
        protected Suit? _suit;
        protected int _min;
        public HasMinShape(Suit? suit, int min)
        {
            this._suit = suit;
            this._min = min;
        }
        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
        {
            return hs.Suits[bid.SuitIfNot(_suit)].GetShape().Min >= _min;
        }
    }

}
