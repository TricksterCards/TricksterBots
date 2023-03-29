using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{


    public class Points : HiddenConstraint
    {
        protected int _min;
        protected int _max;
        protected Suit? _trumpSuit;
        protected bool _countAsDummy;


        public Points(int min, int max)
        {
            this._min = min;
            this._max = max;
            this._trumpSuit = null;
            this._countAsDummy = false;
        }

        public Points(Suit? trumpSuit, int min, int max)
        {
            this._countAsDummy = true;
            this._trumpSuit = trumpSuit;
            this._min = min;
            this._max = max;
        }

        // Returns the points for the hand, adjusted for dummy points if appropriate.
        protected int GetPoints(Bid bid, HandSummary handSummary)
        {
            int points = handSummary.HighCardPoints;
            if (_countAsDummy)
            {
                Suit trumpSuit = _trumpSuit ?? (Suit)bid.Suit;
                points += BasicBidding.DummyPoints(handSummary.Hand, trumpSuit);
            }
            return points;
        }

        public override bool Conforms(Bid bid, Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
        {
            var points = GetPoints(bid, handSummary);
            return points >= _min && points <= _max;
        }

        public override bool CouldConform(Bid bid, Direction direction, BiddingSummary biddingSummary)
        {
            (int min, int max) points = biddingSummary.Positions[direction].ShownPoints;
            return (_min <= points.max && _max >= points.min);
        }

        public override void UpdateKnownState(Bid bid, Direction direction, BiddingSummary biddingSummary, KnownState knownState)
        {
            knownState.ShowsPoints(_min, _max);
        }
    }


    public class PairPoints : Points
    {
        public PairPoints(int min, int max) : base(min, max) { }
        public PairPoints(Suit? trumpSuit, int min, int max) : base(trumpSuit, min, max) { }

        public override bool Conforms(Bid bid, Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
        {
            (int min, int max) partnerPoints = biddingSummary.Positions[direction].Partner.ShownPoints;
            int points = GetPoints(bid, handSummary);
            int pairMin = points + partnerPoints.min;
            int pairMax = pairMin + _max - _min;
            return pairMin >= this._min && pairMax <= this._max;
        }

        public override bool CouldConform(Bid bid, Direction direction, BiddingSummary biddingSummary)
        {
            (int min, int max) ourPoints = biddingSummary.Positions[direction].ShownPoints;
            (int min, int max) partnerPoints = biddingSummary.Positions[direction].Partner.ShownPoints;
            int min = ourPoints.min + partnerPoints.min;
            int max = min + _max - _min;
            return (_min <= min && _max >= max);
        }

        // TODO: Really think through what max and min means here WRT partner's max and min....
        // If partner has shown 15-17 points and we need 25-28 for a game, then if we conform that means
        // that we must have at least 10 points (15+10), but I also think this means that we would max
        // out at 13 points since 15+13=28 which is the max for the pair...   I think min is the critical
        // basis for everything, with the _max - _min giving us the range for the min->max known...

        public override void UpdateKnownState(Bid bid, Direction direction, BiddingSummary biddingSummary, KnownState knownState)
        {
            (int min, int max) partnerPoints = biddingSummary.Positions[direction].Partner.ShownPoints;
            var min = Math.Min(0, _min - partnerPoints.min);
            var max = min + _max - _min;        // TODO: IS THIS RIGHT?  OR MAX+MAX?  NOT SURE.. THINK IT THORUGH
            knownState.ShowsPoints(min, max);
        }
    }


}
