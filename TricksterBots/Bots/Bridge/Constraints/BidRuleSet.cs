using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{

    public class BidRuleSet
    {
		protected class RuleInfo
		{
			public RuleInfo(int index)
			{
				this.RuleIndex = index;
				this.HandSummary = null;
				this.PairAgreements = null;
			}
			public int		RuleIndex;
			public HandSummary HandSummary;
			public PairAgreements PairAgreements;
		}
        public Bid Bid { get; }

        public PrescribedBids PrescribedBids { get; private set; } 

        private List<RuleInfo> _ruleInfo = new List<RuleInfo>();

        public bool HasRules {  get {  return _ruleInfo.Count > 0; } }

       
        public BidRuleSet(Bid bid, PrescribedBids pb) 
        {
            this.Bid = bid;
            this.PrescribedBids = pb;
            this._ruleInfo = new List<RuleInfo>();
        }

		public void AddRuleIndex(int i)
		{
			_ruleInfo.Add(new RuleInfo(i));
			// TODO: All bids should have the same force...
			// Error if they don't since "once and done" should have
			// parred them down to the approriate set of all same force...
		}


		protected BidRule RuleFor(RuleInfo ri)
		{
			return PrescribedBids.Bids[ri.RuleIndex];
		}

        //
        // This method makes sure that any rules that do not apply are removed.  If there are no rules that could apply
        // then the priority.
        /*
		public void Prune(HandSummary handSummary, PositionState positionState)
		{
			var conforming = new List<BidRule>();
			int priority = int.MinValue;
			foreach (BidRule rule in _rules)
			{
				if (handSummary != null && rule.Conforms(positionState, true))
				{
					priority = Math.Max(priority, rule.Priority);
					conforming.Append(rule);
				}
				else if (rule.Conforms(positionState, false))
				{
					conforming.Append(rule);
				}
			}
			this._rules = conforming;
			this.Priority = priority;
		}
		*/


        // Returns true if at least one rule conforms in this group.
        public bool Conforms(PositionState ps, HandSummary hs)
        {
            foreach (var ri in _ruleInfo)
            {
                if (RuleFor(ri).Conforms(false, ps, hs)) { return true; }
            }
            return false;
        }

        public (HandSummary, PairAgreements) ShowState(PositionState ps)
        {
			// TODO: This is a hack. Need to understand what's going on here.  But for now if empty rules
			// just return the current state...
			if (!HasRules) { return (ps.PublicHandSummary, ps.PairAgreements); }

            var showHand = new HandSummary.ShowState();
            var showAgreements = new PairAgreements.ShowState();
            bool firstRule = true;
            foreach (var ri in _ruleInfo)
            {
                (HandSummary hs, PairAgreements pa) newState = RuleFor(ri).ShowState(ps);
				ri.HandSummary = newState.hs;
				ri.PairAgreements = newState.pa;	
				// TODO: This is right to save the state, but needs to be used later WITHOUT calling show.
                showHand.Combine(newState.hs, firstRule ? State.CombineRule.Show : State.CombineRule.CommonOnly);
                showAgreements.Combine(newState.pa, firstRule ? State.CombineRule.Show : State.CombineRule.CommonOnly);
                firstRule = false;
            }
            // After all of the possible shapes of suits have been unioned we can trim the max length of suits
           // TODO: NEED TO CALL ON HAND EVEALUATOR HERE... handSummary.TrimShape();
            return (showHand.HandSummary, showAgreements.PairAgreements);
        }


		public bool PruneRules(PositionState ps)
		{
			var rules = new List<BidRule>();
			foreach (var ri in _ruleInfo)
			{
				if (RuleFor(ri).Conforms(false, ps, ps.PublicHandSummary))
				{
					rules.Add(rule);
				}
			}
			if (rules.Count == _rules.Count) { return false; }
			_rules = rules;
			return true;
		}

        
        /*
		public bool UpdateShownState(Direction direction, PairAgreements PairAgreements)
		{
			// TODO: Start with existing shown state
			// Then create a new shownState object
			ShownState compositeShown = new ShownState();
			bool isFirstRule = true;
			foreach (var rule in _rules)
			{
				var ruleShows = rule.ShownState(direction, PairAgreements);
				if (isFirstRule)
				{
					compositeShown = ruleShows;
					isFirstRule = false;
				} 
				else
				{
					compositeShown.Union(ruleShows);
				}
			}
			return false;	// TODO:   THIS IS ABSOLUTELY BUSTED -- NEED TO COMPARE TO LAST STATE KNOWN....

		}
		*/

    }

}
