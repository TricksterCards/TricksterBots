﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Trickster.Bots;
using Trickster.cloud;
using TricksterBots.Bots.Bridge;

namespace TricksterBots.Bots.Bridge
{

    public class Natural : Bidder
    {

        public static PrescribedBids DefaultBidderXXX()
        {
            var bidder = new Natural();
            return new PrescribedBids(bidder, bidder.Initiate);
        }

        protected Natural() : base(Convention.Natural, 100)
        {

        }

        public (int, int) Open1Suit = (13, 21);
        public (int, int) Open1NT = (15, 17);
        public (int, int) Open2Suit = (5, 12);  // AG Bid card says 5-10 but when you count length you get 11 or 12
        public (int, int) Open2NT = (20, 21);
        public (int, int) Open3NT = (25, 27);
        public (int, int) OpenStrong = (22, int.MaxValue);
        public (int, int) LessThanOpen = (0, 12);
        public (int, int) OpenerRebid1NT = (13, 14);
        public (int, int) MinimumOpener = (13, 16);
    


        // TODO: This is not a great name.  Not exactly right.  Fix later.....
        public (int, int) LessThanOvercall = (0, 17);
        public (int, int) Overcall1Level = (7, 17);
        public (int, int) OvercallStrong2Level = (13, 17);
        public (int, int) OvercallWeak2Level = (7, 11);
        public (int, int) OvercallWeak3Level = (7, 11);

        public (int, int) AdvanceNewSuit1Level = (6, 40); // TODO: Highest level for this?
        public (int, int) AdvanceNewSuit2Level = (11, 40); // Same here...
        public (int, int) AdvanceTo1NT = (6, 10);
        public (int, int) AdvanceWeakJumpRaise = (0, 9);
        public (int, int) AdvanceRaise = (6, 9);
        public (int, int) AdvanceCuebid = (10, 40);

        public BidRule[] HighLevelHugeHands()
        {
            BidRule[] bids =

            {
                Signoff(6, Suit.Clubs, Shape(12)),
                Signoff(6, Suit.Diamonds, Shape(12)),
                Signoff(6, Suit.Hearts, Shape(12)),
                Signoff(6, Suit.Spades, Shape(12)),

                Signoff(7, Suit.Clubs, Shape(13)),
                Signoff(7, Suit.Diamonds, Shape(13)),
                Signoff(7, Suit.Hearts, Shape(13)),
                Signoff(7, Suit.Spades, Shape(13))
            };
            return bids;
        }
        private void Initiate(PrescribedBids pb)
        { 
            pb.Redirects = new RedirectRule[]
            {
                // TODO: DO NOT CALL ALL STATIC METHODS INITIATECONVENTION OR ELSE WILL CALL BASE CLASS... NAMING IS IMPORTANT.
                Redirect(NaturalOpen.xxx, Role(PositionRole.Opener, 1)),
                Redirect(NaturalOvercall.xxx, Role(PositionRole.Overcaller, 1)),
                Redirect(NaturalRespond.xxx, Role(PositionRole.Responder, 1))
            };
        }
    }
}
