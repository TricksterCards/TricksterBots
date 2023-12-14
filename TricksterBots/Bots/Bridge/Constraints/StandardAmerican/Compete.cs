using System;
using System.Collections.Generic;

namespace BridgeBidding
{
    internal class Compete : Bidder
    {



        private static (int, int) CompeteTo2 = (20, 22);
        private static (int, int) CompeteTo3 = (23, 25);
        private static (int, int) CompeteTo2NT = (20, 24);
        private static (int, int) CompeteTo3NT = (25, 31); // TODO: Add more...
        private static (int, int) CompeteTo4 = (26, 28);
        private static (int, int) CompeteTo5 = (29, 32);



        // TODO: This is super ugly.  Need to think through how bids work / fall-through or get them like this
        // throug a static function.  These are all duplicated.  Can be appended to the end of another list.  
        // right now used by ResponderRebid.  

        public static IEnumerable<BidRule> CompBids(PositionState ps)
        {
            var bids = new List<BidRule>();
            bids.AddRange(Blackwood.InitiateConvention(ps));
            bids.AddRange(Gerber.InitiateConvention(ps));
            bids.AddRange(new BidRule[]
            {
                // Stilly buy highest priority bids for any hand...
                Nonforcing(7, Strain.Clubs, Shape(13)),
                Nonforcing(7, Strain.Diamonds, Shape(13)),
                Nonforcing(7, Strain.Hearts, Shape(13)),
                Nonforcing(7, Strain.Spades, Shape(13)),


             //   Nonforcing(Call.Pass, 0),    // TOD   aO: What points?  This is the last gasp attempt here...

                Nonforcing(4, Strain.Hearts, Fit(), PairPoints(CompeteTo4), ShowsTrump()),
                Nonforcing(4, Strain.Spades, Fit(), PairPoints(CompeteTo4), ShowsTrump()),



                Nonforcing(2, Strain.Clubs, Fit(), PairPoints(CompeteTo2), ShowsTrump()),
                Nonforcing(2, Strain.Diamonds, Fit(), PairPoints(CompeteTo2), ShowsTrump()),
                Nonforcing(2, Strain.Hearts, Fit(), PairPoints(CompeteTo2), ShowsTrump()),
                Nonforcing(2, Strain.Spades, Fit(), PairPoints(CompeteTo2), ShowsTrump()),

                Nonforcing(3, Strain.Clubs,  Fit(), PairPoints(CompeteTo3), ShowsTrump()),
                Nonforcing(3, Strain.Diamonds,  Fit(), PairPoints(CompeteTo3), ShowsTrump()),
                Nonforcing(3, Strain.Hearts, Fit(), PairPoints(CompeteTo3), ShowsTrump()),
                Nonforcing(3, Strain.Spades, Fit(), PairPoints(CompeteTo3), ShowsTrump()),

                Signoff(3, Strain.NoTrump, OppsStopped(), PairPoints(CompeteTo3NT)),

                Signoff(2, Strain.NoTrump, OppsContract(), OppsStopped(), PairPoints(CompeteTo2NT)),


                Nonforcing(4, Strain.Clubs, Fit(), PairPoints(CompeteTo4), ShowsTrump()),
                Nonforcing(4, Strain.Diamonds, Fit(), PairPoints(CompeteTo4), ShowsTrump()),

                Nonforcing(5, Strain.Clubs, Fit(), PairPoints(CompeteTo5), ShowsTrump()),
                Nonforcing(5, Strain.Diamonds, Fit(), PairPoints(CompeteTo5), ShowsTrump()),

                // TODO: Penalty doubles for game contracts.
                Signoff(Call.Double, OppsContract(), PairPoints((12, 40)), RuleOf9()),

                // TODO: Priority for these???
                Nonforcing(6, Strain.Clubs, Shape(12)),
                Nonforcing(6, Strain.Diamonds, Shape(12)),
                Nonforcing(6, Strain.Hearts, Shape(12)),
                Nonforcing(6, Strain.Spades, Shape(12)),

            });
            return bids;
        }


    }
}
