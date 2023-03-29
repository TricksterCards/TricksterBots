using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{




    public abstract class Constraint
    {
        public abstract bool Conforms(Bid bid, Direction direction, HandSummary handSummary, BiddingSummary biddingSummary);

    }

    public abstract class HiddenConstraint : Constraint
    {
        public abstract bool CouldConform(Bid bid, Direction direction, BiddingSummary biddingSummary);
        public abstract void UpdateKnownState(Bid bid, Direction direction, BiddingSummary biddingSummary, KnownState knownState);
    }



    public class BidRule
    {
        public Bid Bid { get; }

        private Constraint[] _contraints;
        public BidRule(int level, Suit suit, int priority, params Constraint[] constraints) 
        {
            this.Bid = new Bid(level, suit);
            this._contraints = constraints;
        }
        public BidRule(CallType callType, int priority, params Constraint[] constraints) 
        {
            this.Bid = new Bid(callType);
            this._contraints = constraints;
        }
        public bool Conforms(Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
        {
            foreach (Constraint constraint in _contraints)
            {
                if (!constraint.Conforms(Bid, direction, handSummary, biddingSummary)) { return false; }
            }
            return true;
        }
    }



    /*
    // TODO: Max priority??
    public class BidGroup
    {
        public Bid Bid { get;  }
        private List<BidRule> _rules = new List<BidRule>();

        public 

        public bool Conforms(Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
        {
            foreach (BidRule rule in _rules)
            {
                if (!rule.Conforms(direction, handSummary, biddingSummary)) { return false; }
            }
            return true;
        }

    }
    */


    /*
        public struct SuitInfo
        {
            public int Count;
            public int HighCardPoints;
            public SuitInfo(int count, int highCardPoints)
            {
                this.Count = count;
                this.HighCardPoints = highCardPoints;
            }
        }


        public class SeatInfo
        {
            public Hand Hand { get;  }
       //     public Dictionary<Suit, SuitInfo> Suits { get;  }
            public int SeatNumber { get; }

            public SeatInfo(Hand hand, int seatNumber)
            {
                this.Hand = hand;
                this.SeatNumber = seatNumber;
                this.Suits = new Dictionary<Suit, SuitInfo>();
                foreach (Suit suit in BasicBidding.BasicSuits)
                {
                    Suits[suit] = new SuitInfo(
                        hand.Count(c => c.suit == suit),
                        BasicBidding.ComputeHighCardPoints(hand, suit));
                }
            }
        }

        */

    public class HandSummary
    {
        public Hand Hand { get; }
        public Dictionary<Suit, int> Counts { get; }
        public int HighCardPoints { get; }
        public bool IsBalanced { get; }
        public bool Is4333 { get; }

        public HandSummary(Hand hand)
        {
            Hand = hand;
            Counts = BasicBidding.CountsBySuit(hand);
            HighCardPoints = BasicBidding.ComputeHighCardPoints(hand);
            IsBalanced = BasicBidding.IsBalanced(hand);
            Is4333 = BasicBidding.Is4333(Counts);
        }
    }



    public class KnownState
    {
        private int _pointsMin = 0;
        private int _pointsMax = int.MaxValue;
        private Dictionary<Suit, (int min, int max)> _suitShapes = new Dictionary<Suit, (int min, int max)>();
        public KnownState()
        {

        }

        public void ShowsPoints(int min, int max)
        {
            _pointsMin = Math.Max(min, _pointsMin);
            _pointsMax = Math.Min(max, _pointsMax);
            // TODO: Assert or throw if _pointsMin > _pointsMax...
        }

        public void ShowsShape(Suit suit, int min, int max)
        {
            (int min, int max) shape = _suitShapes.TryGetValue(suit, out shape) ? shape : (0, 13);
            shape.min = Math.Max(min, shape.min);
            shape.max = Math.Min(max, shape.max);
            _suitShapes[suit] = shape;
            // TODO: Throw if max<min...
        }

        internal void Union(KnownState other)
        {
            _pointsMin = Math.Min(_pointsMin, other._pointsMin);
            _pointsMax = Math.Max(_pointsMax, other._pointsMax);
            foreach (Suit suit in BasicBidding.BasicSuits)
            {
                (int min, int max) shapeThis = this._suitShapes.TryGetValue(suit, out shapeThis) ? shapeThis : (0, 13);
                (int min, int max) shapeOther = other._suitShapes.TryGetValue(suit, out shapeOther) ? shapeOther : (0, 13);
                shapeThis.min = Math.Min(shapeThis.min, shapeOther.min);
                shapeThis.max = Math.Max(shapeThis.max, shapeOther.max);
                this._suitShapes[suit] = shapeThis;
            }
        }

    }



    
    // TODO: Need lots more work.  Pass, Double, Redouble, etc...
    public class PartnerBid : Constraint
    {
        private Suit _suit;
        private int _level;
        private bool _desiredValue;

        public PartnerBid(Suit suit, bool desiredValue)
        {
            this._level = 0;
            this._suit = suit;
            this._desiredValue = desiredValue;
        }

        public PartnerBid(int level, Suit suit, bool desiredValue)
        {
            this._level = level;
            this._suit = suit;
            this._desiredValue = desiredValue;
        }

        public override bool Conforms(Bid bid, Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
        {
            var partner = biddingSummary.Positions[direction].Partner;
            if (partner.Bids.Count > 0)
            {
                var partnerBid = partner.Bids.Last();
                if (partnerBid.CallType == CallType.Bid && partnerBid.Suit == _suit &&
                    (_level == 0 || _level == partnerBid.Level))
                {
                    return _desiredValue;
                }
                

            }
            return !_desiredValue;

        }

    }


    public class Seat : Constraint
    {
        private int[] seats;
        public Seat(params int[] seats)
        {
            this.seats = seats;
        }

        public override bool Conforms(Bid bid, Direction position, HandSummary handSummary, BiddingSummary biddingSummary)
        {
            return seats.Contains(biddingSummary.Positions[position].Seat);
        }
    }




    // TODO: Could make this a more generic composite
    class CompositeConstraint : HiddenConstraint
    {
        private HiddenConstraint _c1;
        private HiddenConstraint _c2;
        public CompositeConstraint(HiddenConstraint c1, HiddenConstraint c2) 
        {
            this._c1 = c1;
            this._c2 = c2;
        }

        public override bool Conforms(Bid bid, Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
        {
            return _c1.Conforms(bid, direction, handSummary, biddingSummary) &&
                    _c2.Conforms(bid, direction, handSummary, biddingSummary);
        }

        public override bool CouldConform(Bid bid, Direction direction, BiddingSummary biddingSummary)
        {
            return _c1.CouldConform(bid, direction, biddingSummary) ||
                    _c2.CouldConform(bid, direction, biddingSummary);
        }

        public override void UpdateKnownState(Bid bid, Direction direction, BiddingSummary biddingSummary, KnownState knownState)
        {
            _c1.UpdateKnownState(bid, direction, biddingSummary, knownState);
            _c2.UpdateKnownState(bid, direction, biddingSummary, knownState);
        }
    }

    /*
    class BetterMinor : Constraint
    {
        public override bool Conforms(Bid bid, Direction direction, Hand hand, BiddingSummary biddingSummary)
        {
            var counts = BasicBidding.CountsBySuit(hand);
            if ((counts[Suit.Clubs] == 3 && counts[Suit.Diamonds] == 3) || (counts[Suit.Clubs] > counts[Suit.Diamonds]))
            {
                return bid.Suit == Suit.Clubs;
            }
            return bid.Suit == Suit.Diamonds;
        }
    }

    class LongerMajor : Constraint 
    {
        public override bool Conforms(Bid bid, Direction direction, Hand hand, BiddingSummary biddingSummary)
        {
            var counts = BasicBidding.CountsBySuit(hand);
            if ((counts[Suit.Spades] >= counts[Suit.Hearts]))
            {
                return bid.Suit == Suit.Spades;
            }
            return bid.Suit == Suit.Hearts;
        }
    }
    */
  
    public enum SuitQuality { Poor, Decent, Good, Excellent, Solid }


    class Quality : Constraint  // TODO: This may be HiddenConstraint if we expose the SuitQuality property in the bidstate
    {
        private Suit? _suit;
        private SuitQuality _suitQuality;

        public Quality(SuitQuality suitQuality)
        {
            this._suit = null;
            this._suitQuality = suitQuality;
        }

       public Quality(Suit suit, SuitQuality suitQuality)
        {
            this._suit = suit;
            this._suitQuality = suitQuality;
        }

        // TODO: This is NOT FINAL CODE.  Just a quick implementaiton.  Need more clear definitions of quality...
        public override bool Conforms(Bid bid, Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
        {
            Suit suit = (_suit == null) ? (Suit)bid.Suit : (Suit)_suit;
            var q = SuitQuality.Poor;
            switch (BasicBidding.ComputeHighCardPoints(handSummary.Hand, suit))
            {
                case 10:
                    q = SuitQuality.Solid; break;
                case 8:
                case 9:
                    q = SuitQuality.Excellent; break;
                case 4:
                case 5:
                case 6:
                case 7:
                    q = (BasicBidding.IsGoodSuit(handSummary.Hand, suit)) ? SuitQuality.Good : SuitQuality.Decent; break;
                default:
                    q = SuitQuality.Poor; break;
            }
            return q >= _suitQuality;
        }
    }

    /*

        public enum Convention
        {
            Stayman,
            JacobyTransfers,
            MichaelsCuebid,
            UnsualNoTrump
        }

        class NoTrumpResponse
        {
            static BidRule[] GenerateBids()
            {
                BidRule[] bids = {
                    new BidRule(2, Suit.Clubs, 100, new Points(PointRange.NTNoInvite), new Shape(Suit.Diamonds, 4, 6), new Shape(Suit.Hearts, 4, 4), new Shape(Suit.Spades, 4, 4)),
                    new BidRule(2, Suit.Clubs, 100, new Points(PointRange.NTInviteOrBetter), new Shape(Suit.Hearts, 4, 4), new Shape(Suit.Spades, 4, 5), new Flat(false)),
                    new BidRule(2, Suit.Clubs, 100, new Points(PointRange.NTInviteOrBetter), new Shape(Suit.Hearts, 0, 3), new Shape(Suit.Spades, 4, 4), new Flat(false)),
                    new BidRule(2, Suit.Diamonds, 100, new Shape(Suit.Hearts, 5)),
                    new BidRule(2, Suit.Hearts, 100, new Shape(Suit.Spades, 5)),
                    new BidRule(2, Suit.Spades, 100, new Points(PointRange.NTNoInvite), new Shape(Suit.Clubs, 6, 6), new Quality(SuitQuality.Decent, Suit.Clubs)),
                    new BidRule(2, Suit.Spades, 100, new Points(PointRange.NTNoInvite), new Shape(Suit.Clubs, 7)),
                    new BidRule(2, Suit.Spades, 100, new Points(PointRange.NTNoInvite), new Shape(Suit.Diamonds, 6, 6), new Quality(SuitQuality.Decent, Suit.Diamonds)),
                    new BidRule(2, Suit.Spades, 100, new Points(PointRange.NTNoInvite), new Shape(Suit.Diamonds, 7)),
                    new BidRule(2, Suit.Unknown, 100, new Points(PointRange.NTInvitational))
                };
                return bids;
            }
        }

        class StaymanResponse
        {
            static BidRule[] GenerateBids()
            {
                BidRule[] bids =
                {
                    new BidRule(2, Suit.Diamonds, 0, new Shape(Suit.Hearts, 0, 3), new Shape(Suit.Spades, 0, 3)),
                    new BidRule(2, Suit.Hearts, 0, new Shape(Suit.Hearts, 4)),
                    new BidRule(2, Suit.Spades, 0, new Shape(Suit.Hearts, 0, 3), new Shape(Suit.Spades, 4))
                };
                return bids;
            }
        }

        class TransferReponse
        {
            static BidRule[] GenerateBids()
            {
                BidRule[] bids =
                {
                    new BidRule(2, Suit.Hearts, 0, new PartnerBid(2, Suit.Diamonds)),
                    new BidRule(2, Suit.Spades, 0, new PartnerBid(2, Suit.Hearts)),
                    new BidRule(3, Suit.Hearts, 100, new PartnerBid(2, Suit.Diamonds), new Points(PointRange.MaxNTOpener), new Shape(4,5)),
                    new BidRule(3, Suit.Spades, 100, new PartnerBid(2, Suit.Hearts), new Points(PointRange.MaxNTOpener), new Shape(4,5))
                };
                return bids;
            }

        }
    */

}


