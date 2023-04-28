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
            var nullBid = new Bid(CallType.NotActed, BidForce.Nonforcing);
            foreach (var constraint in _constraints)
            {
                if (!constraint.Conforms(nullBid, ps, ps.PublicHandSummary, ps.PairAgreements)) { return false; }
            }
            return true;
        }
    }


    public class RedirectRule : ConventionRule
    {
        private BidderFactory _redirectTo;
        public RedirectRule(BidderFactory redirectTo, params Constraint[] constraints) : base(constraints)
        {
            this._redirectTo = redirectTo;
        }
        public Bidder Redirect(PositionState ps)
        {
            return (this.Conforms(ps)) ? this._redirectTo() : null;
        }
    }
}
