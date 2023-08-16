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



		public override bool Conforms(Call call, PositionState ps, HandSummary hs)
		{
			if (GetSuit(_better, call) is Suit better &&
				GetSuit(_worse, call) is Suit worse &&
				GetSuit(_defaultIfEqual, call) is Suit defaultIfEqual)
			{
				var betterShape = hs.Suits[better].GetShape();
				var worseShape = hs.Suits[worse].GetShape();
				if (betterShape.Max < worseShape.Min) { return false; }
				if (betterShape.Max == worseShape.Min && worse == defaultIfEqual) { return false; }
				if (!_lengthOnly && betterShape == worseShape)
				{
					int bq = (int)(hs.Suits[better].GetQuality().Min);
					int wq = (int)(hs.Suits[worse].GetQuality().Min);
					if (bq > wq) { return true; }
					if (wq > bq) { return false; }
				}
				return true;
			}
			Debug.Fail("One of three suits not specified in constraint");
			return false;
		}

	}

	public class ShowsBetterSuit : IsBetterSuit, IShowsState
	{
		public ShowsBetterSuit(Suit? better, Suit? worse, Suit? defaultIfEqual = null, bool lengthOnly = false) :
			base(better, worse, defaultIfEqual, lengthOnly)
		{ }

		// The worse suit can not be longer than thw better one, and the quality can not be higher, so all we can
		// do here is simply restrict the maximums for both shape and quality.
		void IShowsState.ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
		{
			if (GetSuit(_better, call) is Suit better &&
				ps.PublicHandSummary.Suits[better].Shape != null &&
				GetSuit(_worse, call) is Suit worse)
			{
				var betterShape = ps.PublicHandSummary.Suits[better].GetShape();
				var worseShape = ps.PublicHandSummary.Suits[worse].GetShape();
				showHand.Suits[worse].ShowShape(worseShape.Min, Math.Min(worseShape.Max, betterShape.Max));
				// TODO: Do fancy thing maxing out worse suit based on all other suit mins.  If Spades min = 5
				// then Hearts max is 6 if Spades > Hearts...

				// NOTE!  YOU CAN NOT DETERMINE ANYTHING ABOUT QUALITY UNLESS YOU ***KNOW*** THAT BOTH SUITS ARE EQUAL LENGTH

			}
		}
	}
}
