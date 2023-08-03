using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static TricksterBots.Bots.Bridge.BidRule;

namespace TricksterBots.Bots.Bridge
{

    public class PartnerChoicesXXX
    {
        private SortedList<Bid, BidChoicesFactory> _choices;

        public PartnerChoicesXXX()
        {
            _choices = new SortedList<Bid, BidChoicesFactory>();
        }

        public void AddFactory(Bid goodThrough, BidChoicesFactory partnerFactory)
        {
            _choices.Add(goodThrough, partnerFactory);
        }

        public BidChoicesFactory GetPartnerBidsFactory(PositionState ps)
        {
            var lhoBid = ps.LeftHandOpponent.GetBidHistory(0);
            foreach (KeyValuePair<Bid, BidChoicesFactory> choice in _choices)
            {
                if (choice.Key.CompareTo(lhoBid) <= 0) return choice.Value;
            }
            return null;
        }


        public void Merge(PartnerChoicesXXX other)
        {
            Bid highestGoodThrough = _choices.Count == 0 ? Bid.Null : _choices.Last().Key;
            foreach (KeyValuePair<Bid, BidChoicesFactory> choice in other._choices)
            {
                if (highestGoodThrough.CompareTo(choice.Key) < 0)
                {
                    AddFactory(choice.Key, choice.Value);
                }
            }
        }
    }

    public class ChoicesFromBids
    {
        public BidChoicesFactory Factory { get { return this.Choices; } }
        private BidRulesFactory _rulesFactory;
        public ChoicesFromBids(BidRulesFactory rulesFactory)
        {
            _rulesFactory = rulesFactory;
        }
        private BidChoices Choices(PositionState ps)
        {
           return new BidChoices(ps, _rulesFactory);
        }
    }

    public delegate BidChoices BidChoicesFactory(PositionState ps);
    public class BidChoices
    {
        public Bid BestBid { get; private set; }
        private PositionState _ps;

        private Dictionary<Bid, BidRuleSet> _choices;
        public PartnerChoicesXXX DefaultPartnerBids { get; private set; }
 
        public BidChoices(PositionState ps)
        {
            BestBid = Bid.Null;
            _ps = ps;
            _choices = new Dictionary<Bid, BidRuleSet>();
            DefaultPartnerBids = new PartnerChoicesXXX();
            DefaultPartnerBids.AddFactory(new Bid(7, Trickster.cloud.Suit.Unknown), new ChoicesFromBids(Compete.CompBids).Factory);
        }

        public BidRuleSet GetBidRuleSet(Bid bid)
        {
            if (_choices.ContainsKey(bid))
            {
                return _choices[bid];
            }
            Debug.Assert(!bid.Equals(Bid.Null));
            var bogusRule = new BidRule(bid, BidForce.Nonforcing, new Constraint[0]);
            var choice = new BidRuleSet(bid);
            choice.AddRule(bogusRule);
            return choice;
        }

        public BidChoices(PositionState ps, BidRulesFactory rulesFactory) : this(ps)
        {
            AddRules(rulesFactory);
        }

        public void AddRules(BidRulesFactory factory)
        {
            AddRules(factory(_ps));
        }



        public void AddRules(IEnumerable<BidRule> rules)
        {

            var added = new HashSet<Bid>();
            var groupDefaultPartnerBids = new PartnerChoicesXXX();
            foreach (var rule in rules)
            {
                if (rule.Bid.Equals(Bid.Null))
                {
                    if (rule is PartnerBidRule partnerBids)
                    {
                        if (rule.SatisifiesStaticConstraints(_ps))
                        {
                            groupDefaultPartnerBids.AddFactory(partnerBids.GoodThrough, partnerBids.PartnerBidFactory);
                        }
                    }
                    else
                    {
                        Debug.Fail("Rules for Null bid must be default bid factory rules only");
                    }

                }
                else
                {
                    if (_ps.IsValidNextBid(rule.Bid))
                    {
                        if (rule.SatisifiesStaticConstraints(_ps))
                        {
                            if (!_choices.ContainsKey(rule.Bid))
                            {
                                _choices[rule.Bid] = new BidRuleSet(rule.Bid); ;
                                added.Add(rule.Bid);
                            }
                            if (added.Contains(rule.Bid))
                            {
                                _choices[rule.Bid].AddRule(rule);
                                if (BestBid.Equals(Bid.Null) && !(rule is PartnerBidRule) && _ps.PrivateHandConforms(rule))
                                {
                                    BestBid = rule.Bid;
                                }
                            }
                        }
                    }
                }
            }
            foreach (var bid in added)
            {
                if (_choices[bid].HasRules)
                {
                    _choices[bid].MergePartnerChoices(groupDefaultPartnerBids);
                    _choices[bid].MergePartnerChoices(DefaultPartnerBids);
                }
                else
                {
                    _choices.Remove(bid);
                }
            }
        }

    }
}
