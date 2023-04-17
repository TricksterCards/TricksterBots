using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public enum CallType { Pass, Bid, Double, Redouble, NotActed }

	public struct Bid : IEquatable<Bid>
	{
		public int? Level { get; }
		public Suit? Suit { get; }

		public BidConvention Convention { get; }
		public CallType CallType { get; }

		public BidMessage Message { get; }

		public bool Is(int level, Suit suit)
		{
			return CallType == CallType.Bid && Level == level && Suit == suit;
		}

		public bool Equals(Bid other)
		{
			return (CallType == other.CallType &&  Level == other.Level && Suit == other.Suit && Convention == other.Convention);
		}

		public bool IsBid
		{
			get { return CallType == CallType.Bid; }
		}
		public bool IsPass
		{
			get { return CallType == CallType.Pass; }
		}

		public bool HasActed => CallType != CallType.NotActed;

		public Suit SuitIfNot(Suit? suit)
		{
			return (suit == null) ? (Suit)Suit : (Suit)suit;
		}

		public Bid(CallType callType, BidConvention convention = BidConvention.None, BidMessage message = BidMessage.Invitational)
		{
			Debug.Assert(callType != CallType.Bid);
			this.CallType = callType;
			this.Level = null;
			this.Suit = null;
			this.Convention = convention; 
			this.Message = message;
		}

		public Bid(int level, Suit suit, BidConvention convention = BidConvention.None, BidMessage message = BidMessage.Invitational)
		{
			this.CallType = CallType.Bid;
			Debug.Assert(level >= 1 && level <= 7);
			this.Level = level;
			this.Suit = suit;
			this.Convention = convention;
			this.Message = message;
		}	
	}


	public class BidRuleGroup
	{
		public Bid Bid { get; }
		public int Priority { get; private set; }
		private List<BidRule> _rules;
		public BidRuleGroup(Bid bid) : base()
		{
			Bid = bid;
			_rules = new List<BidRule>();
			Priority = int.MinValue;		// TODO: Is this right???
		}

		public bool IsConformingChoice
		{
			get { return Priority > int.MinValue;  }
		}



		public void Add(BidRule rule)
		{
			// TODO: Think about this a lot -- different conventions, different forcing, etc.  Seems like basic
			// rules of equality are just the basic stuff.  I think that is what Equals does...
			if (rule.Bid.Equals(Bid))
			{
				_rules.Append(rule);
			}
			else
			{
				throw new ArgumentException("Bid must match group bid");
			}
		}
		//
		// This method makes sure that any rules that do not apply are removed.  If there are no rules that could apply
		// then the priority.
		public void Prune(HandSummary handSummary, PositionState positionState)
		{
			var conforming = new List<BidRule>();
			int priority = int.MinValue;
			foreach (BidRule rule in _rules)
			{
				if (handSummary != null && rule.Conforms(handSummary, positionState))
				{
					priority = Math.Max(priority, rule.Priority);
					conforming.Append(rule);
				}
				else if (rule.Conforms(positionState.HandSummary, positionState))
				{
					conforming.Append(rule);
				}
			}
			this._rules = conforming;
			this.Priority = priority;
		}

		// Returns true if any rule has been removed.  In this case, the state needs to be updated again, and this
		// method needs to be called again.
		public bool PruneRules(PositionState positionState)
		{
			var rules = new List<BidRule>();
			foreach (BidRule rule in _rules)
			{
				if (rule.Conforms(positionState.HandSummary, positionState))
				{
					rules.Add(rule);
				}
			}
			if (rules.Count == _rules.Count) { return false; }
			_rules = rules;
			return true;
		}

		// Returns true IFF the shown state is modified.
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

	}



	class BidChoices : Dictionary<Bid, BidRuleGroup>
	{
		public BidChoices() { }
		public void Add(IEnumerable<BidRule> rules)
		{ 
			foreach (var rule in rules)
			{
				var group = this.ContainsKey(rule.Bid) ? this[rule.Bid] : new BidRuleGroup(rule.Bid);
				group.Add(rule);
				this[group.Bid] = group;
			}
		}

	

		/*
		public List<BidRuleGroup> ConformingBids(Direction direction, HandSummary handSummary, BiddingSummary biddingSummary)
		{
			var groups = new List<BidRuleGroup>();
			foreach (BidRuleGroup group in this.Values)
			{
				if (group.Conforms(direction, handSummary, biddingSummary).)
			}
		}
		*/
	}

}
