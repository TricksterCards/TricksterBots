using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TricksterBots.Bots.Bridge;
using Trickster.cloud;
using System.Threading;
using System.Diagnostics;
using System.Data;


namespace TricksterBots.Bots.Bridge
{
    public delegate IEnumerable<BidRule> BidRulesFactory(PositionState positionState);


   // public delegate PrescribedBids PrescribedBidsFactory();
    //  public delegate void PrescribeBidRules(PrescribedBids prescribedBids);
}
/*
    public class PrescribedBids
    {
        // private Bidder _bidder;

        // public List<ConventionRule> ConventionRules { get; set; }


        public List<BidRule> BidRules { get; private set; }
       // public String Convention { get; private set; }


        private List<RedirectRule> _redirectRules = null;
        public PrescribedBidsFactory DefaultPartnerBidsFactory = null;
        //   private Dictionary<Bid, PrescribedBidsFactory> _partnerBids = null;

        public PrescribedBids()
        {
            this.BidRules = new List<BidRule>();
        }


        public PrescribedBids(PrescribedBidsFactory defaultPartnerBidsFactory, params BidRule[] initialRules)
        {
            this.BidRules = new List<BidRule>(initialRules);
            this.DefaultPartnerBidsFactory = defaultPartnerBidsFactory;
        }

      //  public PrescribedBids(params BidRule[] initialRules)
      //  {
      //      this.BidRules = new List<BidRule>(initialRules);
     //       this.Convention = null;
      //  }

        public void Redirect(PrescribedBidsFactory factory, params Constraint[] constraints)
        {
            if (_redirectRules == null)
            {
                _redirectRules = new List<RedirectRule>();
            }
            _redirectRules.Add(new RedirectRule(factory, constraints));
        }

        public void Redirect(PrescribedBidsFactory factory)
        {
            Redirect(factory, new Constraint[0]);
        }


        public void RedirectIfRhoInterfered(PrescribedBidsFactory factory)
        {
            Redirect(factory, Bidder.RHO(Bidder.Passed(false)));
        }

        public void RedirectIfRhoBid(PrescribedBidsFactory factory)
        {
            Redirect(factory, Bidder.RHO(Bidder.DidBid(false)));
        }


        public List<PrescribedBidsFactory> GetRedirects(PositionState ps)
        {
            List<PrescribedBidsFactory> redirects = null;
            if (_redirectRules != null)
            {
                foreach (var rule in _redirectRules)
                {
                    if (rule.Conforms(ps))
                    {
                        if (redirects == null)
                        {
                            redirects = new List<PrescribedBidsFactory>();
                        }
                        // If we hit a rule that has a null factory then we are done.  Return
                        // the list without any further redirects (may be an empty list!)
                        if (rule.PrescribedBidsFactory == null)
                        {
                            break;
                        }
                        redirects.Add(rule.PrescribedBidsFactory);
                    }
                }
            }
            return redirects;

        }

        /*
        public Dictionary<Bid, BidRuleSet> GetBids(PositionState ps)
        {
            // Step 1: Make sure this set of rules applies.  If not don't redirect or return any bid rules.
            //            if (ConventionRules != null)
            //           {
            //              foreach (var rule in ConventionRules)
            //              {
            //                  if (!rule.Conforms(ps)) { return null; }
            //              }
            //          }
            // Step 2: This set of prescribed bids applies so now we either redirect to one or more other
            // sets of prescribed bids OR we return our own set of rules.
            /*
                        if (_redirects != null)
                        {
                            RedirectGroupXXX rXXX = null;
                            foreach (var redirect in _redirectRules)
                            {
                                PrescribedBidsFactory bidFactory = redirect.RedirectedBidder(ps);
                                if (bidFactory != null)
                                {
                                    // NOTE: It is acceptable to return an empty set of rules.  This means that the 
                                    // redirection rule conforms (so bid rules in this set of Prescribed Bids will not be used)
                                    // but that there are no bids returned.  Basically the empty set of bids is ok, but stops
                                    // us from returning our bids.
                                    if (rXXX == null)
                                    {
                                        rXXX = new RedirectGroupXXX(); ;
                                    }
                                    rXXX.Add(bidFactory);
                                }
                            }
                            if (rXXX != null) { return rXXX.GetBids(ps); }
                        }
           
            // Step 3: Hooray!  We conform and did not need to be redirected.  Now add all confoming bids
            // to the result and return a list of groupled bid rules.
            //
            // This step involves more than just adding all the rules.  Any rule that does not conform based on 
            // "OnceAndDone" will be eleminated before being added to the set of choices.  This means that if ALL
            // rules for a particular bidder are eleminated due to things that are not hand related then any subsequent
            // rules by other bidders will apply.  If ANY rule applies for a given bid then NO SUBSEQUENT RULES will be
            // considered even if further constraints eleminate them.  So, for example, if a particular bid only exists
            // in 3rd seat, and the current position state is not 3rd seat then that bid (say 2C) would be defer to 
            // subsequent bidders.  If that bid passes the "OnceAndDone" test (is in 3rd seat) then 2C will be reserved
            // only for bids made by the first bidder.
            var brs = new Dictionary<Bid, BidRuleSet>();
            var contract = ps.BiddingState.GetContract();
            if (Bids != null)   // TODO: Why is this ever null when redirect doesnt happen?
            {
                for (int i = 0; i < Bids.Count; i++)
                {
                    var rule = Bids[i];
                    // TODO: Perhaps when we pass in "TRUE" to first-time we want to ONLY look at first time bids...

                    if (rule.Bid.IsValid(ps, contract).Valid &&
                        rule.SatisifiesStaticConstraints(ps))
                    {
                        var bid = rule.Bid;
                        if (!brs.ContainsKey(bid))
                        {
                            brs[bid] = new BidRuleSet(bid, this);
                        }
                        // Now we know that the rule conforms to the "Once and Done".  By creating a BidRuleSet
                        // we have "claimed" this bid.  Now we may eleminate it based on the hand summary.  Note
                        // that if there is a private hand summary and the rule conforms to that, it is a valid
                        // candidate even if it does not conform to the public
                        // TODO: Make sure this is the case everwhere - Prune, etc...
                        if (rule.Conforms(false, ps, ps.PublicHandSummary) || ps.PrivateHandConforms(rule))
                        {
                            brs[bid].AddRuleIndex(i);
                        }
                    }
                }
            }
            return brs;
        }
        */


        // This sets the default bidder for partner's next time to act.  This bidder will created and
        // invoked so long as the current bidder selects any bid other than Pass.  For setting specific
        // bidders for specific bids, including Pass, use SetPartnerBidder(Bid, BidderFactory).
        //      public void Partner(PrescribeBidRules partnerRules)
        //     {
        //         Partner(() => new PrescribedBids(_bidder, partnerRules));
        //    }


        // This method is used to provide a bidder factory for a specific bid (including Pass).  If no
        // specific bidder is specified and the current bidder does not Pass then the defualt factory
        // will be used.  Otherwise the specific bidder will be used.  If no specific bidder and no default
        // then no factory will used and partner will rely on default bidders.
        // public void Partner(Bid bid, PrescribeBidRules partnerRules)
        // {
        //     Partner(bid, partnerRules));
        // }
        /*
         public void Partner(Bid bid, PrescribedBidsFactory partnerBids)
         {
             if (_partnerBids == null)
             {
                 _partnerBids = new Dictionary<Bid, PrescribedBidsFactory>();
             }
             _partnerBids[bid] = partnerBids;
         }

         public void Partner(int level, Suit suit, PrescribedBidsFactory partnerBids)
         {
             Partner(new Bid(level, suit), partnerBids);
         }

         // This method returns a specific bidder if one was specified, otherwise it will always return null
         // for a Pass bid, and will return the default bidder factory (which could be null also) for any
         // other bid.
         public PrescribedBidsFactory GetPartnerBidFactory(Bid bid)
         {
             if (_partnerBids != null && _partnerBids.ContainsKey(bid))
             {
                 return _partnerBids[bid];
             }
             if (bid.IsPass || _defaultPartnerBids == null)
             {
                 return null;
             }
             return _defaultPartnerBids;
         }
        
        // Return NULL if no bids satisfy the constraints, otherwise returns a BidRuleSet
        public BidRuleSet GetBidRuleSet(Bid bid, PositionState ps)
        {
            PrescribedBidsFactory partnerBidsFactory = this.DefaultPartnerBidsFactory;
            var rules = new List<BidRule>();
            foreach (var rule in BidRules)
            {
                if (rule.Bid.Equals(bid) && rule.SatisifiesStaticConstraints(ps))
                {
                    if (rule is PartnerBidRule partnerBids )
                    {
                        Debug.Assert(partnerBidsFactory == this.DefaultPartnerBidsFactory);
                        partnerBidsFactory = partnerBids.PartnerBidFactory;
                    }
                    else
                    {
                        rules.Add(rule);
                    }
                }
            }
            return rules.Count == 0 ? null : new BidRuleSet(bid, rules, partnerBidsFactory);
        }

        public HashSet<Bid> AllBids(PositionState ps)
        {
            var contract = ps.BiddingState.GetContract();
            var definedBids = new HashSet<Bid>();
            foreach (var rule in BidRules)
            {
                if (!definedBids.Contains(rule.Bid) && 
                    !(rule is PartnerBidRule) && 
                    rule.Bid.IsValid(ps, contract).Valid && 
                    rule.SatisifiesStaticConstraints(ps))
                {
                    definedBids.Add(rule.Bid);
                }
            }
            return definedBids;
        }

        public BidRuleSet ChooseBestBid(PositionState ps, Contract contract, HashSet<Bid> excludeBids)
        {
            foreach (var bidRule in BidRules)
            {
                if (!excludeBids.Contains(bidRule.Bid) && bidRule.Bid.IsValid(ps, contract).Valid)
                {
                    if (!(bidRule is PartnerBidRule) && bidRule.SatisifiesStaticConstraints(ps))
                    {
                        if (ps.PrivateHandConforms(bidRule))
                        {
                            return GetBidRuleSet(bidRule.Bid, ps);
                        }
                    }
                }
            }
            // TODO: For now we will just pick pass
            return GetBidRuleSet(Bid.Pass, ps);
        }

    }

}
        */