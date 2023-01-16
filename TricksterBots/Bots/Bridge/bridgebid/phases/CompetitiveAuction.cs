using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;

namespace TricksterBots.Bots
{
    internal class CompetitiveAuction 
    {
        public static void HandleInterference(InterpretedBid call)
        {
            return;
        }

        public static void PassOrCompete(InterpretedBid call)
        {
            // TODO: At this point, some contract should be agreed on - NT or some suit.  If opponents
            // bid then this code should either:  Bid contract at higher level, pass, or double for penalty.
            // Could also double for penalty/takeout so partner could decide to leave double in (convert).
            return;
        }
 
    }
}
