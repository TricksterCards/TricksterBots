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
 
        public static BidRule[] InitiateConvention(PositionState ps)
        {
            return new BidRule[]
             {
                // TODO: Need some minimum points...
                PartnerBids(2, Suit.Clubs, Bid.Pass, RespondMajors),
                Forcing(2, Suit.Clubs, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Spades, 5), ShowsSuits(Suit.Hearts, Suit.Spades)),

                PartnerBids(2, Suit.Diamonds, Bid.Pass, RespondMajors),
                Forcing(2, Suit.Diamonds, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Spades, 5), ShowsSuits(Suit.Hearts, Suit.Spades)),

                PartnerBids(2, Suit.Hearts, Bid.Pass, (PositionState _) => { return ResopondMajorMinor(Suit.Spades); }),
                Forcing(2, Suit.Hearts, CueBid(), Shape(Suit.Spades, 5), Shape(Suit.Clubs, 5), ShowsSuits(Suit.Spades, Suit.Clubs)),
                Forcing(2, Suit.Hearts, CueBid(), Shape(Suit.Spades, 5), Shape(Suit.Diamonds, 5), ShowsSuits(Suit.Spades, Suit.Diamonds)),

                PartnerBids(2, Suit.Spades, Bid.Pass, (PositionState _) => { return ResopondMajorMinor(Suit.Hearts); }),
                Forcing(2, Suit.Spades, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Clubs, 5), ShowsSuits(Suit.Hearts, Suit.Clubs)),
                Forcing(2, Suit.Spades, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Diamonds, 5), ShowsSuits(Suit.Hearts, Suit.Diamonds)),

             };
         }

        private static BidRule[] RespondMajors(PositionState _)
        {
            return new BidRule[]
            {
                Signoff(2, Suit.Hearts, BetterThan(Suit.Spades), Points((0, 5))),
                Signoff(2, Suit.Spades, BetterOrEqualTo(Suit.Hearts), Points((0, 5))),
            };
        }

        private static BidRule[] ResopondMajorMinor(Suit majorSuit)
        {
            // TODO: Do something here ...
            return new BidRule[0];
        }
    }
}
