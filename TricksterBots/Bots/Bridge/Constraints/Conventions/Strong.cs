using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class StrongBidder : Bidder
    {
        public static PrescribedBids InitiateConvention()
        {
            var bidder = new StrongBidder();
            return new PrescribedBids(bidder, bidder.Initiate);
        }

        protected static (int, int) StrongOpenRange = (22, 40);
        protected static (int, int) PositiveResponse = (8, 18);
        protected static (int, int) Waiting = (0, 18);
        protected static (int, int) Rebid2NT = (22, 24);


        private StrongBidder() : base(Convention.StrongOpen, 5000) { }

        private void Initiate(PrescribedBids pb)

        {
            pb.ConventionRules = new ConventionRule[]
            {
                ConventionRule(Role(PositionRole.Opener, 1))
            };
            pb.Bids = new BidRule[]
            {
                Forcing(2, Suit.Clubs, Points(StrongOpenRange)),
            };
            pb.PartnerRules = Response;
        }
        private void Response(PrescribedBids pb)
        {
            pb.Bids = new BidRule[]
            {
                // TODO: Priorities for the positive bids, especially if balanced AND have a good suit...
                Forcing(2, Suit.Diamonds, Points(Waiting)),
                Forcing(2, Suit.Hearts, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(2, Suit.Spades, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(2, Suit.Unknown, Points(PositiveResponse), Balanced()),
                Forcing(3, Suit.Clubs, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(3, Suit.Diamonds, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
            };
            pb.PartnerRules = OpenerRebid;
        }

        private void OpenerRebid(PrescribedBids pb)
        {
            pb.Bids = new BidRule[]
            {
                Forcing(2, Suit.Hearts, Shape(5, 11)),
                Forcing(2, Suit.Spades, Shape(5, 11)),
                Forcing(2, Suit.Unknown, Balanced(), Points(Rebid2NT)),
                Forcing(3, Suit.Clubs, Shape(5, 11)),
                Forcing(3, Suit.Diamonds, Shape(5, 11))
            };
            // TODO: Next state, more bids, et.....
        }
    }

}

