using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
   

    public class BiddingSummary
    {
        // TODO: Move this somewhere that makes sense...
        private Direction Next(Direction d)
        {
            switch (d)
            {
                case Direction.North: return Direction.East;
                case Direction.East: return Direction.South;
                case Direction.South: return Direction.West;
                case Direction.West: return Direction.North;
            }
            throw new InvalidEnumArgumentException(nameof(d));
        }
        public Direction Dealer { get; }
        public Dictionary<Direction, PositionSummary> Positions { get; }
        public BiddingSummary(Direction dealer, Dictionary<Direction, Hand> hands)
        { 
            this.Positions = new Dictionary<Direction, PositionSummary>();
            var direction = dealer;
            for  (int seat = 1; seat <= 4; seat++)
            {
                Positions[direction] = new PositionSummary(direction, seat, false, Vulnerability.None);
                direction = Next(direction);
            }
            Positions[Direction.North].Partner = Positions[Direction.South];
            Positions[Direction.South].Partner = Positions[Direction.North];
            Positions[Direction.East].Partner = Positions[Direction.West];
            Positions[Direction.West].Partner = Positions[Direction.East];
            Dealer = dealer;    

        }

        public BidXXX GetBidXXX(Direction direction)
        {
            return new BidXXX(PositionRole.Opener, 1, false, new Bid(CallType.NotActed));
        }

    }

    public struct BidXXX
    {
        public PositionRole Role;
        public int Round;
        public bool LhoInterferred;
        public Bid PartnersBid;

        public BidXXX(PositionRole role, int round, bool lhoInterferred, Bid partnersBid)
        {
            this.Role = role;
            this.Round = round;
            this.LhoInterferred = lhoInterferred;
            this.PartnersBid = partnersBid;
        }

    }

    public enum PositionRole { Opener, Overcaller, Responder, Advancer }

    public class SuitSummary
    {
        public int Min;
        public int Max;
    }


    public class PositionSummary
    {
        public List<Bid> Bids { get; }

        public PositionSummary Partner { get; internal set; }
        public PositionRole? Role { get; }

        public Dictionary<Suit, SuitSummary> Suits { get; }

        public Direction Direction { get; }

        public int Seat { get; }

        public bool Vulnerable { get; }

        public Vulnerability Vulnerability { get; }

        public (int, int) ShownPoints { get; }

        public PositionSummary(Direction direction, int seatNumber, bool vulnerable, Vulnerability vulnerability)
        {
            this.Direction = direction;
            this.Seat = seatNumber;
            this.Vulnerability = vulnerability;
            this.Vulnerable = vulnerable;
            this.ShownPoints = (0, int.MaxValue);
            this.Suits = new Dictionary<Suit, SuitSummary>();
            foreach (Suit s in BasicBidding.BasicSuits)
            {
                Suits[s] = new SuitSummary();
            }
            this.Partner = this;    // Initially set partner to ourselves then caller must set it correctly
            // TODO: Should this be a static function that returns a dictionary so that set can be private?
        }
    }


 

	public class ShownState
	{
		private int _pointsMin = 0;
		private int _pointsMax = int.MaxValue;
		private Dictionary<Suit, (int min, int max)> _suitShapes = new Dictionary<Suit, (int min, int max)>();
		public ShownState()
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

		internal void Union(ShownState other)
		{
			_pointsMin = Math.Min(_pointsMin, other._pointsMin);
			_pointsMax = Math.Max(_pointsMax, other._pointsMax);
			foreach (Suit suit in SuitRank.stdSuits)
			{
				(int min, int max) shapeThis = this._suitShapes.TryGetValue(suit, out shapeThis) ? shapeThis : (0, 13);
				(int min, int max) shapeOther = other._suitShapes.TryGetValue(suit, out shapeOther) ? shapeOther : (0, 13);
				shapeThis.min = Math.Min(shapeThis.min, shapeOther.min);
				shapeThis.max = Math.Max(shapeThis.max, shapeOther.max);
				this._suitShapes[suit] = shapeThis;
			}
		}

	}

}
