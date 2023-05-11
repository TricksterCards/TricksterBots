﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    internal class Compete : Bidder
    {

        // TODO: Terrible name - not really initiate, 
        public static PrescribedBids InitiateConvention()
        {
            var bidder = new Compete();
            return new PrescribedBids(bidder, bidder.Initiate);
        }


        private (int, int) CompeteTo2 = (20, 22);
        private (int, int) CompeteTo3 = (23, 25);
        private (int, int) CompeteTo4 = (26, 28);
        private (int, int) CompeteTo5 = (29, 32);
        private Compete() : base(Convention.Competative, 10)
        {
        }

        private void Initiate(PrescribedBids pb)
        { 
            pb.Bids = new BidRule[]
            {
             //   Nonforcing(Call.Pass, 0),    // TOD   aO: What points?  This is the last gasp attempt here...
                
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


                Nonforcing(5, Suit.Clubs, Fit(), PairPoints(CompeteTo5), ShowsTrump()),
                Nonforcing(5, Suit.Diamonds, Fit(), PairPoints(CompeteTo5), ShowsTrump())
            };
            
        }
    }
}
