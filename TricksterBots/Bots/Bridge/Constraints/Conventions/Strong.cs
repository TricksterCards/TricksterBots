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



        public static PrescribedBids Open()

        {
            return new PrescribedBids(Respond,
                Forcing(2, Suit.Clubs, Points(StrongOpenRange), ShowsNoSuit())
            );
    
        }

        private static PrescribedBids Respond()
        {
            return new PrescribedBids(OpenerRebid, 
                // TODO: Priorities for the positive bids, especially if balanced AND have a good suit...
                Forcing(2, Suit.Diamonds, Points(Waiting), ShowsNoSuit()),
                Forcing(2, Suit.Hearts, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(2, Suit.Spades, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(2, Suit.Unknown, Points(PositiveResponse), Balanced()),
                Forcing(3, Suit.Clubs, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(3, Suit.Diamonds, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid))
            );
        }

        private static PrescribedBids OpenerRebid()
        {
            var pb = new PrescribedBids();
            pb.BidRules.AddRange(new BidRule[]
            {
                Forcing(2, Suit.Hearts, Shape(5, 11)),
                Forcing(2, Suit.Spades, Shape(5, 11)),
                Forcing(2, Suit.Unknown, Balanced(), Points(Rebid2NT)),
                Forcing(3, Suit.Clubs, Shape(5, 11)),
                Forcing(3, Suit.Diamonds, Shape(5, 11))
            });
            return pb;
            // TODO: Next state, more bids, et.....
        }
    }

}

