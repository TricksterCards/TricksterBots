using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trickster.cloud;
/*
namespace TricksterBots.Bots.Bridge
{

 

    public class NaturalAdvance : Natural
    {

		public static new PrescribedBids DefaultBidderXXX()
        {
    		var bidder = new NaturalAdvance();
			return new PrescribedBids(bidder, bidder.Initiate);
		}


        private void Initiate(PrescribedBids pb)
        {
            pb.Bids = new BidRule[]
            {
                Nonforcing(Call.Pass, DefaultPriority - 100),   // TODO: What points?  What shape?

           
                Nonforcing(1, Suit.Hearts, DefaultPriority - 20, Points(AdvanceNewSuit1Level), Shape(5), GoodSuit()),
                Nonforcing(1, Suit.Hearts, DefaultPriority - 20, Points(AdvanceNewSuit1Level), Shape(6, 11)),

                Nonforcing(1, Suit.Spades, DefaultPriority - 20, Points(AdvanceNewSuit1Level), Shape(5), GoodSuit()),
                Nonforcing(1, Suit.Spades, DefaultPriority - 20, Points(AdvanceNewSuit1Level), Shape(6, 11)),

                // TODO: Need to have opps stopped?? What if not?  Then what?
                Nonforcing(1, Suit.Unknown, DefaultPriority - 10, Points(AdvanceTo1NT)),

                Nonforcing(2, Suit.Diamonds, Partner(HasShape(5, 11)), Shape(3, 8), DummyPoints(AdvanceRaise), ShowsTrump()),
                Nonforcing(2, Suit.Hearts, Partner(HasShape(5, 11)), Shape(3, 8), DummyPoints(AdvanceRaise), ShowsTrump()),
                Nonforcing(2, Suit.Spades, Partner(HasShape(5, 11)), Shape(3, 8), DummyPoints(AdvanceRaise), ShowsTrump()),

                // Fill this out better but for now just go on law of total trump, jumping if weak
                Nonforcing(3, Suit.Clubs, Jump(1), Fit(9), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                Nonforcing(3, Suit.Diamonds, Jump(1), Fit(9), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                Nonforcing(3, Suit.Hearts, Jump(1), Fit(9), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                Nonforcing(3, Suit.Spades, Jump(1), Fit(9), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),

                Nonforcing(4, Suit.Clubs, Jump(1, 2), Fit(10), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                Nonforcing(4, Suit.Diamonds, Jump(1, 2), Fit(10), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                Nonforcing(4, Suit.Hearts, Jump(1, 2), Fit(10), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                Nonforcing(4, Suit.Spades, Jump(1, 2), Fit(10), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),

            };
        }
    }

}
*/