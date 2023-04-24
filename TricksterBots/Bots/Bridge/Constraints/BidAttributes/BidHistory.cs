using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	// TODO: For now this just allows the last bid to be examined...  In the future may need to look back in
	// bid history.  But this works for now...
	public class BidHistory : Constraint
	{
		private CallType _callType;
		private Suit _suit;
		private int _level;
		private bool _desiredValue;


		// If you just want to see if the last action was a bid then pass level = 0 and CallType.Bid
		public BidHistory(CallType callType, int level, Suit suit, bool desiredValue)
		{
			this._callType = callType;
			this._level = level;
			this._suit = suit;
			this._desiredValue = desiredValue;
		}

		public override bool Conforms(Bid bid,PositionState ps, HandSummary hs, BiddingSummary bs) 
		{
			var lastBid = ps.LastBid;
			if (_callType != CallType.Bid || _level == 0)
			{
				return (lastBid.CallType == _callType) ? _desiredValue : !_desiredValue;
			}
			if (lastBid.CallType == CallType.Bid && lastBid.Suit == _suit &&
				_level == lastBid.Level)
			{
				return _desiredValue;
			}
			return !_desiredValue;
		}
	}

}
