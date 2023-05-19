﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{



    public delegate PrescribedBids PrescribedBidsFactory();
    public delegate void PrescribeBidRules(PrescribedBids prescribedBids);

    public class PrescribedBids
    {
        private Bidder _bidder;

        public IEnumerable<ConventionRule> ConventionRules { get; set; }

        public IEnumerable<RedirectRule> Redirects { get; set; }

        public IEnumerable<BidRule> Bids { get; set; }

        private PrescribedBidsFactory _defaultPartnerBids;
        private Dictionary<Bid, PrescribedBidsFactory> _partnerBids = null;

        public PrescribedBids(Bidder bidder, PrescribeBidRules setRules)
        {
            this._bidder = bidder;
            setRules(this);
        }



        public Dictionary<Bid, BidRuleSet> GetBids(PositionState ps)
        {
            // Step 1: Make sure this set of rules applies.  If not don't redirect or return any bid rules.
            if (ConventionRules != null)
            {
                foreach (var rule in ConventionRules)
                {
                    if (!rule.Conforms(ps)) { return null; }
                }
            }
            // Step 2: This set of prescribed bids applies so now we either redirect to one or more other
            // sets of prescribed bids OR we return our own set of rules.

            if (Redirects != null)
            {
                RedirectGroupXXX rXXX = null;
                foreach (var redirect in Redirects)
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
            var brs = new Dictionary<Bid, BidRuleSet>();
            var contract = ps.BiddingState.GetContract();
            if (Bids != null)   // TODO: Why is this ever null when redirect doesnt happen?
            {
                foreach (var rule in Bids)
                {
                    // TODO: Perhaps when we pass in "TRUE" to first-time we want to ONLY look at first time bids...

                    if (rule.Bid.IsValid(ps, contract).Valid &&
                        rule.Conforms(true, ps, ps.PublicHandSummary, ps.PairAgreements))
                    {
                        var bid = rule.Bid;
                        if (!brs.ContainsKey(bid))
                        {
                            brs[bid] = new BidRuleSet(bid, _bidder.Convention, GetPartnerBidFactory(bid));
                        }
                        brs[bid].Add(rule);
                    }
                }
            }
            return brs;
        }

        // This sets the default bidder for partner's next time to act.  This bidder will created and
        // invoked so long as the current bidder selects any bid other than Pass.  For setting specific
        // bidders for specific bids, including Pass, use SetPartnerBidder(Bid, BidderFactory).
        public void Partner(PrescribeBidRules partnerRules)
        {
            Partner(() => new PrescribedBids(_bidder, partnerRules));
        }

        public void Partner(PrescribedBidsFactory partnerBids)
        {
            this._defaultPartnerBids = partnerBids;
        }

        // This method is used to provide a bidder factory for a specific bid (including Pass).  If no
        // specific bidder is specified and the current bidder does not Pass then the defualt factory
        // will be used.  Otherwise the specific bidder will be used.  If no specific bidder and no default
        // then no factory will used and partner will rely on default bidders.
        public void Partner(Bid bid, PrescribeBidRules partnerRules)
        {
            Partner(bid, () => new PrescribedBids(_bidder, partnerRules));
        }
        public void Partner(Bid bid, PrescribedBidsFactory partnerBids)
        {
            if (_partnerBids == null)
            {
                _partnerBids = new Dictionary<Bid, PrescribedBidsFactory>();
            }
            _partnerBids[bid] = partnerBids;
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

    }



}
