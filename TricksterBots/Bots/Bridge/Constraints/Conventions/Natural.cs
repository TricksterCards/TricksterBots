using System;
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

   //     public static PrescribedBids DefaultBidderXXX()
   //     {
   //         var bidder = new Natural();
   //         return new PrescribedBids(bidder, bidder.Initiate);
   //     }

    

        public (int, int) Open1Suit = (13, 21);
        public (int, int) Open1NT = (15, 17);
        public (int, int) Open2Suit = (5, 12);  // AG Bid card says 5-10 but when you count length you get 11 or 12
        public (int, int) Open2NT = (20, 21);
        public (int, int) Open3NT = (25, 27);
        public (int, int) OpenStrong = (22, 100);
        public (int, int) LessThanOpen = (0, 12);
        public (int, int) OpenerRebid1NT = (13, 14);
        public (int, int) OpenerRebid2NT = (18, 19);
        public (int, int) MinimumOpener = (13, 16);
        public (int, int) MediumOpener = (17, 18);
        public (int, int) MaximumOpener = (19, 21);
    


        // TODO: This is not a great name.  Not exactly right.  Fix later.....
        public (int, int) LessThanOvercall = (0, 17);
        public (int, int) Overcall1Level = (7, 17);
        public (int, int) OvercallStrong2Level = (13, 17);
        public (int, int) OvercallWeak2Level = (7, 11);
        public (int, int) OvercallWeak3Level = (7, 11);

        public (int, int) AdvanceNewSuit1Level = (6, 40); // TODO: Highest level for this?
        public (int, int) AdvanceNewSuit2Level = (11, 40); // Same here...
        public (int, int) AdvanceTo1NT = (6, 10);
        public (int, int) AdvanceWeakJumpRaise = (0, 11);   // TODO: What is the high end of jump raise weak
        public (int, int) AdvanceRaise = (6, 9);
        public (int, int) AdvanceCuebid = (10, 40);

        private PrescribedBids GetBids()
        {
            var pb = new PrescribedBids();
            pb.Redirect(StandardAmericanOpenRespond.Open, Role(PositionRole.Opener, 1));
            pb.Redirect(StandardAmericanOvercallAdvance.GetBids, Role(PositionRole.Overcaller, 1));
            return pb;
        }
    }
}
