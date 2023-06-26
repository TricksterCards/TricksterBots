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

    public class StandardAmerican : Bidder
    {

   //     public static PrescribedBids DefaultBidderXXX()
   //     {
   //         var bidder = new Natural();
   //         return new PrescribedBids(bidder, bidder.Initiate);
   //     }

    

        public static (int, int) Open1Suit = (13, 21);
        public static (int, int) Open1NT = (15, 17);
        public static (int, int) Open2Suit = (5, 12);  // AG Bid card says 5-10 but when you count length you get 11 or 12
        public static (int, int) Open2NT = (20, 21);
        public static (int, int) Open3NT = (25, 27);
        public static (int, int) OpenStrong = (22, 100);
        public static (int, int) LessThanOpen = (0, 12);
        public static (int, int) OpenerRebid1NT = (13, 14);
        public static (int, int) OpenerRebid2NT = (18, 19);
        public static (int, int) MinimumOpener = (13, 16);
        public static (int, int) MediumOpener = (17, 18);
        public static (int, int) MaximumOpener = (19, 21);
    


        // TODO: This is not a great name.  Not exactly right.  Fix later.....
        public static (int, int) LessThanOvercall = (0, 17);
        public static (int, int) Overcall1Level = (7, 17);
        public static (int, int) OvercallStrong2Level = (13, 17);
        public static (int, int) OvercallWeak2Level = (7, 11);
        public static (int, int) OvercallWeak3Level = (7, 11);

        public static (int, int) AdvanceNewSuit1Level = (6, 40); // TODO: Highest level for this?
        public static (int, int) AdvanceNewSuit2Level = (11, 40); // Same here...
        public static (int, int) AdvanceTo1NT = (6, 10);
        public static (int, int) AdvanceWeakJumpRaise = (0, 11);   // TODO: What is the high end of jump raise weak
        public static (int, int) AdvanceRaise = (6, 9);
        public static (int, int) AdvanceCuebid = (10, 40);


        // TODO: Perhaps move this to somewhere better.  For now, we 
        public static PrescribedBids DefaultBidsFactory()
        {
            var pb = new PrescribedBids();

            pb.Redirect(Open, Role(PositionRole.Opener, 1));
            pb.Redirect(Overcall, Role(PositionRole.Overcaller, 1));
            pb.Redirect(Compete.GetBids);
            return pb;
        }

        public static PrescribedBids Open()
        {
            var pb = new PrescribedBids();
            pb.Redirect(Strong2Clubs.Open);
            pb.Redirect(NoTrump.Open);
            pb.Redirect(StandardAmericanOpenRespond.OpenSuit);
            return pb;
        }

        private static PrescribedBids Overcall()
        {
            var pb = new PrescribedBids();
            pb.Redirect(StandardAmericanOvercallAdvance.Overcall);
            pb.Redirect(NoTrump.StrongOvercall);
            pb.Redirect(TakeoutDouble.InitiateConvention);
            pb.Redirect(NoTrump.BalancingOvercall);
           
            return pb;
        }
    }
}
