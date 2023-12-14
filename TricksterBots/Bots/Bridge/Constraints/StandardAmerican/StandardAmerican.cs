using System;
using System.Collections.Generic;

namespace BridgeBidding
{

    public class StandardAmerican : Bidder, IBiddingSystem
    {

        public BidChoices GetBidChoices(PositionState ps)
        {
            if (ps.Role == PositionRole.Opener && ps.RoleRound == 1)
            {
                return Open.GetBidChoices(ps);
            }
            else if (ps.Role == PositionRole.Overcaller && ps.RoleRound == 1)
            {
                return Overcall.GetBidChoices(ps);
            }
            else
            {
                return new BidChoices(ps, Compete.CompBids);
            }
        }





        // TODO: This is not a great name.  Not exactly right.  Fix later.....
        public static (int, int) LessThanOvercall = (0, 17);
        public static (int, int) Overcall1Level = (7, 17);
        public static (int, int) OvercallStrong2Level = (13, 17);
        public static (int, int) OvercallWeak2Level = (7, 11);
        public static (int, int) OvercallWeak3Level = (7, 11);




    }
}
