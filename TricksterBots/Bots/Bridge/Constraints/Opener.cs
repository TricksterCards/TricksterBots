using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{

    internal class Opener : Bidder
    {
        public (int, int) Open1Suit = (13, 21);
        public (int, int) Open1NT = (15, 17);
        public (int, int) Open2Suit = (5, 10);
        public (int, int) Open2NT = (20, 21);
        public (int, int) OpenStrong = (22, int.MaxValue);
        public (int, int) LessThanOpen = (0, 12);

       

        public BidRule[] Rules() // TODO: Perhaps biddubg round passed in here???)
        {
            BidRule[] bids =
            {
                new BidRule(1, Suit.Clubs, 10, Points(Open1Suit),
                            Shape(3, 4), Shape(Suit.Diamonds, 0, 3), LongestMajor(4)),
                new BidRule(1, Suit.Clubs, 10, Points(Open1Suit),
                            Shape(5), Shape(Suit.Diamonds, 0, 4), LongestMajor(4)),
                new BidRule(1, Suit.Clubs, 10, Points(Open1Suit),
                            Shape(6), Shape(Suit.Diamonds, 0, 5), LongestMajor(4)),
                new BidRule(1, Suit.Clubs, 10, Points(Open1Suit),
                            Shape(7, 11), LongestMajor(4)),

                new BidRule(1, Suit.Diamonds, 10, Points(Open1Suit),
                            Shape(3), Shape(Suit.Clubs, 2), LongestMajor(4)),
                new BidRule(1, Suit.Diamonds, 10, Points(Open1Suit),
                            Shape(4), Shape(Suit.Clubs, 0, 4), LongestMajor(4)),
                new BidRule(1, Suit.Diamonds, 10, Points(Open1Suit),
                            Shape(5), Shape(Suit.Clubs, 0, 5), LongestMajor(4)),
                new BidRule(1, Suit.Diamonds, 10, Points(Open1Suit),
                            Shape(6), Shape(Suit.Clubs, 0, 6), LongestMajor(4)), 
                new BidRule(1, Suit.Diamonds, 10, Points(Open1Suit),
                            Shape(7, 11), LongestMajor(4)),

                new BidRule(1, Suit.Hearts, 50, Points(Open1Suit), Shape(5), Shape(Suit.Spades, 0, 4)),
                new BidRule(1, Suit.Hearts, 50, Points(Open1Suit), Shape(6), Shape(Suit.Spades, 0, 5)),
                new BidRule(1, Suit.Hearts, 50, Points(Open1Suit), Shape(7, 11)),

                new BidRule(1, Suit.Spades, 50, Points(Open1Suit), Shape(5), Shape(Suit.Hearts, 0, 5)),
                new BidRule(1, Suit.Spades, 50, Points(Open1Suit), Shape(6, 11), Shape(Suit.Hearts, 0, 6)),

                new BidRule(1, Suit.Unknown, 100, Points(Open1NT), Balanced()),

                new BidRule(2, Suit.Clubs, 0, Points(OpenStrong)),

                new BidRule(2, Suit.Diamonds, 0, Points(Open2Suit), Shape(6), Quality(SuitQuality.Good)),
    
                new BidRule(2, Suit.Hearts, 0, Points(Open2Suit), Shape(6), Quality(SuitQuality.Good)),

                new BidRule(2, Suit.Spades, 0, Points(Open2Suit), Shape(6), Quality(SuitQuality.Good)),

                new BidRule(2, Suit.Unknown, 1, Points(Open2NT), Balanced()),

                new BidRule(3, Suit.Clubs, 0, Points(LessThanOpen), Shape(7), Quality(SuitQuality.Good)),

                new BidRule(3, Suit.Diamonds, 0, Points(LessThanOpen), Shape(7), Quality(SuitQuality.Good)),

                new BidRule(3, Suit.Hearts, 0, Points(LessThanOpen), Shape(7), Quality(SuitQuality.Good)),

                new BidRule(3, Suit.Spades, 0, Points(LessThanOpen), Shape(7), Quality(SuitQuality.Good)),

                new BidRule(6, Suit.Clubs, 1000, Shape(12)),
                new BidRule(6, Suit.Diamonds, 1000, Shape(12)),
                new BidRule(6, Suit.Hearts, 1000, Shape(12)),
                new BidRule(6, Suit.Spades, 1000, Shape(12)),

                new BidRule(7, Suit.Clubs, 1000, Shape(13)),
                new BidRule(7, Suit.Diamonds, 1000, Shape(13)),
                new BidRule(7, Suit.Hearts, 1000, Shape(13)),
                new BidRule(7, Suit.Spades, 1000, Shape(13)),
            };
            return bids;
        }

    }



   
}
