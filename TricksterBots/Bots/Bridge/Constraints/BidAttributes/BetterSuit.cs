using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public class IsBetterSuit : Constraint
	{
		protected Suit? _better;
		protected Suit? _worse;
		protected Suit? _defaultIfEqual;
		protected bool _lengthOnly;




		// Suit "better" must be better than suit "worse".  If lengthOnly is true then length is the only consideration
		// and the default value will be returned
		public IsBetterSuit(Suit? better, Suit? worse, Suit? defaultIfEqual = null, bool lengthOnly = false)
		{
			Debug.Assert(better != worse);
			Debug.Assert(defaultIfEqual == better || defaultIfEqual == worse);
			// TODO: More checks.  Should they be Assert or throw?
			this._better = better;
			this._worse = worse;
			this._defaultIfEqual = defaultIfEqual;
			this._lengthOnly = lengthOnly;
		}



		public override bool Conforms(Bid bid, PositionState ps, HandSummary hs, PairAgreements pa)
		{
			var better = bid.SuitIfNot(_better);
			var worse = bid.SuitIfNot(_worse);
			var betterShape = hs.Suits[better].Shape;
			var worseShape = hs.Suits[worse].Shape;
			var defaultIfEqual = bid.SuitIfNot(_defaultIfEqual);

			
			if (betterShape.Max < worseShape.Min) { return false; }
			if (betterShape.Max == worseShape.Min && worse == defaultIfEqual) { return false; }
			if (!_lengthOnly && betterShape == worseShape)
			{
				int bq = (int)(hs.Suits[better].Quality.Min);
				int wq = (int)(hs.Suits[worse].Quality.Min);
				if (bq > wq) { return true; }
				if (wq > bq) { return false;}
			}
			return true;
		}

	}

	public class ShowsBetterSuit : IsBetterSuit, IShowsState
	{
		public ShowsBetterSuit(Suit? better, Suit? worse, Suit? defaultIfEqual = null, bool lengthOnly = false) :
			base(better, worse, defaultIfEqual, lengthOnly)
		{ }

		// The worse suit can not be longer than thw better one, and the quality can not be higher, so all we can
		// do here is simply restrict the maximums for both shape and quality.
		public void Update(Bid bid, PositionState ps, HandSummary hs, PairAgreements pa)
		{
			var better = bid.SuitIfNot(_better);
			var worse = bid.SuitIfNot(_worse);
			var betterShape = hs.Suits[better].Shape;
			var worseShape = hs.Suits[worse].Shape;
			hs.Suits[worse].Shape = (worseShape.Min, Math.Min(worseShape.Max, betterShape.Max));
			if (!_lengthOnly)
			{
				var betterQuality = hs.Suits[better].Quality;
				var worseQuality = hs.Suits[worse].Quality;
				SuitQuality maxWorse = (SuitQuality)(Math.Min((int)betterQuality.Max, (int)worseQuality.Max));
				hs.Suits[worse].Quality = (worseQuality.Min, maxWorse);
			}
		}
	}
}
