using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace BridgeBidding
{
    public class Advance : StandardAmerican
    {
        public static (int, int) AdvanceNewSuit1Level = (6, 40); // TODO: Highest level for this?
        public static (int, int) NewSuit2Level = (11, 40); // Same here...
        public static (int, int) AdvanceTo1NT = (6, 10);
        public static (int, int) PairAdvanceTo2NT = (23, 24);
        public static (int, int) PairAdvanceTo3NT = (25, 31);   
        public static (int, int) WeakJumpRaise = (0, 8);   // TODO: What is the high end of jump raise weak
        public static (int, int) Raise = (6, 10);
        public static (int, int) AdvanceCuebid = (11, 40);


        public static IEnumerable<BidRule> FirstBid(PositionState ps)
        {
            if (ps.Partner.LastCall is Bid partnerBid)
            {
                // NOTE: We only shold get here when a suit has been bid.  NoTrump overcalls have different logic.
                Suit partnerSuit = (Suit)partnerBid.Suit;
                var bids = new List<BidRule>
                {
                    // TODO: What is the level of interference we can take
                    DefaultPartnerBids(new Bid(4, Strain.NoTrump), Overcall.Rebid),

                                        // Weak jumps to game are highter priority than simple raises.
                    // Fill this out better but for now just go on law of total trump, jumping if weak.  
                    Nonforcing(4, Strain.Clubs, Jump(1, 2), Fit(10), DummyPoints(WeakJumpRaise), ShowsTrump()),
                    Nonforcing(4, Strain.Diamonds, Jump(1, 2, 3), Fit(10), DummyPoints(WeakJumpRaise), ShowsTrump()),
                    Nonforcing(4, Strain.Hearts, Jump(1, 2, 3), Fit(10), DummyPoints(WeakJumpRaise), ShowsTrump()),
                    Nonforcing(4, Strain.Spades, Break(false, "4SWeek"), Jump(1, 2, 3), Fit(10), DummyPoints(WeakJumpRaise), ShowsTrump()),


                    // If we have support for partner
                    Nonforcing(2, Strain.Diamonds,  Fit(), DummyPoints(Raise), ShowsTrump()),
                    Nonforcing(2, Strain.Hearts,    Fit(), DummyPoints(Raise), ShowsTrump()),
                    Nonforcing(2, Strain.Spades,    Fit(), DummyPoints(Raise), ShowsTrump()),


                    Nonforcing(1, Strain.Hearts, Points(AdvanceNewSuit1Level), Shape(5), GoodSuit()),
                    Nonforcing(1, Strain.Hearts, Points(AdvanceNewSuit1Level), Shape(6, 11)),

                    Nonforcing(1, Strain.Spades, Points(AdvanceNewSuit1Level), Shape(5), GoodSuit()),
                    Nonforcing(1, Strain.Spades, Points(AdvanceNewSuit1Level), Shape(6, 11)),

               
                    // TODO: Should these be prioirty - 5 - support should be higher priorty.  Seems reasonable
                    Nonforcing(2, Strain.Clubs, Points(NewSuit2Level), Shape(5), GoodSuit()),
                    Nonforcing(2, Strain.Clubs, Points(NewSuit2Level), Shape(6, 11)),
                    Nonforcing(2, Strain.Diamonds, Points(NewSuit2Level), Shape(5), GoodSuit()),
                    Nonforcing(2, Strain.Diamonds, Points(NewSuit2Level), Shape(6, 11)),
                    Nonforcing(2, Strain.Hearts, Jump(0), Points(NewSuit2Level), Shape(5), GoodSuit()),
                    Nonforcing(2, Strain.Hearts, Jump(0), Points(NewSuit2Level), Shape(6, 11)),
                    Nonforcing(2, Strain.Spades, Jump(0), Points(NewSuit2Level), Shape(5), GoodSuit()),
                    Nonforcing(2, Strain.Spades, Jump(0), Points(NewSuit2Level), Shape(6, 11)),



                    // TODO: Make a special BidRule here to handle rebid after cuebid...
                    Forcing(2, Strain.Clubs, CueBid(), Fit(partnerSuit), DummyPoints(AdvanceCuebid), ShowsTrump(partnerSuit)),
                    Forcing(2, Strain.Diamonds, CueBid(), Fit(partnerSuit), DummyPoints(AdvanceCuebid), ShowsTrump(partnerSuit)),
                    Forcing(2, Strain.Hearts, CueBid(), Fit(partnerSuit), DummyPoints(AdvanceCuebid), ShowsTrump(partnerSuit)),
                    Forcing(2, Strain.Spades, CueBid(), Fit(partnerSuit), DummyPoints(AdvanceCuebid), ShowsTrump(partnerSuit)),

 

                    Nonforcing(3, Strain.Clubs, Jump(1), Fit(9), DummyPoints(WeakJumpRaise), ShowsTrump()),
                    Nonforcing(3, Strain.Diamonds, Jump(1), Fit(9), DummyPoints(WeakJumpRaise), ShowsTrump()),
                    Nonforcing(3, Strain.Hearts, Jump(1), Fit(9), DummyPoints(WeakJumpRaise), ShowsTrump()),
                    Nonforcing(3, Strain.Spades, Jump(1), Fit(9), DummyPoints(WeakJumpRaise), ShowsTrump()),


                    // Need to differentiate between weak and strong overcalls and advance properly.
                    // Perhaps depend more on PairPoints(). 

                    // Lowest priority is to bid some level of NT - all fit() bids should be higher priority.
                    Nonforcing(1, Strain.NoTrump, OppsStopped(), Points(AdvanceTo1NT)),
                    Nonforcing(2, Strain.NoTrump, OppsStopped(), PairPoints(PairAdvanceTo2NT)),
					Nonforcing(3, Strain.NoTrump, OppsStopped(), PairPoints(PairAdvanceTo3NT))


                    // TODO: Any specification of PASS?>>
                };
                // TODO: Should this be higher priority?
                // TODO: Are there situations where 4NT is not blackwood.  Overcall 4D advanace 4NT?
                bids.AddRange(Blackwood.InitiateConvention(ps));
                return bids;
            }
            Debug.Fail("Partner.LastCall is not a bid.  How in the world did we get here?");
            return new BidRule[0];
        }


        public static IEnumerable<BidRule> Rebid(PositionState _)
        {
            return new BidRule[] { 
                // TODO: ONly bid these if they are necessary.  Minors don't need to go the 4-level unless forced there...
                Signoff(4, Strain.Clubs, Fit(), PairPoints((26, 28)), ShowsTrump()),
                Signoff(4, Strain.Diamonds, Fit(), PairPoints((26, 28)), ShowsTrump()),
                Signoff(4, Strain.Hearts, Fit(), PairPoints((26, 31)), ShowsTrump()),
                Signoff(4, Strain.Spades, Fit(), PairPoints((26, 31)), ShowsTrump())
            };
        }

    }
}
