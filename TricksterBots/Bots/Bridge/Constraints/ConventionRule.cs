using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class ConventionRule
    {

        public BidConvention Convention { get; }
        private Constraint[] _constraints;

        public ConventionRule(params Constraint[] constraints)
        {
            this._constraints = constraints;
        }

        public bool Conforms(PositionState ps)
        {
            var nullBid = new Bid(Call.NotActed, BidForce.Nonforcing);
            foreach (var constraint in _constraints)
            {
                if (!constraint.Conforms(nullBid, ps, ps.PublicHandSummary, ps.PairAgreements)) { return false; }
            }
            return true;
        }
    }


    public class RedirectRule : ConventionRule
    {
        private Bidder _bidder;
        private PrescribeBidRules _setRules;
        private PrescribedBidsFactory _factory;
        public RedirectRule(Bidder bidder, PrescribeBidRules setRules, params Constraint[] constraints) : base(constraints)
        {
            this._bidder = bidder;
            this._setRules = setRules;
            this._factory = null;
        }
        public RedirectRule(PrescribedBidsFactory factory, params Constraint[] constraints)
        {
            this._bidder = null;
            this._setRules = null;
            this._factory= factory;
        }

        public PrescribedBids RedirectedBidder(PositionState ps)
        {
            if (this.Conforms(ps))
            {
                return (_factory != null) ? _factory() : new PrescribedBids(_bidder, _setRules);
            }
            return null;
        }
    }
}
