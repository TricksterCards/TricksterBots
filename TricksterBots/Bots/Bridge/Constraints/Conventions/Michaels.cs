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
        public Michaels(Convention convention, int defaultPriority) : base(convention, defaultPriority)
        {
        }

        void Bids(PrescribedBids pb)
        {
            pb.ConventionRules = new ConventionRule[]
            {
                ConventionRule(Role(PositionRole.Overcaller, 1))
            };
            pb.Bids = new List<BidRule>()
            {
                Forcing(2, Suit.Clubs, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Spades, 5), ShowsSuits(Suit.Hearts, Suit.Spades)),
                Forcing(2, Suit.Diamonds, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Spades, 5), ShowsSuits(Suit.Hearts, Suit.Spades)),

                Forcing(2, Suit.Hearts, CueBid(), Shape(Suit.Spades, 5), Shape(Suit.Clubs, 5), ShowsSuits(Suit.Spades, Suit.Clubs)),
                Forcing(2, Suit.Hearts, CueBid(), Shape(Suit.Spades, 5), Shape(Suit.Diamonds, 5), ShowsSuits(Suit.Spades, Suit.Diamonds)),


                Forcing(2, Suit.Spades, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Clubs, 5), ShowsSuits(Suit.Hearts, Suit.Clubs)),
                Forcing(2, Suit.Spades, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Diamonds, 5), ShowsSuits(Suit.Hearts, Suit.Diamonds)),

            };
            pb.Partner(Respond);
        }

        void Respond(PrescribedBids pb)
        {

        }
    }
}
