using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		private int _bidIndex;
		private CallType _callType;
		private Suit? _suit;
		private int _level;
		private bool _compareSuit;
		private bool _desiredValue;


		// If you just want to see if the last action was a bid then pass level = 0 and CallType.Bid
		// If level == 0 then level will be ignored.  If compareSuit is false then suit will be ignored, otherwise
		// if compareSuit is true and suit == null then the suit of the bid will be used.
		// bidIndex specifies how far back in history to look for the bid.  0 is the last bid made by
		// the position.  1 indicates 1 bid back in history.
		public BidHistory(int bidIndex, CallType callType, int level, bool compareSuit, Suit? suit, bool desiredValue)
		{
			Debug.Assert(level >= 0 && level <= 7);
			this._bidIndex = bidIndex;
			this._callType = callType;
			this._level = level;
			this._suit = suit;
			this._compareSuit = compareSuit;
			this._desiredValue = desiredValue;

			this.OnceAndDone = true;
		}

		public override bool Conforms(Bid bid, PositionState ps, HandSummary hs, PairAgreements pa) 
		{
			var previousBid = ps.GetBidHistory(_bidIndex);
			if (_callType != CallType.Bid)
			{
				return ((previousBid.CallType == _callType) == _desiredValue);
			}
			if (previousBid.CallType == CallType.Bid && 
				(_compareSuit == false || previousBid.Suit == bid.SuitIfNot(_suit)) &&
				(_level == 0 || _level == previousBid.Level))
			{
				return _desiredValue;
			}
			return !_desiredValue;
		}
	}

}
