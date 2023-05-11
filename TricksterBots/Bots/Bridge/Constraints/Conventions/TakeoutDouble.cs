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

        private TakeoutDouble() : base(Convention.TakeoutDouble, 1000)
        {
        }

        private void Initiate(PrescribedBids pb)
        {
            pb.ConventionRules = new ConventionRule[]
            {
                // TODO: Need takeout doubles after first round, but for now this works...
                ConventionRule(Role(PositionRole.Responder, 1))
            };
            pb.Bids = new BidRule[]
            {
                Takeout(Suit.Clubs),
                Takeout(Suit.Diamonds),
                Takeout(Suit.Hearts),
                Takeout(Suit.Spades)
            };
            pb.PartnerRules = Respond;
        }


        private BidRule Takeout(Suit suit)
        {
            var rule = Forcing(Call.Double, CueBid(suit), Points(TakeoutRange), Shape(suit, 0, 4));
            foreach (var otherSuit in BasicBidding.BasicSuits)
            {
                if (otherSuit != suit)
                {
                    // TODO: Is this reasonable?  6+ card suit needs to be bid.  Not takeout.   
                    rule.AddConstraint(Shape(otherSuit, 3, 5));
                }
            }
            return rule;
        }

        public static (int, int) MinLevel = (0, 8);
        public static (int, int) NoTrump1 = (6, 10);
        public static (int, int) InviteLevel = (9, 11);
        public static (int, int) GameLevel = (12, 40);

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
                // TODO: Should this be BestSuit()...
                Nonforcing(1, Suit.Diamonds, BestSuit(), Points(MinLevel)),
                Nonforcing(1, Suit.Hearts, BestSuit(), Points(MinLevel)),
                Nonforcing(1, Suit.Spades, BestSuit(), Points(MinLevel)),
           // TODO - ADD OPPS STOPPED     Nonforcing(1, Suit.Unknown, Balanced(), Stopped(OPPS), Points(NoTrump1)),
                Nonforcing(2, Suit.Clubs, BestSuit(), CueBid(false), Points(MinLevel)),
                Nonforcing(2, Suit.Diamonds, BestSuit(), Jump(0), CueBid(false), Points(MinLevel)),
                Nonforcing(2, Suit.Diamonds, BestSuit(), Jump(1), CueBid(false), Points(InviteLevel)),
                Nonforcing(2, Suit.Hearts, BestSuit(), Jump(0), CueBid(false), Points(MinLevel)),
                Nonforcing(2, Suit.Hearts, BestSuit(), Jump(1), CueBid(false), Points(InviteLevel)),
                Nonforcing(2, Suit.Spades, BestSuit(), Jump(0), CueBid(false), Points(MinLevel)),
                Nonforcing(2, Suit.Spades, BestSuit(), Jump(1), CueBid(false), Points(InviteLevel))

            };
        }
    }
}
