using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BridgeBidding
{
    public interface IBiddingSystem
    {
        BidChoices GetBidChoices(PositionState positionState);
    }
}
