
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class StandardAmericanOvercallAdvance : StandardAmerican
    {

		public static IEnumerable<BidRule> Overcall(PositionState _)
        {
            return new BidRule[] {
                // TODO: What is the level of interference we can take
                DefaultPartnerBids(new Bid(4, Suit.Unknown), Advance), 
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
          
        }


        public static BidRule[] PassInsteadOfOvercall(PositionState ps)
        {
            return new BidRule[]
            {

                Nonforcing(Call.Pass, Points(LessThanOvercall))
            };
        }

        private static IEnumerable<BidRule> Advance(PositionState ps)
        {
            if (ps.Partner.LastCall is Bid partnerBid)
            {
                var partnerSuit = partnerBid.Suit;
                return new BidRule[] {
                    // TODO: What is the level of interference we can take
                    DefaultPartnerBids(new Bid(4, Suit.Unknown), OvercallRebid),
                    Nonforcing(1, Suit.Hearts, Points(AdvanceNewSuit1Level), Shape(5), GoodSuit()),
                    Nonforcing(1, Suit.Hearts, Points(AdvanceNewSuit1Level), Shape(6, 11)),

                    Nonforcing(1, Suit.Spades, Points(AdvanceNewSuit1Level), Shape(5), GoodSuit()),
                    Nonforcing(1, Suit.Spades, Points(AdvanceNewSuit1Level), Shape(6, 11)),

               
                    // TODO: Should these be prioirty - 5 - support should be higher priorty.  Seems reasonable
                    Nonforcing(2, Suit.Clubs, Points(AdvanceNewSuit2Level), Shape(5), GoodSuit()),
                    Nonforcing(2, Suit.Clubs, Points(AdvanceNewSuit2Level), Shape(6, 11)),
                    Nonforcing(2, Suit.Diamonds, Points(AdvanceNewSuit2Level), Shape(5), GoodSuit()),
                    Nonforcing(2, Suit.Diamonds, Points(AdvanceNewSuit2Level), Shape(6, 11)),
                    Nonforcing(2, Suit.Hearts, Jump(0), Points(AdvanceNewSuit2Level), Shape(5), GoodSuit()),
                    Nonforcing(2, Suit.Hearts, Jump(0), Points(AdvanceNewSuit2Level), Shape(6, 11)),
                    Nonforcing(2, Suit.Spades, Jump(0), Points(AdvanceNewSuit2Level), Shape(5), GoodSuit()),
                    Nonforcing(2, Suit.Spades, Jump(0), Points(AdvanceNewSuit2Level), Shape(6, 11)),



                    // TODO: Make a special BidRule here to handle rebid after cuebid...
                    Forcing(2, Suit.Clubs, CueBid(), Fit(partnerSuit), DummyPoints(AdvanceCuebid), ShowsTrump(partnerSuit)),
                    Forcing(2, Suit.Diamonds, CueBid(), Fit(partnerSuit), DummyPoints(AdvanceCuebid), ShowsTrump(partnerSuit)),
                    Forcing(2, Suit.Hearts, CueBid(), Fit(partnerSuit), DummyPoints(AdvanceCuebid), ShowsTrump(partnerSuit)),
                    Forcing(2, Suit.Spades, CueBid(), Fit(partnerSuit), DummyPoints(AdvanceCuebid), ShowsTrump(partnerSuit)),

                    // 2C is not really possible since this is an advance...
                    Nonforcing(2, Suit.Diamonds, Partner(HasMinShape(5)), Fit(), DummyPoints(AdvanceRaise), ShowsTrump()),
                    Nonforcing(2, Suit.Hearts, Partner(HasMinShape(5)), Fit(), DummyPoints(AdvanceRaise), ShowsTrump()),
                    Nonforcing(2, Suit.Spades, Partner(HasMinShape(5)), Fit(), DummyPoints(AdvanceRaise), ShowsTrump()),

                    // Fill this out better but for now just go on law of total trump, jumping if weak.  
                    Nonforcing(4, Suit.Clubs, Jump(1, 2), Fit(10), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                    Nonforcing(4, Suit.Diamonds, Jump(1, 2), Fit(10), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                    Nonforcing(4, Suit.Hearts, Jump(1, 2), Fit(10), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                    Nonforcing(4, Suit.Spades, Jump(1, 2), Fit(10), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),

                    Nonforcing(3, Suit.Clubs, Jump(1), Fit(9), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                    Nonforcing(3, Suit.Diamonds, Jump(1), Fit(9), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                    Nonforcing(3, Suit.Hearts, Jump(1), Fit(9), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),
                    Nonforcing(3, Suit.Spades, Jump(1), Fit(9), DummyPoints(AdvanceWeakJumpRaise), ShowsTrump()),


                    // Lowest priority is to bid some level of NT - all fit() bids should be higher priority.
                    Nonforcing(1, Suit.Unknown, OppsStopped(), Points(AdvanceTo1NT))

                    // TODO: Any specification of PASS?>>
                };
            }
            Debug.Fail("Partner.LastCall is not a bid.  How in the world did we get here?");
            return new BidRule[0];
        }
       

        private static IEnumerable<BidRule> OvercallRebid(PositionState _)
        {
            return new BidRule[] {
                DefaultPartnerBids(new Bid(4, Suit.Hearts), AdvancerRebid),
                // TODO: NEED TO FORMALIZE THE POINT RANGES... FOR NOW JUST LOOK AT 3-LEVEL BIDS
                Nonforcing(3, Suit.Clubs, Fit(), PairPoints((24, 25)), ShowsTrump()),
                Nonforcing(3, Suit.Diamonds, Fit(), PairPoints((24, 25)), ShowsTrump()),
                Nonforcing(3, Suit.Hearts, Fit(), PairPoints((24, 25)), ShowsTrump()),
                Nonforcing(3, Suit.Spades, Fit(), PairPoints((24, 25)), ShowsTrump()),

                Signoff(3, Suit.Unknown, OppsStopped(), OppsStopped(), PairPoints((25, 30)) )

            };
        }


        private static IEnumerable<BidRule> AdvancerRebid(PositionState _)
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
