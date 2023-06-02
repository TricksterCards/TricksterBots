
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class StandardAmericanOvercallAdvance : Natural
    {
		public static new PrescribedBids DefaultBidderXXX()
		{
			var bidder = new StandardAmericanOvercallAdvance();
			return new PrescribedBids(bidder, bidder.Initiate);
		}
		private void Initiate(PrescribedBids pb)
        {
            pb.Bids = new BidRule[]
            {
                Nonforcing(Call.Pass, DefaultPriority - 100, Points(LessThanOvercall)),

                Nonforcing(1, Suit.Diamonds, Points(Overcall1Level), Shape(6, 11)),
                Nonforcing(1, Suit.Hearts, Points(Overcall1Level), Shape(6, 11)),
                Nonforcing(1, Suit.Spades, Points(Overcall1Level), Shape(6, 11)),

                // TODO: May want to consider more rules for 1-level overcall.  If you have 10 points an a crummy suit for example...
                Nonforcing(1, Suit.Diamonds, Points(Overcall1Level), Shape(5), GoodSuit()),
                Nonforcing(1, Suit.Hearts, Points(Overcall1Level), Shape(5), GoodSuit()),
                Nonforcing(1, Suit.Spades, Points(Overcall1Level), Shape(5), GoodSuit()),


                // TODO: NT Overcall needs to have suit stopped...

                Nonforcing(2, Suit.Clubs, CueBid(false), Points(OvercallStrong2Level), Shape(5, 11)),

                Nonforcing(2, Suit.Diamonds, Jump(0), CueBid(false), Points(OvercallStrong2Level), Shape(5, 11)),
                Nonforcing(2, Suit.Diamonds, Jump(1), CueBid(false), Points(OvercallWeak2Level), Shape(6), GoodSuit()),

                Nonforcing(2, Suit.Hearts, Jump(0), CueBid(false), Points(OvercallStrong2Level), Shape(5, 11)),
                Nonforcing(2, Suit.Hearts, Jump(1), CueBid(false), Points(OvercallWeak2Level), Shape(6), GoodSuit()),

                Nonforcing(2, Suit.Spades, Jump(0), CueBid(false), Points(OvercallStrong2Level), Shape(5, 11)),
                Nonforcing(2, Suit.Spades, Jump(1), CueBid(false), Points(OvercallWeak2Level), Shape(6), GoodSuit()),

                Nonforcing(3, Suit.Clubs, Jump(1), CueBid(false), Points(OvercallWeak3Level), Shape(7), DecentSuit()),
				Nonforcing(3, Suit.Diamonds, Jump(1, 2), CueBid(false), Points(OvercallWeak3Level), Shape(7), DecentSuit()),
				Nonforcing(3, Suit.Hearts, Jump(1, 2), CueBid(false), Points(OvercallWeak3Level), Shape(7), DecentSuit()),
				Nonforcing(3, Suit.Spades, Jump(1, 2), CueBid(false), Points(OvercallWeak3Level), Shape(7), DecentSuit()),


			};
            pb.Partner(Advance);
        }


        private void Advance(PrescribedBids pb)
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

                // TODO: Should these be prioirty - 5 - support should be higher priorty.  Seems reasonable
                Nonforcing(2, Suit.Clubs, Points(AdvanceNewSuit2Level), Shape(5), GoodSuit()),
                Nonforcing(2, Suit.Clubs, Points(AdvanceNewSuit2Level), Shape(6, 11)),
                Nonforcing(2, Suit.Diamonds, Points(AdvanceNewSuit2Level), Shape(5), GoodSuit()),
                Nonforcing(2, Suit.Diamonds, Points(AdvanceNewSuit2Level), Shape(6, 11)),
                Nonforcing(2, Suit.Hearts, Jump(0), Points(AdvanceNewSuit2Level), Shape(5), GoodSuit()),
                Nonforcing(2, Suit.Hearts, Jump(0), Points(AdvanceNewSuit2Level), Shape(6, 11)),
                Nonforcing(2, Suit.Spades, Jump(0), Points(AdvanceNewSuit2Level), Shape(5), GoodSuit()),
                Nonforcing(2, Suit.Spades, Jump(0), Points(AdvanceNewSuit2Level), Shape(6, 11)),




                Nonforcing(2, Suit.Diamonds, Partner(HasMinShape(5)), Fit(), DummyPoints(AdvanceRaise), ShowsTrump()),
                Nonforcing(2, Suit.Hearts, Partner(HasMinShape(5)), Fit(), DummyPoints(AdvanceRaise), ShowsTrump()),
                Nonforcing(2, Suit.Spades, Partner(HasMinShape(5)), Fit(), DummyPoints(AdvanceRaise), ShowsTrump()),

                // Fill this out better but for now just go on law of total trump, jumping if weak
                Nonforcing(3, Suit.Clubs, Jump(1), Fit(9), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                Nonforcing(3, Suit.Diamonds, Jump(1), Fit(9), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                Nonforcing(3, Suit.Hearts, Jump(1), Fit(9), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                Nonforcing(3, Suit.Spades, Jump(1), Fit(9), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),

                Nonforcing(4, Suit.Clubs, DefaultPriority + 100, Jump(1, 2), Fit(10), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                Nonforcing(4, Suit.Diamonds, DefaultPriority + 100, Jump(1, 2), Fit(10), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                Nonforcing(4, Suit.Hearts, DefaultPriority + 100, Jump(1, 2), Fit(10), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                Nonforcing(4, Suit.Spades, DefaultPriority + 100, Jump(1, 2), Fit(10), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),

            };

            pb.Partner(OvercallRebid);
        }

        private void OvercallRebid(PrescribedBids pb)
        {
            pb.Bids = new BidRule[]
            {
                // TODO: NEED TO FORMALIZE THE POINT RANGES... FOR NOW JUST LOOK AT 3-LEVEL BIDS
                Nonforcing(3, Suit.Clubs, Fit(), PairPoints((24, 25)), ShowsTrump()),
                Nonforcing(3, Suit.Diamonds, Fit(), PairPoints((24, 25)), ShowsTrump()),
                Nonforcing(3, Suit.Hearts, Fit(), PairPoints((24, 25)), ShowsTrump()),
                Nonforcing(3, Suit.Spades, Fit(), PairPoints((24, 25)), ShowsTrump()),

                Signoff(3, Suit.Unknown, DefaultPriority - 100, OppsStopped(), PairPoints((25, 30)) )

            };
            pb.Partner(AdvancerRebid);

        }


        private void AdvancerRebid(PrescribedBids pb)
        {

            // TODO: Need to do more than this, but for now this seems reasonable.  
            pb.Bids = new BidRule[]
            {
                // TODO: ONly bid these if they are necessary.  Minors don't need to go the 4-level unless forced there...
                Signoff(4, Suit.Clubs, Fit(), PairPoints((26, 28)), ShowsTrump()),
                Signoff(4, Suit.Diamonds, Fit(), PairPoints((26, 28)), ShowsTrump()),
                Signoff(4, Suit.Hearts, Fit(), PairPoints((26, 31)), ShowsTrump()),
                Signoff(4, Suit.Spades, Fit(), PairPoints((26, 31)), ShowsTrump())
            };

        }
    
    }

}
