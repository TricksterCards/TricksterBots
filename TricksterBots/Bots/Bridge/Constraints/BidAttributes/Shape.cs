using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class Shape : Constraint
    {
        protected Suit? _suit = null;
        protected int _min;
        protected int _max;


        public Shape(int min, int max)
        {
            this._min = min;
            this._max = max;
        }

        public Shape(Suit suit, int min, int max) : this(min, max)
        {
            this._suit = suit;
        }

        public override bool Conforms(Bid bid, Bridge.HandSummary handSummary, PositionState positionState)
        {
            (int Min, int Max) shape = handSummary.Suits[bid.SuitIfNot(_suit)].Shape;
			return (shape.Max >= _min && shape.Min <= _max);
		}

    }

	public class ShowsShape : Shape, IShowsState
	{
		public void UpdateState(Bid bid, ModifiableHandSummary handSummary, ModifiablePositionState positionState)
		{
            handSummary.ModifiableSuits[bid.SuitIfNot(_suit)].ShowShape(_min, _max);
		}
	}


	

    class Flat : HiddenConstraint
    {
        private bool _desiredValue;
        public Flat(bool desiredValue = true)
        {
            this._desiredValue = desiredValue;
        }

        public override bool Conforms(Bid bid, Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
        {
            return handSummary.Is4333 == _desiredValue;
        }

        public override bool CouldConform(Bid bid, Direction direction, BiddingSummary biddingSummary)
        {
            // If we are have a desired value of False, that is, the rule needs a hand that is not
            // balanced, then we will aways just return true, since it's so unlikely that we will
            // ever know if the hand has to be balanced that we will just always return true to
            // indicate that it is possible that this hand is not flat
            if (_desiredValue == false) { return true; }

            // This will check if it is POSSIBLE that the hand is flat.  Not that it actaully is...
            foreach (Suit suit in BasicBidding.BasicSuits)
            {
                SuitSummary ss = biddingSummary.Positions[direction].Suits[suit];
                if (ss.Min > 4 || ss.Max < 3) { return false; }
            }
            return true;
        }

        public override void UpdateShownState(Bid bid, Direction direction, BiddingSummary biddingSummary, ShownState shownState)
        {
            if (_desiredValue)
            {
                foreach (Suit suit in BasicBidding.BasicSuits)
                {
                    shownState.ShowsShape(suit, 3, 4);
                }
            }
        }
    }

}
