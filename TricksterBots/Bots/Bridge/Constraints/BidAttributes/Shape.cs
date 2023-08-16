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

        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {
            if (GetSuit(_suit, call) is Suit suit)
            {
                (int Min, int Max) shape = hs.Suits[suit].GetShape();
                return (shape.Max >= _min && shape.Min <= _max);
            }
            Debug.Fail("No suit specified in call or constraint declaration");
            return false;
		}

    }

	public class ShowsShape : HasShape, IShowsState
	{
        public ShowsShape(Suit? suit, int min, int max) : base(suit, min, max) { }

	    void IShowsState.ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showArgeements)
		{
            if (GetSuit(_suit, call) is Suit suit)
            {
                showHand.Suits[suit].ShowShape(_min, _max);
            }
		}

    }



    public class HasMinShape : Constraint
    {
        protected Suit? _suit;
        protected int _min;
        public HasMinShape(Suit? suit, int min)
        {
            this._suit = suit;
            this._min = min;
        }
        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {
            if (GetSuit(_suit, call) is Suit suit)
            {
                return hs.Suits[suit].GetShape().Min >= _min;
            }
            Debug.Fail("No suit specified in call or constraint");
            return false;
        }
    }

}
