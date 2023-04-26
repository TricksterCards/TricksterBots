using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class NoTrumpConventions : Bidder
    {
        public static Bidder Bidder() { return new NoTrumpConventions(); }
        private NoTrumpConventions() : base(Convention.NT, 1000)
        {
            this.ConventionRules = new ConventionRule[]
            {
                ConventionRule(Role(PositionRole.Responder, 1), Partner(LastBid(1, Suit.Unknown)))
            };

            this.Redirects = new RedirectRule[]
            {
                new RedirectRule(() => new InitiateStayman(), new Constraint[0]),
                new RedirectRule(() => new InitiateTransfer(), new Constraint[0])
            };
        }
    }
}
