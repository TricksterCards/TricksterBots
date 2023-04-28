using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{
	public class IsFlat : Constraint
	{
		protected bool _desiredValue;
		public IsFlat(bool desiredValue = true)
		{
			this._desiredValue = desiredValue;
		}

		public override bool Conforms(Bid bid, PositionState ps, HandSummary hs, PairAgreements pa)
		{
			return hs.IsFlat == null || hs.IsFlat == _desiredValue;
		}
	}

	public class ShowsFlat: IsFlat, IShowsState
	{
		public ShowsFlat(bool desiredValue = true) : base(desiredValue) { }
		public void Update(Bid bid, PositionState ps, HandSummary hs, PairAgreements pa)
		{
			hs.IsBalanced = _desiredValue;
		}
	}

}
