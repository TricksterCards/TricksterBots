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



    public enum PositionRole { Opener, Overcaller, Responder, Advancer }



    /*
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
    */



}
