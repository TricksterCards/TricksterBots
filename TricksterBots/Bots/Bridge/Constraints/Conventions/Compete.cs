using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    internal class Compete : Bidder
    {
        public (int, int) CompeteTo2 = (20, 22);
        public (int, int) CompeteTo3 = (23, 24);
        public (int, int) CompeteTo4 = (25, 28);
        public Compete() : base(Convention.Natural, 100)
        {
            //  this.ConventionRules = new ConventionRule[]
            //  {
            //  }
            this.BidRules = new BidRule[]
            {
             //   Nonforcing(CallType.Pass, 0),    // TODO: What points?  This is the last gasp attempt here...
                
                Nonforcing(2, Suit.Clubs, Fit(), PairPoints(CompeteTo2), ShowsTrump()),
                Nonforcing(2, Suit.Diamonds, Fit(), PairPoints(CompeteTo2), ShowsTrump()),
                Nonforcing(2, Suit.Hearts, Fit(), PairPoints(CompeteTo2), ShowsTrump()),
                Nonforcing(2, Suit.Spades, Fit(), PairPoints(CompeteTo2), ShowsTrump()),

                Nonforcing(3, Suit.Clubs, Fit(), PairPoints(CompeteTo3), ShowsTrump()),
                Nonforcing(3, Suit.Diamonds, Fit(), PairPoints(CompeteTo3), ShowsTrump()),
                Nonforcing(3, Suit.Hearts, Fit(), PairPoints(CompeteTo3), ShowsTrump()),
                Nonforcing(3, Suit.Spades, Fit(), PairPoints(CompeteTo3), ShowsTrump()),

                Nonforcing(4, Suit.Clubs, Fit(), PairPoints(CompeteTo4), ShowsTrump()),
                Nonforcing(3, Suit.Diamonds, Fit(), PairPoints(CompeteTo4), ShowsTrump()),
                Nonforcing(4, Suit.Hearts, Fit(), PairPoints(CompeteTo4), ShowsTrump()),
                Nonforcing(4, Suit.Spades, Fit(), PairPoints(CompeteTo4), ShowsTrump()),


            };
            
        }
    }
}
