using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{
	public class CompositeConstraint : Constraint
	{
		public enum Operation { And, Or };

		protected Operation _operation;
		protected Constraint[] _constraints;
		public CompositeConstraint(Operation opertaion, params Constraint[] constraints)
		{
			Debug.Assert(constraints != null);
			this._operation = opertaion;
			this._constraints = constraints;
			// Note that an empty set of contraints is allowed.  In this case it will
			// always returns false from Conforms method no matter which logical opertation.
			if (constraints.Length == 0)
			{
				this.OnceAndDone = true;
			}
			else
			{
				this.OnceAndDone = constraints[0].OnceAndDone;
#if DEBUG
				int cShowsState = 0;
				foreach (Constraint c in constraints)
				{
					Debug.Assert(c.OnceAndDone == this.OnceAndDone);
					if (c is IShowsState _)
					{
						//Debug.Assert(opertaion == Operation.And);
						cShowsState += 1;
					}
				}
				Debug.Assert(cShowsState == 0 || cShowsState == _constraints.Length);
#endif
			}

		}

		public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
		{
			if (_constraints.Length == 0) { return false; }
			foreach (Constraint c in _constraints)
			{
				if (c.Conforms(bid, ps, hs))
				{
					if (_operation == Operation.Or) { return true; }
				}
				else
				{
					if (_operation == Operation.And) { return false; }
				}
			}
			return (_operation == Operation.And);
		}
	}

	public class CompositeShowsState : CompositeConstraint, IShowsState
	{
		public CompositeShowsState(Operation operation, params Constraint[] constraints) : base (operation, constraints)
		{
			// TODO: Perhaps this should throw.  Using OR and showing state is a bad bad bad idea....
			//Debug.Assert(operation == Operation.And);
		}
		void IShowsState.ShowState(Bid bid, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
		{
			if (_operation == Operation.And)
			{
				foreach (Constraint c in _constraints)
				{
					if (c is IShowsState cShowsState)
					{
						cShowsState.ShowState(bid, ps, showHand, showAgreements);
					}
				}
			}
			else
			{
				Debug.Assert(_operation == Operation.Or);
                // In the case of OR, only one of the contraints might have conformed so we need to check it out once
                // more before calling Update *AND* we want to union the states and only show common properties of the
                // shown states.
                HandSummary.ShowState combinedHand = null;
                PairAgreements.ShowState combinedAgreements = null;
                foreach (Constraint c in _constraints)
                {
                    if (c is IShowsState cShowsState)
                    {
						if (c.Conforms(bid, ps, ps.PublicHandSummary))
						{
							var showThisHand = new HandSummary.ShowState();
							var showThisAgreements = new PairAgreements.ShowState();
							cShowsState.ShowState(bid, ps, showThisHand, showThisAgreements);
							if (combinedHand == null)
							{
								combinedHand = showThisHand;
								combinedAgreements = showThisAgreements;
							} 
							else
							{
								combinedHand.Combine(showThisHand.HandSummary, State.CombineRule.CommonOnly);
								combinedAgreements.Combine(showThisAgreements.PairAgreements, State.CombineRule.CommonOnly);
							}
							// TODO: Now what?  Merge?  Merge with other "ORs"?  Then "Show"....
						}
                    }
                }
				if (combinedHand != null)
				{
					Debug.Assert(combinedAgreements != null);
					showHand.Combine(combinedHand.HandSummary, State.CombineRule.Show);
					showAgreements.Combine(combinedAgreements.PairAgreements, State.CombineRule.Show);
				}
            }
		}
	}

}
