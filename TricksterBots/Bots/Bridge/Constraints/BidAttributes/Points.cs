using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{


    public class HasPoints : Constraint
    {
        protected int _min;
        protected int _max;
        protected Suit? _trumpSuit;
        protected bool _countAsDummy;



        public HasPoints(Suit? trumpSuit, int min, int max)
        {
            // TODO:  Completely broken. But OK for now.  Need to rethink initiaializer
            this._countAsDummy = false;
            this._trumpSuit = trumpSuit;
            this._min = min;
            this._max = max;
        }

        // Returns the points for the hand, adjusted for dummy points if appropriate.
        protected (int, int) GetPoints(Bid bid, HandSummary handSummary)
        {
            if (!_countAsDummy) { return handSummary.OpeningPoints; }
            return handSummary.Suits[bid.SuitIfNot(_trumpSuit)].DummyPoints;
        }


        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs, BiddingSummary bs)
        {
            (int Min, int Max) points = GetPoints(bid, hs);
            return (_min <= points.Max && _max >= points.Min);
        }
    }

    class ShowsPoints : HasPoints, IShowsState
    {
		public ShowsPoints(Suit? trumpSuit, int min, int max) : base(trumpSuit, min, max) { }

		void IShowsState.Update(Bid bid, PositionState ps, HandSummary hs, BiddingSummary bs)
		{
			if (_countAsDummy)
			{
				hs.Suits[bid.SuitIfNot(_trumpSuit)].DummyPoints = (_min, _max);
			}
			else
			{
				hs.OpeningPoints = (_min, _max);
			}
		}
	}

    /*
    public class PairPoints : Points, IHandConstraint, IPublicConstraint, IShowsState
    {
        public PairPoints(int min, int max) : base(min, max) { }
        public PairPoints(Suit? trumpSuit, int min, int max) : base(trumpSuit, min, max) { }

		bool IHandConstraint.Conforms(Bid bid, HandSummary handSummary, PositionState positionState)
		{
            (int min, int max) partnerPoints = positionState.Partner.ShownState.Points;
            int points = GetPoints(bid, handSummary);
            int pairMin = points + partnerPoints.min;
            int pairMax = pairMin + _max - _min;
            return pairMin >= this._min && pairMax <= this._max;
        }

        bool IPublicConstraint.Conforms(Bid bid, PositionState positionState)
        {
            (int min, int max) ourPoints = positionState.ShownState.Points;
            (int min, int max) partnerPoints = positionState.Partner.ShownState.Points;
            int min = ourPoints.min + partnerPoints.min;
            int max = min + _max - _min;
            return (_min <= min && _max >= max);
        }

        // TODO: Really think through what max and min means here WRT partner's max and min....
        // If partner has shown 15-17 points and we need 25-28 for a game, then if we conform that means
        // that we must have at least 10 points (15+10), but I also think this means that we would max
        // out at 13 points since 15+13=28 which is the max for the pair...   I think min is the critical
        // basis for everything, with the _max - _min giving us the range for the min->max known...

        public override void UpdateShownState(Bid bid, Direction direction, BiddingSummary biddingSummary, ShownState shownState)
        {
            (int min, int max) partnerPoints = biddingSummary.Positions[direction].Partner.ShownPoints;
            var min = Math.Min(0, _min - partnerPoints.min);
            var max = min + _max - _min;        // TODO: IS THIS RIGHT?  OR MAX+MAX?  NOT SURE.. THINK IT THORUGH
            shownState.ShowsPoints(min, max);
        }
    }
    */
}
