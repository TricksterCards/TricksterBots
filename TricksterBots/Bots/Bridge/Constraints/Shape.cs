using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class Shape : HiddenConstraint
    {
        private Suit? _suit = null;
        private int _min;
        private int _max;

        private Suit GetSuit(Bid bid)
        {
            return _suit ?? (Suit)bid.Suit;
        }

        public Shape(int min, int max)
        {
            this._min = min;
            this._max = max;
        }
        public Shape(Suit suit, int min, int max) : this(min, max)
        {
            this._suit = suit;
        }

        public override bool Conforms(Bid bid, Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
        {
            int count = handSummary.Counts[GetSuit(bid)];
            return (count >= _min && count <= _max);

        }
        public override bool CouldConform(Bid bid, Direction direction, BiddingSummary biddingSummary)
        {
            SuitSummary suitSummary = biddingSummary.Positions[direction].Suits[GetSuit(bid)];
            if (suitSummary.Max < _min) { return false; }
            if (suitSummary.Min > _max) { return false; }
            return true;
        }
        public override void UpdateKnownState(Bid bid, Direction position, BiddingSummary biddingSummary, KnownState knownState)
        {
            knownState.ShowsShape(GetSuit(bid), _min, _max);
        }

    }


    public class Balanced : HiddenConstraint
    {
        private bool _desiredValue;
        public Balanced(bool desiredValue = true)
        {
            this._desiredValue = desiredValue;
        }
        public override bool Conforms(Bid bid, Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
        {
            return handSummary.IsBalanced == _desiredValue;
        }
        public override bool CouldConform(Bid bid, Direction direction, BiddingSummary biddingSummary)
        {
            // If we are have a desired value of False, that is, the rule needs a hand that is not
            // balanced, then we will aways just return true, since it's so unlikely that we will
            // ever know if the hand has to be balanced that we will just always return true to
            // indicate that it is possible that this hand is not balanced
            if (_desiredValue == false) { return true; }

            // This will check if it is POSSIBLE that the hand is balanced.  Not that it actaully is...
            int count2 = 0, count4 = 0, count5 = 0;
            foreach (Suit suit in BasicBidding.BasicSuits)
            {
                SuitSummary ss = biddingSummary.Positions[direction].Suits[suit];
                if (ss.Min > 5 || ss.Max < 2)
                {
                    return false;
                };
                if (ss.Min == 5) { count5++; }
                if (ss.Min == 4) { count4++; }
                if (ss.Max == 2) { count2++; }
            }
            // Can't have 2 5-card suits. Cant have two doubletons, and could not have a 5-card and 4-card
            // suit and still be balanced.
            return (count5 < 2 && (count5 + count4 < 2) && (count2 < 2));
        }

        public override void UpdateKnownState(Bid bid, Direction direction, BiddingSummary biddingSummary, KnownState knownState)
        {
            // TODO: Need to union suit knowledge, not just set it.  knownstate[pos].SetSuit(suit, (range))
            if (_desiredValue)
            {
                foreach (Suit suit in BasicBidding.BasicSuits)
                {
                    knownState.ShowsShape(suit, 2, 5);
                }
            }
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
            bool found4 = false;
            foreach (Suit suit in BasicBidding.BasicSuits)
            {
                SuitSummary ss = biddingSummary.Positions[direction].Suits[suit];
                if (ss.Min > 4 || ss.Max < 3) { return false; }
                if (ss.Min == 4)
                {
                    if (found4) { return false; }
                    found4 = true;
                }
            }
            return true;
        }

        public override void UpdateKnownState(Bid bid, Direction direction, BiddingSummary biddingSummary, KnownState knownState)
        {
            if (_desiredValue)
            {
                foreach (Suit suit in BasicBidding.BasicSuits)
                {
                    knownState.ShowsShape(suit, 3, 4);
                }
            }
        }
    }

}
