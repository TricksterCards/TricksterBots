using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;
using static Trickster.Bots.InterpretedBid;

namespace TricksterBots.Bots
{
    internal class PlayerDisclosures
    {
        public Range Points { get; }
        // suits shown so far and min/max
    }
    internal class PairDisclosures
    {
        public bool AgreedOnTrump = false;
        // fits[Suits] = bool;...
        public Range Points { get; }    // TODO: Compute these from playerDisclosures
        // Suits shown so far and count (combined)
    }
}
