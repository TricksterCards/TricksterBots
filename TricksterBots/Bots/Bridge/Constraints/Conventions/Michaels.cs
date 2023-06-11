using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
   
    public class Michaels : Bidder
    {
 
        public PrescribedBids GetBids()
        {
            var pb = new PrescribedBids();
            // Do nothing if we are not overcaller
            pb.Redirect(null, Role(PositionRole.Overcaller, 1, false));
            // TODO: Do nothing if responder has acted.  Only overcall if there has been a single opening bid...
            pb.Bids = new List<BidRule>()
            {
                // TODO: Need some minimum points...
                Forcing(2, Suit.Clubs, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Spades, 5), ShowsSuits(Suit.Hearts, Suit.Spades)),
                Forcing(2, Suit.Diamonds, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Spades, 5), ShowsSuits(Suit.Hearts, Suit.Spades)),

                Forcing(2, Suit.Hearts, CueBid(), Shape(Suit.Spades, 5), Shape(Suit.Clubs, 5), ShowsSuits(Suit.Spades, Suit.Clubs)),
                Forcing(2, Suit.Hearts, CueBid(), Shape(Suit.Spades, 5), Shape(Suit.Diamonds, 5), ShowsSuits(Suit.Spades, Suit.Diamonds)),


                Forcing(2, Suit.Spades, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Clubs, 5), ShowsSuits(Suit.Hearts, Suit.Clubs)),
                Forcing(2, Suit.Spades, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Diamonds, 5), ShowsSuits(Suit.Hearts, Suit.Diamonds)),

            };
            pb.Partner(2, Suit.Clubs, RespondMajors);
            pb.Partner(2, Suit.Diamonds, RespondMajors);
            return pb;
        }

        private PrescribedBids RespondMajors()
        {
            var pb = new PrescribedBids();
            pb.Bids = new List<BidRule>
            {
                Signoff(2, Suit.Hearts, BetterThan(Suit.Spades), Points((0, 5))),
                Signoff(2, Suit.Spades, BetterOrEqualTo(Suit.Hearts), Points((0, 5))),
            };
            return pb;
        }
    }
}
