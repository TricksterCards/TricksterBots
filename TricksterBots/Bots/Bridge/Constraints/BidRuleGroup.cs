using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{

    public class BidRuleGroup
    {
        public Bid Bid { get; }
        public int Priority { get; private set; }
        private List<BidRule> _rules;
        public BidRuleGroup(Bid bid) : base()
        {
            Bid = bid;
            _rules = new List<BidRule>();
            Priority = int.MinValue;        // TODO: Is this right???
        }

        public bool IsConformingChoice
        {
            get { return Priority > int.MinValue; }
        }


        public static Dictionary<Bid, BidRuleGroup> BidsGrouped(IEnumerable<BidRule> rules)
        {
            var dict = new Dictionary<Bid, BidRuleGroup>();
            foreach (var rule in rules)
            {
                if (dict.ContainsKey(rule.Bid) == false)
                {
                    dict[rule.Bid] = new BidRuleGroup(rule.Bid);
                }
                dict[rule.Bid].Add(rule);
            }
            return dict;
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
