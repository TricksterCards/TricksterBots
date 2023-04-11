using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    /*
    public class Responder : Bidder
    {

        static private (int, int) Respond1Level = (6, 40);
        static private (int, int) Raise1 = (6, 10);
        static private (int, int) Respond1NT = (6, 10);
        static private (int, int) NewSuit2Level = (12, 40);  // TODO: null??
        static private (int, int) RaiseTo2NT = (11, 12);
        static private (int, int) SlamInterest = (17, 40);
        static private (int, int) LimitRaise = (11, 12);
        static private (int, int) RaiseTo3NT = (13, 15);
        static private (int, int) Weak4Level = (0, 10);
        static private (int, int) GameOrBetter = (13, 40);


        static private (int, int) NTLessThanInvite = (0, 7);
        static private (int, int) NTInvite = (8, 9);
        static private (int, int) NTInviteOrBetter = (8, 40);
        static private (int, int) NTGame = (10, 15);
        static private (int, int) NTSlamInterest = (16, 40);



        public List<BidRule> Rules(Bid partnersBid, Bid lhoBid) // TODO: Perhaps biddubg round passed in here???)
        {
            List<BidRule> bids = new List<BidRule>();
            if (partnersBid.Is(1, Suit.Clubs) && lhoBid.IsPass)
            {
                BidRule[] b =
                {
                    new BidRule(1, Suit.Diamonds, 10, Points(Respond1Level), Shape(4, 5), LongestMajor(4)),
                    new BidRule(1, Suit.Diamonds, 10, Points(Respond1Level), Shape(6), LongestMajor(5)),
                    new BidRule(1, Suit.Diamonds, 10, Points(Respond1Level), Shape(7, 11), LongestMajor(6)),

                    new BidRule(1, Suit.Hearts, 100, Points(Respond1Level), Shape(4), Shape(Suit.Diamonds, 0, 3), Shape(Suit.Spades, 0, 4)),
                    new BidRule(1, Suit.Hearts, 100, Points(Respond1Level), Shape(5), Shape(Suit.Diamonds, 0, 5), Shape(Suit.Spades, 0, 4)),
                    new BidRule(1, Suit.Hearts, 100, Points(Respond1Level), Shape(6), Shape(Suit.Diamonds, 0, 6), Shape(Suit.Spades, 0, 5)),
                    new BidRule(1, Suit.Hearts, 100, Points(Respond1Level), Shape(7, 11)),

                    new BidRule(1, Suit.Spades, 100, Points(Respond1Level), Shape(4), Shape(Suit.Diamonds, 0, 3), Shape(Suit.Hearts, 0, 3)),
                    new BidRule(1, Suit.Spades, 100, Points(Respond1Level), Shape(5), Shape(Suit.Diamonds, 0, 5), Shape(Suit.Hearts, 0, 5)),
                    new BidRule(1, Suit.Spades, 100, Points(Respond1Level), Shape(6, 11), Shape(Suit.Diamonds, 0, 6), Shape(Suit.Hearts, 0, 6)),

                    new BidRule(1, Suit.Unknown, 0, Points(Respond1NT), Balanced()),

                    new BidRule(2, Suit.Clubs, 0, Points(Raise1), Shape(5), LongestMajor(3)),

                    new BidRule(2, Suit.Diamonds, 200, Points(SlamInterest), Shape(5, 11)),

                    new BidRule(2, Suit.Hearts, 200, Points(SlamInterest), Shape(5, 11)),

                    new BidRule(2, Suit.Spades, 200, Points(SlamInterest), Shape(5, 11)),

                    // TODO: Really balanced?  This would only be the case for 4333 given current rules.  Maybe so...
                    new BidRule(2, Suit.Unknown, 1, Points(RaiseTo2NT), LongestMajor(3), Balanced()),

                    new BidRule(3, Suit.Clubs, 1, Points(LimitRaise), Shape(5), LongestMajor(3)),

                    new BidRule(3, Suit.Unknown, 1, Points(RaiseTo3NT), Balanced(), LongestMajor(3)),

                    new BidRule(4, Suit.Clubs, 1, Points(Weak4Level), Shape(6, 11)),

                    // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                    new BidRule(4, Suit.Hearts, 1, Points(Weak4Level), Shape(7, 11), Quality(SuitQuality.Good)),

                    new BidRule(4, Suit.Spades, 1, Points(Weak4Level), Shape(7, 11), Quality(SuitQuality.Good)),

                };
                bids.Concat(b);

            }
            else if (partnersBid.Is(1, Suit.Diamonds) && lhoBid.IsPass)
            {
                BidRule[] b =
                {

                    new BidRule(1, Suit.Hearts, 100, Points(Respond1Level), Shape(4, 5), Shape(Suit.Spades, 0, 4)),
                    new BidRule(1, Suit.Hearts, 100, Points(Respond1Level), Shape(6), Shape(Suit.Spades, 0, 5)),
                    new BidRule(1, Suit.Hearts, 100, Points(Respond1Level), Shape(7, 11)),

                    new BidRule(1, Suit.Spades, 100, Points(Respond1Level), Shape(4), Shape(Suit.Hearts, 0, 3)),
                    new BidRule(1, Suit.Spades, 100, Points(Respond1Level), Shape(5), Shape(Suit.Hearts, 0, 5)),
                    new BidRule(1, Suit.Spades, 100, Points(Respond1Level), Shape(6, 11), Shape(Suit.Hearts, 0, 6)),

                    new BidRule(1, Suit.Unknown, 0, Points(Respond1NT), Balanced(), LongestMajor(3)),

                   
                    new BidRule(2, Suit.Clubs, 2, Points(NewSuit2Level), Shape(5, 11), LongestMajor(3)),

                    new BidRule(2, Suit.Diamonds, 0, Points(Raise1), Shape(5, 11), LongestMajor(3)),

                    new BidRule(2, Suit.Hearts, 200, Points(SlamInterest), Shape(5, 11)),

                    new BidRule(2, Suit.Spades, 200, Points(SlamInterest), Shape(5, 11)),

                    // TODO: Really balanced?  This would only be the case for 4333 given current rules.  Maybe so...
                    new BidRule(2, Suit.Unknown, 1, Points(RaiseTo2NT), LongestMajor(3), Balanced()),

                    new BidRule(3, Suit.Diamonds, 1, Points(LimitRaise), Shape(5, 11), LongestMajor(3)),

                    new BidRule(3, Suit.Unknown, 1, Points(RaiseTo3NT), LongestMajor(3)),

                    new BidRule(4, Suit.Diamonds, 1, Points(Weak4Level), Shape(6, 11)),

                    // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                    new BidRule(4, Suit.Hearts, 1, Points(Weak4Level), Shape(7, 11)),

                    new BidRule(4, Suit.Spades, 1, Points(Weak4Level), Shape(7, 11)),

                };
                bids.Concat(b);

            }
            else if (partnersBid.Is(1, Suit.Hearts) && lhoBid.IsPass)
            {
                BidRule[] b =
                {
                    // TODO: Only if planning to raise if we ha
                  
                    new BidRule(1, Suit.Spades, 100, Points(Respond1Level), Shape(4, 11), Shape(Suit.Hearts, 0, 2)),
                    new BidRule(1, Suit.Spades, 100, DummyPoints(Suit.Hearts, LimitRaise), Shape(4, 11), Shape(Suit.Hearts, 3)),
                    new BidRule(1, Suit.Spades, 100, DummyPoints(Suit.Hearts, GameOrBetter), Shape(4, 11), Shape(Suit.Hearts, 3, 8)),

                    new BidRule(1, Suit.Unknown, 0, Points(Respond1NT), Balanced()),

                    // Two level minor bids are handled by NewMinorSuit2Level...

                    new BidRule(2, Suit.Hearts, 200, DummyPoints(Suit.Hearts, Raise1), Shape(3, 8)),

                    new BidRule(2, Suit.Spades, 200, Points(SlamInterest), Shape(5, 11)),

                    new BidRule(2, Suit.Unknown, 200, Points(RaiseTo2NT), Balanced()),

                    new BidRule(3, Suit.Hearts, 1, DummyPoints(Suit.Hearts, LimitRaise), Shape(4, 8)),

                    new BidRule(3, Suit.Unknown, 1, Points(RaiseTo3NT), LongestMajor(3)),


                    // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                    new BidRule(4, Suit.Hearts, 1, DummyPoints(Suit.Hearts, Weak4Level), Shape(5, 8)),

                    new BidRule(4, Suit.Spades, 1, Points(Weak4Level), Shape(7, 11)),

                };
                bids.Concat(b);
                bids.Concat(NewMinorSuit2Level(Suit.Hearts));
            }
            else if (partnersBid.Is(1, Suit.Spades) && lhoBid.IsPass)
            {
                {
                    BidRule[] b =
                    {
                        new BidRule(1, Suit.Unknown, 0, Points(Respond1NT), Balanced()),

                        // Two level minor bids are handled by NewMinorSuit2Level...
                        // THIS IS HIGHER PRIORITY THAN SHOWING MINORS NO MATTER WHAT THE LENGTH...
                        new BidRule(2, Suit.Hearts, 1000, Points(NewSuit2Level), Shape(5, 11)),

                        new BidRule(2, Suit.Spades, 200, DummyPoints(Suit.Spades, Raise1), Shape(3, 8)),


                        new BidRule(2, Suit.Unknown, 200, Points(RaiseTo2NT), Balanced()),

                        new BidRule(3, Suit.Spades, 1, DummyPoints(Suit.Spades, LimitRaise), Shape(4, 8)),

                        new BidRule(3, Suit.Unknown, 1, Points(RaiseTo3NT), LongestMajor(3)),

                        // TODO: This is all common wacky bids from thsi point on.  Need to append at the bottom of this function

                        new BidRule(4, Suit.Hearts, 1, Points(Weak4Level), Shape(7, 11)),

                        new BidRule(4, Suit.Spades, 1, DummyPoints(Suit.Spades, Weak4Level), Shape(5, 8))
                    };
                    bids.Concat(b);
                }
            }
            else if (partnersBid.Is(1, Suit.Unknown) && lhoBid.IsPass)
            {
                BidRule[] b =
                {
                    new BidRule(2, Suit.Clubs, 10, Points(NTInviteOrBetter), Shape(Suit.Hearts, 4), Flat(false)),
                    new BidRule(2, Suit.Clubs, 10, Points(NTInviteOrBetter), Shape(Suit.Spades, 4), Flat(false)),

                    new BidRule(2, Suit.Diamonds, 10, Shape(Suit.Hearts, 5, 11)),

                    new BidRule(2, Suit.Hearts, 10, Shape(Suit.Spades, 5, 11)),

                    new BidRule(2, Suit.Spades, 10, Points(NTLessThanInvite), Shape(Suit.Clubs, 6), Quality(Suit.Clubs, SuitQuality.Good)),
                    new BidRule(2, Suit.Spades, 10, Points(NTLessThanInvite), Shape(Suit.Clubs, 7, 11)),
                    new BidRule(2, Suit.Spades, 10, Points(NTLessThanInvite), Shape(Suit.Diamonds, 6), Quality(Suit.Diamonds, SuitQuality.Good)),
                    new BidRule(2, Suit.Spades, 10, Points(NTLessThanInvite), Shape(Suit.Diamonds, 7, 11)),

                    new BidRule(2, Suit.Unknown, 0, Points(NTInvite)),

                    new BidRule(3, Suit.Diamonds, 10, Points(NTInvite), Shape(6, 11), Quality(SuitQuality.Good)),

                    new BidRule(3, Suit.Hearts, 10, Points(NTSlamInterest), Shape(6, 11)),

                    new BidRule(3, Suit.Spades, 10, Points(NTSlamInterest), Shape(6, 11)),

                    new BidRule(3, Suit.Unknown, 0, Points(NTGame))


                    // TODO: For weak hands, transfer to longest major.
                    // TODO: For invitational hands, 5/5 transfer to hearts then bid spades
                    // TODO: For game-going hands 5/5 transfer to spades then bid 3H

                };
                bids.Concat(b);
            }

            // Now add wacky bids for huge hands.
            bids.Concat(HighLevelHugeHands);
            return bids;
        }


        private BidRule[] NewMinorSuit2Level(Suit openersSuit)
        {
            BidRule[] b =
            {
                    new BidRule(2, Suit.Clubs, 2, Points(NewSuit2Level), Shape(4, 5), Shape(Suit.Diamonds, 0, 4)),
                    new BidRule(2, Suit.Clubs, 2, Points(NewSuit2Level), Shape(6), Shape(Suit.Diamonds, 0, 5)),
                    new BidRule(2, Suit.Clubs, 2, Points(NewSuit2Level), Shape(7, 11)),
                    new BidRule(2, Suit.Clubs, 2, DummyPoints(openersSuit, LimitRaise), Shape(3), Shape(openersSuit, 3), Shape(Suit.Diamonds, 0, 3)),
                    new BidRule(2, Suit.Clubs, 2, DummyPoints(openersSuit, LimitRaise), Shape(4, 5), Shape(openersSuit, 3), Shape(Suit.Diamonds, 0, 4)),
                    new BidRule(2, Suit.Clubs, 2, DummyPoints(openersSuit, LimitRaise), Shape(6), Shape(openersSuit, 3)),
                    new BidRule(2, Suit.Clubs, 2, DummyPoints(openersSuit, GameOrBetter), Shape(3), Shape(openersSuit, 3, 11), Shape(Suit.Diamonds, 0, 3)),
                    new BidRule(2, Suit.Clubs, 2, DummyPoints(openersSuit, GameOrBetter), Shape(4, 5), Shape(openersSuit, 3, 11), Shape(Suit.Diamonds, 0, 4)),
                    new BidRule(2, Suit.Clubs, 2, DummyPoints(openersSuit, GameOrBetter), Shape(6, 11), Shape(openersSuit, 3, 11)),


                    new BidRule(2, Suit.Diamonds, 2, Points(NewSuit2Level), Shape(4), Shape(Suit.Clubs, 0, 3)),
                    new BidRule(2, Suit.Diamonds, 2, Points(NewSuit2Level), Shape(5), Shape(Suit.Clubs, 0, 5)),
                    new BidRule(2, Suit.Diamonds, 2, Points(NewSuit2Level), Shape(6), Shape(Suit.Clubs, 0, 6)),
                    new BidRule(2, Suit.Diamonds, 2, Points(NewSuit2Level), Shape(7, 11)),
                    new BidRule(2, Suit.Diamonds, 2, DummyPoints(openersSuit, LimitRaise), Shape(3), Shape(openersSuit, 3), Shape(Suit.Clubs, 0, 2)),
                    new BidRule(2, Suit.Diamonds, 2, DummyPoints(openersSuit, LimitRaise), Shape(4), Shape(openersSuit, 3), Shape(Suit.Clubs, 0, 3)),
                    new BidRule(2, Suit.Diamonds, 2, DummyPoints(openersSuit, LimitRaise), Shape(5), Shape(openersSuit, 3), Shape(Suit.Clubs, 0, 5)),
                    new BidRule(2, Suit.Diamonds, 2, DummyPoints(openersSuit, LimitRaise), Shape(6, 11), Shape(openersSuit, 3)),
                    new BidRule(2, Suit.Diamonds, 2, DummyPoints(openersSuit, GameOrBetter), Shape(3), Shape(openersSuit, 3, 11), Shape(Suit.Clubs, 0, 2)),
                    new BidRule(2, Suit.Diamonds, 2, DummyPoints(openersSuit, GameOrBetter), Shape(4), Shape(openersSuit, 3, 11), Shape(Suit.Clubs, 0, 3)),
                    new BidRule(2, Suit.Diamonds, 2, DummyPoints(openersSuit, GameOrBetter), Shape(5), Shape(openersSuit, 3, 11), Shape(Suit.Clubs, 0, 5)),
                    new BidRule(2, Suit.Diamonds, 2, DummyPoints(openersSuit, GameOrBetter), Shape(6, 11), Shape(openersSuit, 3, 11)),
            };
            return b;
        }

    }
    */

}
