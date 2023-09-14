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


    public class HasPoints : DynamicConstraint
    {
        protected int _min;
        protected int _max;
        protected Suit? _trumpSuit;
        protected PointType _pointType;

        public enum PointType { HighCard, Starting, Suit }


        public HasPoints(Suit? trumpSuit, int min, int max, PointType pointType)
        {
            // TODO:  Completely broken. But OK for now.  Need to rethink initiaializer
            this._pointType = pointType;
            this._trumpSuit = trumpSuit;
            this._min = min;
            this._max = max;
        }

        // Returns the points for the hand, adjusted for dummy points if appropriate.
        protected (int, int) GetPoints(Call call, PositionState ps, HandSummary hs)
        {
            (int, int)? points = null;
            switch (_pointType)
            {
                case PointType.HighCard:
                    points = hs.HighCardPoints;
                    break;
                case PointType.Starting:
                    points = hs.StartingPoints;
                    break;
                case PointType.Suit:
                    if (GetSuit(_trumpSuit, call) is Suit suit)
                    {
                        if (ps.PairState.Agreements.Suits[suit].LongHand == ps)
                        {
                            points = hs.Suits[suit].LongHandPoints;
                        }
                        else if (ps.PairState.Agreements.Suits[suit].Dummy == ps)
                        {
                            points = hs.Suits[suit].DummyPoints;
                        }
                    }
                    break;
            }
            if (points == null)
            {
                points = hs.Points;
            }
            return points == null ? (0, 100) : ((int, int))points;
        }


        public override bool Conforms(Call call, PositionState ps, HandSummary hs)
        {
            (int Min, int Max) points = GetPoints(call, ps, hs);
            return (_min <= points.Max && _max >= points.Min);
        }


    }

    class ShowsPoints : HasPoints, IShowsState
    {
        public ShowsPoints(Suit? trumpSuit, int min, int max, PointType pointType) : base(trumpSuit, min, max, pointType) { }


        void IShowsState.ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
        {
            switch (_pointType)
            {
                case PointType.HighCard:
                    showHand.ShowHighCardPoints(_min, _max);
                    break;
                case PointType.Starting:
                    showHand.ShowStartingPoints(_min, _max);
                    break;
                case PointType.Suit:
                    if (GetSuit(_trumpSuit, call) is Suit suit)
                    {
                        if (ps.PairState.Agreements.Suits[suit].LongHand == ps)
                        {
                            showHand.Suits[suit].ShowLongHandPoints(_min, _max);

                        }
                        else if (ps.PairState.Agreements.Suits[suit].Dummy == ps)
                        {
                            showHand.Suits[suit].ShowDummyPoints(_min, _max);
                        }
                        else
                        {
                            showHand.ShowStartingPoints(_min, _max);
                        }
                    }
                    else
                    {
                        Debug.Fail("Need to specify a suit.");
                    }
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }
    }

}
