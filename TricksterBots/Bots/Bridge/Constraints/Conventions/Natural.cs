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

        public static Bidder Bidder() { return new NaturalRedirects();  }

        protected Natural() : base(Convention.Natural, 100)
        {

        }

        public (int, int) Open1Suit = (13, 21);
        public (int, int) Open1NT = (15, 17);
        public (int, int) Open2Suit = (5, 10);
        public (int, int) Open2NT = (20, 21);
        public (int, int) Open3NT = (25, 27);
        public (int, int) OpenStrong = (22, int.MaxValue);
        public (int, int) LessThanOpen = (0, 12);


        // TODO: This is not a great name.  Not exactly right.  Fix later.....
        public (int, int) LessThanOvercall = (0, 17);
        public (int, int) Overcall1Level = (7, 17);
        public (int, int) OvercallStrong2Level = (13, 17);
        public (int, int) OvercallWeak2Level = (7, 11);
        public (int, int) OvercallWeak3Level = (7, 11);

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

    }

    public class NaturalRedirects : Natural
    {
        public NaturalRedirects() : base()
        {
            this.Redirects = new RedirectRule[]
            {
                new RedirectRule(NaturalOpen.Open, Role(PositionRole.Opener, 1)),
                new RedirectRule(NaturalOvercall.Overcall, Role(PositionRole.Overcaller, 1)),
                new RedirectRule(NaturalRespond.Respond, Role(PositionRole.Responder, 1))
            };
        }
    }
}
