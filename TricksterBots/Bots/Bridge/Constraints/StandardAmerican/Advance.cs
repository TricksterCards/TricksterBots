using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class Advance : StandardAmerican
    {
        public static (int, int) AdvanceNewSuit1Level = (6, 40); // TODO: Highest level for this?
        public static (int, int) NewSuit2Level = (11, 40); // Same here...
        public static (int, int) AdvanceTo1NT = (6, 10);
        public static (int, int) WeakJumpRaise = (0, 8);   // TODO: What is the high end of jump raise weak
        public static (int, int) Raise = (6, 10);
        public static (int, int) AdvanceCuebid = (11, 40);


        public static IEnumerable<BidRule> FirstBid(PositionState ps)
        {
            if (ps.Partner.LastCall is Bid partnerBid)
            {
                // NOTE: We only shold get here when a suit has been bid.  NoTrump overcalls have different logic.
                Suit partnerSuit = (Suit)partnerBid.Suit;
                return new BidRule[] {
                    // TODO: What is the level of interference we can take
                    DefaultPartnerBids(new Bid(4, Suit.Unknown), Overcall.Rebid),

                                        // Weak jumps to game are highter priority than simple raises.
                    // Fill this out better but for now just go on law of total trump, jumping if weak.  
                    Nonforcing(4, Suit.Clubs, Jump(1, 2), Fit(10), DummyPoints(WeakJumpRaise), ShowsTrump()),
                    Nonforcing(4, Suit.Diamonds, Jump(1, 2, 3), Fit(10), DummyPoints(WeakJumpRaise), ShowsTrump()),
                    Nonforcing(4, Suit.Hearts, Jump(1, 2, 3), Fit(10), DummyPoints(WeakJumpRaise), ShowsTrump()),
                    Nonforcing(4, Suit.Spades, Break(false, "4SWeek"), Jump(1, 2, 3), Fit(10), DummyPoints(WeakJumpRaise), ShowsTrump()),


                    // If we have support for partner
                    Nonforcing(2, Suit.Diamonds,  Fit(), DummyPoints(Raise), ShowsTrump()),
                    Nonforcing(2, Suit.Hearts,    Fit(), DummyPoints(Raise), ShowsTrump()),
                    Nonforcing(2, Suit.Spades,    Fit(), DummyPoints(Raise), ShowsTrump()),


                    Nonforcing(1, Suit.Hearts, Points(AdvanceNewSuit1Level), Shape(5), GoodSuit()),
                    Nonforcing(1, Suit.Hearts, Points(AdvanceNewSuit1Level), Shape(6, 11)),

                    Nonforcing(1, Suit.Spades, Points(AdvanceNewSuit1Level), Shape(5), GoodSuit()),
                    Nonforcing(1, Suit.Spades, Points(AdvanceNewSuit1Level), Shape(6, 11)),

               
                    // TODO: Should these be prioirty - 5 - support should be higher priorty.  Seems reasonable
                    Nonforcing(2, Suit.Clubs, Points(NewSuit2Level), Shape(5), GoodSuit()),
                    Nonforcing(2, Suit.Clubs, Points(NewSuit2Level), Shape(6, 11)),
                    Nonforcing(2, Suit.Diamonds, Points(NewSuit2Level), Shape(5), GoodSuit()),
                    Nonforcing(2, Suit.Diamonds, Points(NewSuit2Level), Shape(6, 11)),
                    Nonforcing(2, Suit.Hearts, Jump(0), Points(NewSuit2Level), Shape(5), GoodSuit()),
                    Nonforcing(2, Suit.Hearts, Jump(0), Points(NewSuit2Level), Shape(6, 11)),
                    Nonforcing(2, Suit.Spades, Jump(0), Points(NewSuit2Level), Shape(5), GoodSuit()),
                    Nonforcing(2, Suit.Spades, Jump(0), Points(NewSuit2Level), Shape(6, 11)),



                    // TODO: Make a special BidRule here to handle rebid after cuebid...
                    Forcing(2, Suit.Clubs, CueBid(), Fit(partnerSuit), DummyPoints(AdvanceCuebid), ShowsTrump(partnerSuit)),
                    Forcing(2, Suit.Diamonds, CueBid(), Fit(partnerSuit), DummyPoints(AdvanceCuebid), ShowsTrump(partnerSuit)),
                    Forcing(2, Suit.Hearts, CueBid(), Fit(partnerSuit), DummyPoints(AdvanceCuebid), ShowsTrump(partnerSuit)),
                    Forcing(2, Suit.Spades, CueBid(), Fit(partnerSuit), DummyPoints(AdvanceCuebid), ShowsTrump(partnerSuit)),

 

                    Nonforcing(3, Suit.Clubs, Jump(1), Fit(9), DummyPoints(WeakJumpRaise), ShowsTrump()),
                    Nonforcing(3, Suit.Diamonds, Jump(1), Fit(9), DummyPoints(WeakJumpRaise), ShowsTrump()),
                    Nonforcing(3, Suit.Hearts, Jump(1), Fit(9), DummyPoints(WeakJumpRaise), ShowsTrump()),
                    Nonforcing(3, Suit.Spades, Jump(1), Fit(9), DummyPoints(WeakJumpRaise), ShowsTrump()),


       

                    // Lowest priority is to bid some level of NT - all fit() bids should be higher priority.
                    Nonforcing(1, Suit.Unknown, OppsStopped(), Points(AdvanceTo1NT))

                    // TODO: Any specification of PASS?>>
                };
            }
            Debug.Fail("Partner.LastCall is not a bid.  How in the world did we get here?");
            return new BidRule[0];
        }


        public static IEnumerable<BidRule> Rebid(PositionState _)
        {
            return new BidRule[] { 
                // TODO: ONly bid these if they are necessary.  Minors don't need to go the 4-level unless forced there...
                Signoff(4, Suit.Clubs, Fit(), PairPoints((26, 28)), ShowsTrump()),
                Signoff(4, Suit.Diamonds, Fit(), PairPoints((26, 28)), ShowsTrump()),
                Signoff(4, Suit.Hearts, Fit(), PairPoints((26, 31)), ShowsTrump()),
                Signoff(4, Suit.Spades, Fit(), PairPoints((26, 31)), ShowsTrump())
            };
        }

    }
}
