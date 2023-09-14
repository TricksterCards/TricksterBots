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
	public class BidHistory : StaticConstraint
	{
		private int _bidIndex;
		private Call _call;
	//	private Suit? _suit;
	//	private int _level;
	//	private bool _compareSuit;
		private bool _desiredValue;


		// If call is null then this class will return true if the spcified call
		// is the same suit as the previous bid.
		// If call is non-null then the calls must be equal
		public BidHistory(int bidIndex, Call call, bool desiredValue)
		{
			this._bidIndex = bidIndex;
			this._call = call;
			this._desiredValue = desiredValue;

		}

		public override bool Conforms(Call call, PositionState ps) 
		{
			var previousCall = ps.GetBidHistory(_bidIndex);
			if (previousCall != null)
			{
				if (_call != null)
				{
					return previousCall.Equals(_call) == _desiredValue;
				}
				if (call is Bid bid && previousCall is Bid previousBid)
				{
					return (bid.Suit == previousBid.Suit) == _desiredValue;
				}
			}
			return !_desiredValue;
		}
	}

}
