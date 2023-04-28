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
                new RedirectRule(() => new NaturalNTResponse(), new Constraint[0]),
                new RedirectRule(() => new InitiateStayman(), new Constraint[0]),
                new RedirectRule(() => new InitiateTransfer(), new Constraint[0])
            };
        }
    }

    public class NaturalNTResponse: Bidder
    {

        static protected (int, int) NTLessThanInvite = (0, 7);
        static protected (int, int) NTInvite = (8, 9);
        static protected (int, int) NTInviteOrBetter = (8, 40);
        static protected (int, int) NTGame = (10, 15);
        static protected (int, int) NTSlamInterest = (16, 40);
        static protected (int, int) NTGameOrBetter = (10, 40);
        static protected (int, int) NTAcceptInvite = (16, 17);
        static protected (int, int) NTDontAcceptInvite = (15, 15);
        static protected (int, int) NTOpen = (15, 17);

        public NaturalNTResponse() : base(Convention.Natural, 100)
        {
            this.BidRules = new BidRule[]
            {
                Signoff(CallType.Pass, Points(NTLessThanInvite)),
                Signoff(2, Suit.Clubs, Shape(5, 11), Points(NTLessThanInvite)),
                Signoff(2, Suit.Diamonds, Shape(5, 11), Points(NTLessThanInvite)),
                Signoff(2, Suit.Hearts, Shape(5, 11), Points(NTLessThanInvite)),
                Signoff(2, Suit.Spades, Shape(5, 11), Points(NTLessThanInvite)),

                Invitational(2, Suit.Unknown, Points(NTInvite), LongestMajor(4)),
                
                // TODO: These natural bids are not exactly right....
                Forcing(3, Suit.Hearts, Points(NTGameOrBetter), Shape(5, 11)),
                Forcing(3, Suit.Spades, Points(NTGameOrBetter), Shape(5, 11)),
                Signoff(3, Suit.Unknown, Points(NTGame), LongestMajor(4))
            };
            this.NextConventionState = () => new NaturalNTOpenerRebid();
        }
    }

    public class NaturalNTOpenerRebid: Bidder
    {

        static protected (int, int) NTLessThanInvite = (0, 7);
        static protected (int, int) NTInvite = (8, 9);
        static protected (int, int) NTInviteOrBetter = (8, 40);
        static protected (int, int) NTGame = (10, 15);
        static protected (int, int) NTSlamInterest = (16, 40);
        static protected (int, int) NTGameOrBetter = (10, 40);
        static protected (int, int) NTAcceptInvite = (16, 17);
        static protected (int, int) NTDontAcceptInvite = (15, 15);
        static protected (int, int) NTOpen = (15, 17);

        public NaturalNTOpenerRebid(): base(Convention.Natural, 100)
        {
            this.BidRules = new BidRule[]
            {
                Signoff(CallType.Pass, Points(NTDontAcceptInvite), Partner(LastBid(2, Suit.Unknown))),
                Signoff(CallType.Pass, Partner(LastBid(2, Suit.Clubs))),
                Signoff(CallType.Pass, Partner(LastBid(2, Suit.Diamonds))),
                Signoff(CallType.Pass, Partner(LastBid(2, Suit.Hearts))),
                Signoff(CallType.Pass, Partner(LastBid(2, Suit.Spades))),

                Forcing(3, Suit.Hearts, Partner(LastBid(2, Suit.Unknown)), Points(NTAcceptInvite), Shape(5)),
                Forcing(3, Suit.Spades, Partner(LastBid(2, Suit.Unknown)), Points(NTAcceptInvite), Shape(5)),

                Signoff(3, Suit.Unknown, Points(NTAcceptInvite), Partner(LastBid(2, Suit.Unknown))),
                Signoff(3, Suit.Unknown, Partner(LastBid(3, Suit.Hearts)), Shape(Suit.Hearts, 0, 2)),
                Signoff(3, Suit.Unknown, Partner(LastBid(3, Suit.Spades)), Shape(Suit.Spades, 0, 2)),

                Nonforcing(4, Suit.Hearts, Partner(LastBid(3, Suit.Hearts)), Shape(3, 5)),
                Nonforcing(4, Suit.Spades, Partner(LastBid(3, Suit.Spades)), Shape(3, 5))
            };
            this.NextConventionState = () => new NaturalNTResponderRebid();
        }
    }

    public class NaturalNTResponderRebid: Bidder
    {
        public NaturalNTResponderRebid() : base(Convention.Natural, 100)
        {
            this.BidRules = new BidRule[]
            {
                // TODO: Ideally this would be "Parther(ShowsShape(Hearts, 5)" Better than lastbid...
                Signoff(3, Suit.Unknown, Partner(LastBid(3, Suit.Hearts)), Shape(Suit.Hearts, 0, 2)),
                Signoff(3, Suit.Unknown, Partner(LastBid(3, Suit.Spades)), Shape(Suit.Spades, 0, 2)),


                Nonforcing(4, Suit.Hearts, Partner(LastBid(3, Suit.Hearts)), Shape(3, 4)),
                Nonforcing(4, Suit.Spades, Partner(LastBid(3, Suit.Spades)), Shape(3, 4))

            };
        }
    }
}
