using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static (int, int) TakeoutRange = (11, 16);
        private static (int, int) StrongTakeout = (17, 40);



        public static IEnumerable<BidRule> InitiateConvention(PositionState ps)
        {
            var bids = new List<BidRule>();
            // TODO: Make this work off of the CONTRACT, not last bid but for now just use RHO...
            var rhoBid = ps.RightHandOpponent.LastBid;
            Debug.Assert(rhoBid.IsBid); // Should only get here if RHO opened
            if (rhoBid.Level == 1 && rhoBid.Suit != Suit.Unknown)
            {
                bids.AddRange(Takeout((Suit)rhoBid.Suit));
            }
            return bids;
        }


        private static BidRule[] Takeout(Suit suit)
        {
            // TODO: Should this be 2 or 1 for MinBidLevel?  Or is this really based on opponent bids?
            // TODO: Ugly way to avoid bidding this over NT...
            var rule = Forcing(Bid.Double, CueBid(suit), Points(TakeoutRange), Shape(suit, 0, 4), BidAvailable(2, Suit.Clubs));
            foreach (var otherSuit in BasicBidding.BasicSuits)
            {
                if (otherSuit != suit)
                {
                    // TODO: Is this reasonable?  6+ card suit needs to be bid.  Not takeout.   
                    rule.AddConstraint(Shape(otherSuit, 3, 4));
                }
            }
            return new BidRule[]
            {
                Forcing(Bid.Double, Points(StrongTakeout)),
                rule,
                DefaultPartnerBids(Bid.Pass, Respond)
            };
        }

        public static (int, int) MinLevel = (0, 8);
        public static (int, int) NoTrump1 = (6, 10);
        public static (int, int) NoTrump2 = (11, 12);
        public static (int, int) InviteLevel = (9, 11);
        public static (int, int) GameLevel = (12, 40);
        public static (int, int) Game3NT = (13, 40);

        private static IEnumerable<BidRule> Respond(PositionState ps)
        {
            return new List<BidRule>
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

        // TODO: Interference...
        /*
        private static PrescribedBids RespondWithInterference()
        {
            var pb = new PrescribedBids();
            pb.BidRules.Add(Signoff(Bid.Pass, new Constraint[0]));   // TODO: Do something here
            return pb;
        }
        */
    }
}
