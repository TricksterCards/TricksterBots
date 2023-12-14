using System;
using System.Collections.Generic;


namespace BridgeBidding
{
	internal class ForcedBid
	{
		// TODO: Where should we check position state to see if the forced bids are necessary?  Here?  
		public static IEnumerable<BidRule> Bids(PositionState ps)
		{
			return new BidRule[] {
			};
		}
	}
}
