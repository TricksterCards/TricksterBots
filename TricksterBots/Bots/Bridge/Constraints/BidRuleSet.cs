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
        public Bid Bid { get; }

        public Convention Convention { get; }

        public PrescribedBidsFactory PartnerRules { get; }
        public int Priority { get; private set; }

        public bool HasRules {  get {  return _rules.Count > 0; } }

        private List<BidRule> _rules;
        public BidRuleSet(Bid bid, Convention convention, PrescribedBidsFactory partnerRules) 
        {
            this.Bid = bid;
            this._rules = new List<BidRule>();
            this.Priority = int.MinValue;        // TODO: Is this right???
            this.PartnerRules = partnerRules;
            this.Convention = convention;
        }



        public void Add(BidRule rule)
        {
            // TODO: Think about this a lot -- different conventions, different forcing, etc.  Seems like basic
            // rules of equality are just the basic stuff.  I think that is what Equals does...
            if (rule.Bid.Equals(Bid))
            {
                _rules.Add(rule);
                if (rule.Priority > this.Priority) { this.Priority = rule.Priority; }
            }
            else
            {
                throw new ArgumentException("Bid must match group bid");
            }
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
        public bool Conforms(PositionState ps, HandSummary hs, PairAgreements pa)
        {
            foreach (var rule in _rules)
            {
                if (rule.Conforms(false, ps, hs, pa)) { return true; }
            }
            return false;
        }

        public (HandSummary, PairAgreements) ShowState(PositionState ps)
        {
            // TODO: This is a hack. Need to understand what's going on here.  But for now if empty rules
            // just return the current state...
            if (_rules.Count == 0) { return (ps.PublicHandSummary, ps.PairAgreements); }

            HandSummary handSummary = null;
            PairAgreements PairAgreements = null;
            foreach (var rule in _rules)
            {
                (HandSummary hs, PairAgreements pa) newState = rule.ShowState(ps);
                if (handSummary == null)
                {
                    handSummary = newState.hs;
                    PairAgreements = newState.pa;
                }
                else
                {
                    handSummary.Union(newState.hs);
                    PairAgreements.Union(newState.pa);
                }
            }
            // After all of the possible shapes of suits have been unioned we can trim the max length of suits
            handSummary.TrimShape();
            return (handSummary, PairAgreements);
        }


		public bool PruneRules(PositionState ps)
		{
			var rules = new List<BidRule>();
			foreach (BidRule rule in _rules)
			{
				if (rule.Conforms(false, ps, ps.PublicHandSummary, ps.PairAgreements))
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
