
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class Overcall: StandardAmerican
    {

        public static new BidChoices GetBidChoices(PositionState ps)
        {

            var choices = new BidChoices(ps);
            choices.AddRules(SuitOvercall);
            choices.AddRules(NoTrump.StrongOvercall);
            choices.AddRules(TakeoutDouble.InitiateConvention);
            choices.AddRules(NoTrump.BalancingOvercall);

            // TODO: Perhaps do this for open also -- Pass as separate and final rule group...
            choices.AddRules(new BidRule[] { Nonforcing(Call.Pass, Points(LessThanOvercall)) });

            return choices;
        }

		public static IEnumerable<BidRule> SuitOvercall(PositionState _)
        {
            return new BidRule[] {
                // TODO: What is the level of interference we can take
                DefaultPartnerBids(new Bid(4, Suit.Unknown), Advance.FirstBid), 
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


       

       

        public static IEnumerable<BidRule> Rebid(PositionState _)
        {
            return new BidRule[] {
                DefaultPartnerBids(new Bid(4, Suit.Hearts), Advance.Rebid),


                // TODO: NEED TO FORMALIZE THE POINT RANGES... FOR NOW JUST LOOK AT 3-LEVEL BIDS
                Nonforcing(3, Suit.Clubs, Fit(), PairPoints((24, 25)), ShowsTrump()),
                Nonforcing(3, Suit.Diamonds, Fit(), PairPoints((24, 25)), ShowsTrump()),
                Nonforcing(3, Suit.Hearts, Fit(), PairPoints((24, 25)), ShowsTrump()),
                Nonforcing(3, Suit.Spades, Fit(), PairPoints((24, 25)), ShowsTrump()),

                Signoff(3, Suit.Unknown, OppsStopped(), OppsStopped(), PairPoints((25, 30)) )

            };
        }


    }

}
