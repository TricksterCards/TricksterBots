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
    public class Strong2Clubs : Bidder
    {

        protected static (int, int) StrongOpenRange = (22, 40);
        protected static (int, int) PositiveResponse = (8, 18);
        protected static (int, int) Waiting = (0, 18);
        protected static (int, int) Rebid2NT = (22, 24);



        public static IEnumerable<BidRule> Open(PositionState _)

        {
            return new BidRule[] {
                // TODO: Interference here...
                DefaultPartnerBids(Bid.Pass, Respond),
                Forcing(2, Suit.Clubs, Points(StrongOpenRange), ShowsNoSuit())
            };
    
        }

        private static IEnumerable<BidRule> Respond(PositionState _)
        {
            return new BidRule[] {
               // TODO: DefaultPartnerBids(TODO: NEED POSITIVE RESPONSES TO GO TO NEW STATE),
                Forcing(2, Suit.Hearts, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(2, Suit.Spades, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(2, Suit.Unknown, Points(PositiveResponse), Balanced()),
                Forcing(3, Suit.Clubs, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(3, Suit.Diamonds, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),

                PartnerBids(2, Suit.Diamonds, Bid.Pass, OpenerRebid), 
                // TODO: Interference...
                Forcing(2, Suit.Diamonds, Points(Waiting), ShowsNoSuit()),

            };
        }

        private static IEnumerable<BidRule> OpenerRebid(PositionState _)
        {
            return new BidRule[]
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

