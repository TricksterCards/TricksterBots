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

    public class BidOptions
    {

        internal class BidGroupChoices
        {
            public BidRuleGroup Natural;
            public BidRuleGroup Conventional;
            public BidGroupChoices()
            {
                Natural = null;
                Conventional = null;
            }

            // Unconditionally add the rule to the appropriate convention group 
            internal void Add(Convention convention, BidderFactory nextState, BidRule rule)
            {
                if (convention == Convention.Natural)
                {
                    if (this.Natural == null)
                    {
                        this.Natural = new BidRuleGroup(rule.Bid, convention, nextState);
                    }
                    this.Natural.Add(rule);
                } 
                else
                {
                    if (this.Conventional == null)
                    {
                        this.Conventional = new BidRuleGroup(rule.Bid, convention, nextState);
                    }
                    Debug.Assert(this.Conventional.Convention == convention);
                    // TODO: Make sure bidding factory is the same...
                    this.Conventional.Add(rule);
                }
            }

            internal BidRuleGroup BidRules {
                get 
                { 
                    if (Conventional != null && Conventional.HasRules) { return Conventional; }
                    if (Natural != null && Natural.HasRules) { return Natural;  }
                    return null;
                }
            }
        }


        private Dictionary<Bid, BidGroupChoices> Choices { get; }
        public BidOptions() { 
            this.Choices = new Dictionary<Bid, BidGroupChoices>();
        }

        public void Add(Convention convention, BidderFactory nextState, IEnumerable<BidRule> rules, PositionState ps)
        {
            foreach (var rule in rules)
            {
                if (rule.Conforms(ps, ps.PublicHandSummary, ps.BiddingSummary))
                { 
                    if (Choices.ContainsKey(rule.Bid) == false)
                    {
                        Choices[rule.Bid] = new BidGroupChoices();
                    }
                    Choices[rule.Bid].Add(convention, nextState, rule);
                }
            }
        }

        public Dictionary<Bid, BidRuleGroup> GetChoices()
        {
            var choices = new Dictionary<Bid, BidRuleGroup>();
            foreach (var convChoice in Choices)
            {
                var rules = convChoice.Value.BidRules;
                if (rules != null) 
                {
                    choices[convChoice.Key] = rules;
                }
            }
            return choices;
        }
    }


    public class BidRuleGroup
    {
        public Bid Bid { get; }

        public Convention Convention { get; }

        public BidderFactory NextBidder { get; }
        public int Priority { get; private set; }

        public bool HasRules {  get {  return _rules.Count > 0; } }

        private List<BidRule> _rules;
        public BidRuleGroup(Bid bid, Convention convention, BidderFactory bidderFactory) 
        {
            this.Bid = bid;
            this._rules = new List<BidRule>();
            this.Priority = int.MinValue;        // TODO: Is this right???
            this.NextBidder = bidderFactory;
            this.Convention = convention;
        }

        public bool IsConformingChoice
        {
            get { return Priority > int.MinValue; }
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
        public bool Conforms(PositionState ps, HandSummary hs, BiddingSummary bs)
        {
            foreach (var rule in _rules)
            {
                // TODO: Hack for now
                if (rule.Conforms(ps, hs, bs)) { return true; }
            }
            return false;
        }

        public (HandSummary, BiddingSummary) UpdateState(PositionState ps)
        {
            HandSummary handSummary = null;
            BiddingSummary biddingSummary = null;
            foreach (var rule in _rules)
            {
                var newState = rule.ShowState(ps);
                if (handSummary == null)
                {
                    handSummary = newState.Item1;
                    biddingSummary = newState.Item2;
                }
                else
                {
                    handSummary.Union(newState.Item1);
                    biddingSummary.Union(newState.Item2);
                }
            }
            // After all of the possible shapes of suits have been unioned we can trim the max length of suits
            handSummary.TrimShape();
            return (handSummary, biddingSummary);
        }

        // Returns true if any rule has been removed.  In this case, the state needs to be updated again, and this
        // method needs to be called again.
        /*
		public bool PruneRules(PositionState positionState)
		{
			var rules = new List<BidRule>();
			foreach (BidRule rule in _rules)
			{
				if (rule.Conforms(positionState, false))
				{
					rules.Add(rule);
				}
			}
			if (rules.Count == _rules.Count) { return false; }
			_rules = rules;
			return true;
		}
		*/
        // Returns true IFF the shown state is modified.

        /*
		public bool UpdateShownState(Direction direction, BiddingSummary biddingSummary)
		{
			// TODO: Start with existing shown state
			// Then create a new shownState object
			ShownState compositeShown = new ShownState();
			bool isFirstRule = true;
			foreach (var rule in _rules)
			{
				var ruleShows = rule.ShownState(direction, biddingSummary);
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
