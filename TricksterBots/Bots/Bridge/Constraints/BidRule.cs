using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{


	public class BidRule
	{
		public Bid Bid { get; }
		public int Priority { get; }

		private Constraint[] _constraints;
		public BidRule(Bid bid, int priority, params Constraint[] constraints)
		{
			this.Bid = bid;
			this.Priority = priority;
			this._constraints = constraints;
		}


		public bool Conforms(HandSummary handSummary, PositionState positionState)
		{
			foreach (Constraint constraint in _constraints)
			{
				if (!constraint.Conforms(Bid, handSummary, positionState)) { return false; }
			}
			return true;
		}


		public void ShowState(ModifiableHandSummary handSummary, ModifiablePositionState positionState)
		{
			foreach (Constraint constraint in _constraints)
			{
				if (constraint is IShowsState showsState)
				{
					showsState.UpdateState(Bid, handSummary, positionState);
				}
			} 
		}
	}


}
