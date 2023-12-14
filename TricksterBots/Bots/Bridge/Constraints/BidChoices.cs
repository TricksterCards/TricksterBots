using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using static BridgeBidding.BidRule;

namespace BridgeBidding
{

    public delegate IEnumerable<BidRule> BidRulesFactory(PositionState positionState);


    public class PartnerChoicesXXX
    {
        private SortedList<Call, BidChoicesFactory> _choices;

        public PartnerChoicesXXX()
        {
            _choices = new SortedList<Call, BidChoicesFactory>();
        }

        public void AddFactory(Call goodThrough, BidChoicesFactory partnerFactory)
        {
            _choices.Add(goodThrough, partnerFactory);
        }

        public BidChoicesFactory GetPartnerBidsFactory(PositionState ps)
        {
            var lhoBid = ps.LeftHandOpponent.GetBidHistory(0);
            foreach (KeyValuePair<Call, BidChoicesFactory> choice in _choices)
            {
                if (choice.Key.CompareTo(lhoBid) >= 0) return choice.Value;
            }
            return null;
        }


        public void Merge(PartnerChoicesXXX other)
        {
            if (_choices.Count == 0)
            {
                _choices = other._choices;  // TODO: Should I copy this???
            }
            else
            {
                Call highestGoodThrough = _choices.Last().Key;
                foreach (KeyValuePair<Call, BidChoicesFactory> choice in other._choices)
                {
                    if (highestGoodThrough.CompareTo(choice.Key) < 0)
                    {
                        AddFactory(choice.Key, choice.Value);
                    }
                }
            }
        }
    }
    /*
    public class ChoicesFromRules
    {
        public BidChoicesFactory Factory { get { return this.Choices; } }
        private BidRulesFactory _rulesFactory;
        public ChoicesFromRules(BidRulesFactory rulesFactory)
        {
            _rulesFactory = rulesFactory;
        }
        private BidChoices Choices(PositionState ps)
        {
           return new BidChoices(ps, _rulesFactory);
        }
    }
    */

    public delegate BidChoices BidChoicesFactory(PositionState ps);
    public class BidChoices
    {
        public Call BestCall { get; private set; }
        private PositionState _ps;

        private Dictionary<Call, BidRuleSet> _choices;
        public PartnerChoicesXXX DefaultPartnerBids { get; private set; }
 
        public BidChoices(PositionState ps)
        {
            BestCall = null;
            _ps = ps;
            _choices = new Dictionary<Call, BidRuleSet>();
            DefaultPartnerBids = new PartnerChoicesXXX();
        }

        public BidRuleSet GetBidRuleSet(Call call)
        {
            if (_choices.ContainsKey(call))
            {
                return _choices[call];
            }
            var bogusRule = new BidRule(call, BidForce.Nonforcing, new Constraint[0]);
            var choice = new BidRuleSet(call, bogusRule.Force);
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

            var added = new HashSet<Call>();
            var groupDefaultPartnerBids = new PartnerChoicesXXX();
            foreach (var rule in rules)
            {
                if (rule.Call == null)
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
                    if (_ps.IsValidNextCall(rule.Call))
                    {
                        if (rule.SatisifiesStaticConstraints(_ps))
                        {
                            if (!_choices.ContainsKey(rule.Call))
                            {
                                _choices[rule.Call] = new BidRuleSet(rule.Call, rule.Force); ;
                                added.Add(rule.Call);
                            }
                            if (added.Contains(rule.Call))
                            {
                                // TODO: IS THIS CORRECT.  SEEMS LIKE ALL BIDS MUST HAVE THE SAME FORCE IF THEY 
                                // ARE GOING TO BE ADDDED.  THIS MEANS THAT THE RULESET MUST HAVE THE SAME FORCE
                                // AS THE NEW RULE OR ELSE THAT RULE IS ELEMINATED (kind of like a static constraint)
                                if (true) //(rule.Force == _choices[rule.Call].BidForce ||
                                    //(rule.Force != BidRule.BidForce.Forcing && _choices[rule.Call].BidForce != BidRule.BidForce.Forcing))
                                {
                                    _choices[rule.Call].AddRule(rule);
                                    if (BestCall == null && !(rule is PartnerBidRule) && _ps.PrivateHandConforms(rule))
                                    {
                                        BestCall = rule.Call;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (var call in added)
            {
                // Add default rules for any call other than Pass.  Pass must have
                // explicit rules or it defaults to a null factory (default behavior).
                if (_choices[call].HasRules)
                {
                    if (!call.Equals(Call.Pass))
                    {
                        _choices[call].MergePartnerChoices(groupDefaultPartnerBids);
                        _choices[call].MergePartnerChoices(DefaultPartnerBids);
                    }
                }
                else
                {
                    _choices.Remove(call);
                }
            }
        }

    }
}
