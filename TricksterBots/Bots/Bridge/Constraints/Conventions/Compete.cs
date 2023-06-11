﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    internal class Compete : Bidder
    {



        private (int, int) CompeteTo2 = (20, 22);
        private (int, int) CompeteTo3 = (23, 25);
        private (int, int) CompeteTo2NT = (20, 24);
        private (int, int) CompeteTo3NT = (25, 31); // TODO: Add more...
        private (int, int) CompeteTo4 = (26, 28);
        private (int, int) CompeteTo5 = (29, 32);



        // TODO: This is super ugly.  Need to think through how bids work / fall-through or get them like this
        // throug a static function.  These are all duplicated.  Can be appended to the end of another list.  
        // right now used by ResponderRebid.  
        public static List<BidRule> HackXXXGetBids()
        {
            var c = new Compete();
            return c.HackGetBids();
        }

        private List<BidRule> HackGetBids()
        {
            return new List<BidRule>()
            {
                // Stilly buy highest priority bids for any hand...
                Nonforcing(7, Suit.Clubs, Shape(13)),
                Nonforcing(7, Suit.Diamonds, Shape(13)),
                Nonforcing(7, Suit.Hearts, Shape(13)),
                Nonforcing(7, Suit.Spades, Shape(13)),


             //   Nonforcing(Call.Pass, 0),    // TOD   aO: What points?  This is the last gasp attempt here...

                Nonforcing(4, Suit.Hearts, Fit(), PairPoints(CompeteTo4), ShowsTrump()),
                Nonforcing(4, Suit.Spades, Fit(), PairPoints(CompeteTo4), ShowsTrump()),



                Nonforcing(2, Suit.Clubs, Fit(), PairPoints(CompeteTo2), ShowsTrump()),
                Nonforcing(2, Suit.Diamonds, Fit(), PairPoints(CompeteTo2), ShowsTrump()),
                Nonforcing(2, Suit.Hearts, Fit(), PairPoints(CompeteTo2), ShowsTrump()),
                Nonforcing(2, Suit.Spades, Fit(), PairPoints(CompeteTo2), ShowsTrump()),

                Nonforcing(3, Suit.Clubs,  Fit(), PairPoints(CompeteTo3), ShowsTrump()),
                Nonforcing(3, Suit.Diamonds,  Fit(), PairPoints(CompeteTo3), ShowsTrump()),
                Nonforcing(3, Suit.Hearts, Fit(), PairPoints(CompeteTo3), ShowsTrump()),
                Nonforcing(3, Suit.Spades, Fit(), PairPoints(CompeteTo3), ShowsTrump()),

                Signoff(3, Suit.Unknown, OppsStopped(), PairPoints(CompeteTo3NT)),

                Signoff(2, Suit.Unknown, OppsContract(), OppsStopped(), PairPoints(CompeteTo2NT)),


                Nonforcing(4, Suit.Clubs, Fit(), PairPoints(CompeteTo4), ShowsTrump()),
                Nonforcing(4, Suit.Diamonds, Fit(), PairPoints(CompeteTo4), ShowsTrump()),

                Nonforcing(5, Suit.Clubs, Fit(), PairPoints(CompeteTo5), ShowsTrump()),
                Nonforcing(5, Suit.Diamonds, Fit(), PairPoints(CompeteTo5), ShowsTrump()),


                // TODO: Priority for these???
                Nonforcing(6, Suit.Clubs, Shape(12)),
                Nonforcing(6, Suit.Diamonds, Shape(12)),
                Nonforcing(6, Suit.Hearts, Shape(12)),
                Nonforcing(6, Suit.Spades, Shape(12)),

            };

        }


        private PrescribedBids GetBids()
        {
            var pb = new PrescribedBids();
            pb.Bids = HackGetBids();
            return pb;
        }
    }
}
