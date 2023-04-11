using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        public abstract void UpdateShownState(Bid bid, Direction direction, BiddingSummary biddingSummary, ShownState shownState);
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


	public class PreviousBid : Constraint
	{
		private Suit _suit;
		private int _level;
		private bool _desiredValue;

		public PreviousBid(Suit suit, bool desiredValue)
		{
			this._level = 0;
			this._suit = suit;
			this._desiredValue = desiredValue;
		}

		public PreviousBid(int level, Suit suit, bool desiredValue)
		{
			this._level = level;
			this._suit = suit;
			this._desiredValue = desiredValue;
		}

		public override bool Conforms(Bid bid, Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
		{
			var we = biddingSummary.Positions[direction];
			if (we.Bids.Count > 0)
			{
                var lastBid = we.Bids.Last();
				if (lastBid.CallType == CallType.Bid && lastBid.Suit == _suit &&
					(_level == 0 || _level == lastBid.Level))
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

  
    class BetterSuit : HiddenConstraint
    {
        private Suit? _better;
        private Suit? _worse;
        private Suit? _defaultIfEqual;
		private bool _lengthOnly;


		// TODO: Move this to BasicBidding after massive merge
		public Suit HigherRanking(Suit s1, Suit s2)
        {
            Debug.Assert(s1 != s2);
            Debug.Assert(s1 == Suit.Clubs || s1 == Suit.Diamonds || s1 == Suit.Hearts || s1 == Suit.Spades);
			Debug.Assert(s2 == Suit.Clubs || s2 == Suit.Diamonds || s2 == Suit.Hearts || s2 == Suit.Spades);
            switch (s1)
            {
                case Suit.Clubs:
                    return s2;
                case Suit.Diamonds:
                    return (s2 == Suit.Clubs) ? s1 : s2;
                case Suit.Hearts:
					return (s2 == Suit.Spades) ? s2 : s1;
                case Suit.Spades:
                    return s1;
			}
            throw new ArgumentException();  // TODO: Is this OK?  Is it right?
		}

        // Suit "better" must be better than suit "worse".  If lengthOnly is true then length is the only consideration
        // and the default value will be returned
		public BetterSuit(Suit? better, Suit? worse, Suit? defaultIfEqual = null, bool lengthOnly = false)
        {
            Debug.Assert(better != worse);
            Debug.Assert(defaultIfEqual == better || defaultIfEqual == worse);
            // TODO: More checks.  Should they be Assert or throw?
            this._better = better;
            this._worse = worse;
            this._defaultIfEqual = defaultIfEqual;
			this._lengthOnly = lengthOnly;
        }

        // TODO: This is repeated too often.  Maybe move to Constraint...
       

		public override bool Conforms(Bid bid, Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
		{
            var better = bid.SuitIfNot(_better);
            var worse = bid.SuitIfNot(_worse);
			if (handSummary.Counts[better] > handSummary.Counts[worse]) { return true; }
            if (handSummary.Counts[better] < handSummary.Counts[worse]) { return false; }
            if (!_lengthOnly)
            {
                int hcpBetter = BasicBidding.ComputeHighCardPoints(handSummary.Hand, better);
                int hcpWorse = BasicBidding.ComputeHighCardPoints(handSummary.Hand, better);
                if (hcpBetter != hcpWorse)
                {

                    return hcpBetter > hcpWorse;
                }
            }
			return (better == bid.SuitIfNot(_defaultIfEqual));
		}

		public override bool CouldConform(Bid bid, Direction direction, BiddingSummary biddingSummary)
		{
			var better = bid.SuitIfNot(_better);
			var worse = bid.SuitIfNot(_worse);
            var ps = biddingSummary.Positions[direction];
			return (ps.Suits[better].Max <= ps.Suits[worse].Min);
		}

		public override void UpdateShownState(Bid bid, Direction direction, BiddingSummary biddingSummary, ShownState shownState)
		{
			var better = bid.SuitIfNot(_better);
			var worse = bid.SuitIfNot(_worse);
			// The worse suit can not be longer than the better suit...
			var ps = biddingSummary.Positions[direction];
            shownState.ShowsShape(worse, ps.Suits[worse].Min, Math.Min(ps.Suits[worse].Max, ps.Suits[better].Max));
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

        public override void UpdateShownState(Bid bid, Direction direction, BiddingSummary biddingSummary, ShownState shownState)
        {
            _c1.UpdateShownState(bid, direction, biddingSummary, shownState);
            _c2.UpdateShownState(bid, direction, biddingSummary, shownState);
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
        // This should probably go into HandEvaluation class...
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


