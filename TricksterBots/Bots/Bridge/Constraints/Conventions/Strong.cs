using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class Strong : Bidder
    {
        public static Bidder Bidder() => new StrongOpen();

        protected static (int, int) StrongOpenRange = (22, 40);
        protected static (int, int) PositiveResponse = (8, 18);
        protected static (int, int) Waiting = (0, 18);
        protected static (int, int) Rebid2NT = (22, 24);


        public Strong() : base(Convention.StrongOpen, 5000) { }

    }

    public class StrongOpen : Strong
    {
        public StrongOpen() : base()
        {
            this.ConventionRules = new ConventionRule[]
            {
                ConventionRule(Role(PositionRole.Opener, 1))
            };
            this.BidRules = new BidRule[]
            {
                Forcing(2, Suit.Clubs, Points(StrongOpenRange)),
            };
            this.NextConventionState = () => new StrongResponse();
        }

    }

    public class StrongResponse : Strong
    {
        public StrongResponse() : base()
        {
            this.BidRules = new BidRule[]
            {
                // TODO: Priorities for the positive bids, especially if balanced AND have a good suit...
                Forcing(2, Suit.Diamonds, Points(Waiting)),
                Forcing(2, Suit.Hearts, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(2, Suit.Spades, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(2, Suit.Unknown, Points(PositiveResponse), Balanced()),
                Forcing(3, Suit.Clubs, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(3, Suit.Diamonds, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
            };
            this.NextConventionState = () => new StrongRebid();
        }
    }
   
    public class StrongRebid : Strong
    {
        public StrongRebid() : base()
        {
     //       this.Redirects = new RedirectRule[]
      //      {
          //        new RedirectRule(() => new StrongRebidPositiveResponse(), LastBid(2, Suit.Diamonds, false))
        //    };
            this.BidRules = new BidRule[]
            {
                Forcing(2, Suit.Hearts, Shape(5, 11)),
                Forcing(2, Suit.Spades, Shape(5, 11)),
                Forcing(2, Suit.Unknown, Balanced(), Points(Rebid2NT)),
                Forcing(3, Suit.Clubs, Shape(5, 11)),
                Forcing(3, Suit.Diamonds, Shape(5, 11))
            };
            this.NextConventionState = () => new StrongResponderRebid();
        }
    }

    public class StrongResponderRebid: Strong
    {

    }

    public class StrongRebidPositiveResponse: Strong
    {

    }

}

