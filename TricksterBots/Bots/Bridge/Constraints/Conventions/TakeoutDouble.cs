using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class TakeoutDouble: Bidder
    {
        private static (int, int) TakeoutRange = (11, 17);

        public static PrescribedBids InitiateConvention()
        {
            var bidder = new TakeoutDouble();
            return new PrescribedBids(bidder, bidder.Initiate);
        }

        private TakeoutDouble() : base(Convention.TakeoutDouble, 100)
        {
        }

        private void Initiate(PrescribedBids pb)
        {
            pb.ConventionRules = new ConventionRule[]
            {
                // TODO: Need takeout doubles after first round, but for now this works...
                // This is ugly way to avoid bidding over 1NT too. Hack but works for now.  Don't really need to check
                // that 2C is available in subsueqnet rules, but whatever...  Hack works for now.
                ConventionRule(Role(PositionRole.Overcaller, 1), BidAvailable(1, Suit.Unknown))
            };
            pb.Bids = new BidRule[]
            {
                Takeout(Suit.Clubs),
                Takeout(Suit.Diamonds),
                Takeout(Suit.Hearts),
                Takeout(Suit.Spades)
            };
            pb.Partner(Respond);
        }


        private BidRule Takeout(Suit suit)
        {
            // TODO: Should this be 2 or 1 for MinBidLevel?  Or is this really based on opponent bids?
            // TODO: Ugly way to avoid bidding this over NT...
            var rule = Forcing(Call.Double, CueBid(suit), Points(TakeoutRange), Shape(suit, 0, 4), BidAvailable(2, Suit.Clubs));
            foreach (var otherSuit in BasicBidding.BasicSuits)
            {
                if (otherSuit != suit)
                {
                    // TODO: Is this reasonable?  6+ card suit needs to be bid.  Not takeout.   
                    rule.AddConstraint(Shape(otherSuit, 3, 4));
                }
            }
            return rule;
        }

        public static (int, int) MinLevel = (0, 8);
        public static (int, int) NoTrump1 = (6, 10);
        public static (int, int) NoTrump2 = (11, 12);
        public static (int, int) InviteLevel = (9, 11);
        public static (int, int) GameLevel = (12, 40);
        public static (int, int) Game3NT = (13, 40);

        private void Respond(PrescribedBids pb)
        { 
            pb.ConventionRules = new ConventionRule[]
            {
                // TODO: For now we will just do the takeout if RHO has passed...
                ConventionRule(RHO(Passed()))         
            };
            pb.Bids = new BidRule[]
            {
                // TODO: FOR NOW WE WILL JUST BID AT THE NEXT LEVEL REGARDLESS OF POINTS...
                // TODO: Need LongestSuit()...
                // TODO: Should this be TakeoutSuit()...
                Nonforcing(1, Suit.Diamonds, TakeoutSuit(), Points(MinLevel)),
                Nonforcing(1, Suit.Hearts, TakeoutSuit(), Points(MinLevel)),
                Nonforcing(1, Suit.Spades, TakeoutSuit(), Points(MinLevel)),


                Nonforcing(1, Suit.Unknown, Balanced(), OppsStopped(), Points(NoTrump1)),

                Nonforcing(2, Suit.Clubs, TakeoutSuit(), CueBid(false), Points(MinLevel)),
                Nonforcing(2, Suit.Diamonds, TakeoutSuit(), Jump(0), CueBid(false), Points(MinLevel)),
                Nonforcing(2, Suit.Diamonds, TakeoutSuit(), Jump(1), CueBid(false), Points(InviteLevel)),
                Nonforcing(2, Suit.Hearts, TakeoutSuit(), Jump(0), CueBid(false), Points(MinLevel)),
                Nonforcing(2, Suit.Hearts, TakeoutSuit(), Jump(1), CueBid(false), Points(InviteLevel)),
                Nonforcing(2, Suit.Spades, TakeoutSuit(), Jump(0), CueBid(false), Points(MinLevel)),
                Nonforcing(2, Suit.Spades, TakeoutSuit(), Jump(1), CueBid(false), Points(InviteLevel)),


                Nonforcing(2, Suit.Unknown, Balanced(), OppsStopped(), Points(NoTrump2)),



               // Signoff(3, Suit.Unknown, )

            };
        }
    }
}
