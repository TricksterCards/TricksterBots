using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public enum CallType { Pass, Bid, Double, Redouble }

    public struct Bid
    {
        public int? Level { get; }
        public Suit? Suit { get; }

        public CallType CallType { get; }

        public bool Is(int level, Suit suit)
        {
            return CallType == CallType.Bid && Level == level && Suit == suit; 
        }

        public bool IsBid
        {
            get { return CallType == CallType.Bid; }
        }
        public bool IsPass
        {
            get { return CallType == CallType.Pass; }
        }

        public Bid(CallType callType)
        {
            // TODO: ASSERT NOT TYPE == Bid
            this.CallType = callType;
            this.Level = null;
            this.Suit = null;
        }

        public Bid(int level, Suit suit)
        {
            this.CallType = CallType.Bid;
            // TODO: Assert level >=1 and <= 7
            this.Level = level;
            this.Suit = suit;
        }
    }

   

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
}
