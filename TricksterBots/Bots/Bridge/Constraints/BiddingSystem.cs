using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{
    public interface IBiddingSystem
    {
        BidChoices GetBidChoices(PositionState positionState);
    }
}
