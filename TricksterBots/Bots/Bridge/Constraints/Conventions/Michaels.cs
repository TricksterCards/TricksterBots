using System;
using System.Collections.Generic;

namespace BridgeBidding
{
   
    public class Michaels : Bidder
    {
 
        public static BidRule[] InitiateConvention(PositionState ps)
        {
            return new BidRule[]
             {
                // TODO: Need some minimum points...
                PartnerBids(2, Strain.Clubs, Bid.Pass, RespondMajors),
                Forcing(2, Strain.Clubs, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Spades, 5), ShowsSuits(Suit.Hearts, Suit.Spades)),

                PartnerBids(2, Strain.Diamonds, Bid.Pass, RespondMajors),
                Forcing(2, Strain.Diamonds, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Spades, 5), ShowsSuits(Suit.Hearts, Suit.Spades)),

                PartnerBids(2, Strain.Hearts, Bid.Pass, (PositionState _) => { return ResopondMajorMinor(Suit.Spades); }),
                Forcing(2, Strain.Hearts, CueBid(), Shape(Suit.Spades, 5), Shape(Suit.Clubs, 5), ShowsSuits(Suit.Spades, Suit.Clubs)),
                Forcing(2, Strain.Hearts, CueBid(), Shape(Suit.Spades, 5), Shape(Suit.Diamonds, 5), ShowsSuits(Suit.Spades, Suit.Diamonds)),

                PartnerBids(2, Strain.Spades, Bid.Pass, (PositionState _) => { return ResopondMajorMinor(Suit.Hearts); }),
                Forcing(2, Strain.Spades, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Clubs, 5), ShowsSuits(Suit.Hearts, Suit.Clubs)),
                Forcing(2, Strain.Spades, CueBid(), Shape(Suit.Hearts, 5), Shape(Suit.Diamonds, 5), ShowsSuits(Suit.Hearts, Suit.Diamonds)),

             };
         }

        private static BidRule[] RespondMajors(PositionState _)
        {
            return new BidRule[]
            {
                Signoff(2, Strain.Hearts, BetterThan(Suit.Spades), Points((0, 5))),
                Signoff(2, Strain.Spades, BetterOrEqualTo(Suit.Hearts), Points((0, 5))),
            };
        }

        private static BidRule[] ResopondMajorMinor(Suit majorSuit)
        {
            // TODO: Do something here ...
            return new BidRule[0];
        }
    }
}
