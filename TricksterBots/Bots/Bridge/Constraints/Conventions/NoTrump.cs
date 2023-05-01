using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class NoTrumpBidder : Bidder
    {
        public enum NTType { Open1NT, Open2NT, Open3NT, Overcall1NT }
        public enum OpenerRange { Open, DontAcceptInvite, AcceptInvite, LessThanSuperAccept, SuperAccept }
        public enum ResponderRange { LessThanInvite, Invite, InviteOrBetter, Game, GameOrBetter, GameIfSuperAccept }

        private static Dictionary<OpenerRange, Constraint> Open1NTPoints = new Dictionary<OpenerRange, Constraint>
        {
            { OpenerRange.Open, And(HighCardPoints(15, 17), Points(15, 18)) },
            { OpenerRange.DontAcceptInvite, And(HighCardPoints(15, 15), Points(15, 16)) },
            { OpenerRange.AcceptInvite, And(HighCardPoints(16, 17), Points(16, 18)) },
            { OpenerRange.LessThanSuperAccept, And(HighCardPoints(15, 16), Points(15, 17)) },
            { OpenerRange.SuperAccept, And(HighCardPoints(17, 17), Points(17, 18)) }
        };

        private static Dictionary<ResponderRange, Constraint> Respond1NTPoints = new Dictionary<ResponderRange, Constraint>
        {
            { ResponderRange.LessThanInvite, And(HighCardPoints(0, 7), Points(0, 8)) },
            { ResponderRange.Invite, And(HighCardPoints(8, 9), Points(8, 10)) },
            { ResponderRange.InviteOrBetter, And(HighCardPoints(8, 40), Points(8, int.MaxValue)) },
            // TODO: What are these ranges????
            { ResponderRange.Game, And(HighCardPoints(10, 15), Points(10, 15)) },
            { ResponderRange.GameOrBetter, And(HighCardPoints(10, 40), Points(10, 40)) },
            // TODO: What is This range??  
            { ResponderRange.GameIfSuperAccept, And(HighCardPoints(6, 20), Points(6, 20)) }
            // TODO: Range for slam interest...
        };
  
        public NoTrumpBidder(NTType type, Convention convention, int priority) : base(convention, priority)
        {

        }

        public Constraint Points(OpenerRange range)
        {
            return Open1NTPoints[range];
        }

        public Constraint Points(ResponderRange range)
        {
            return Respond1NTPoints[range];
        }

        public Constraint DummyPoints(ResponderRange range)
        {
            // TODO: Clean up.  Make a table
            if (range == ResponderRange.Invite) return DummyPoints(8, 9);
            if (range == ResponderRange.InviteOrBetter) return DummyPoints(8, 15);
            if (range == ResponderRange.Game) return DummyPoints(10,  int.MaxValue);
            Debug.Assert(false);
            return DummyPoints(0, 0);
        }
    }

    public class NoTrumpConventions : NoTrumpBidder
    {
        public static Bidder Bidder(NTType type) { return new NoTrumpConventions(type); }
        private NoTrumpConventions(NTType type) : base(type, Convention.NT, 1000)
        {
            switch (type)
            {
                case NTType.Open1NT:
                    this.ConventionRules = new ConventionRule[]
                    {
                         ConventionRule(Role(PositionRole.Responder, 1), Partner(LastBid(1, Suit.Unknown)))
                    };

                    this.Redirects = new RedirectRule[]
                    {
                        new RedirectRule(() => new NaturalNTResponse(type), new Constraint[0]),
                        new RedirectRule(() => new InitiateStayman(type), new Constraint[0]),
                        new RedirectRule(() => new InitiateTransfer(type), new Constraint[0])
                    };
                    break;
            }
        }
    }

    public class NoTrumpNaturalBidder: NoTrumpBidder
    {
        public NoTrumpNaturalBidder(NTType type) : base(type, Convention.Natural, 100) { }
    }
 
    public class NaturalNTResponse: NoTrumpNaturalBidder
    {

        public NaturalNTResponse(NTType type) : base(type)
        {
            this.BidRules = new BidRule[]
            {
                Signoff(CallType.Pass, Points(ResponderRange.LessThanInvite)),
                Signoff(2, Suit.Clubs, Shape(5, 11), Points(ResponderRange.LessThanInvite)),
                Signoff(2, Suit.Diamonds, Shape(5, 11), Points(ResponderRange.LessThanInvite)),
                Signoff(2, Suit.Hearts, Shape(5, 11), Points(ResponderRange.LessThanInvite)),
                Signoff(2, Suit.Spades, Shape(5, 11), Points(ResponderRange.LessThanInvite)),

                Invitational(2, Suit.Unknown, Points(ResponderRange.Invite), LongestMajor(4)),
                
                // TODO: These natural bids are not exactly right....
                Forcing(3, Suit.Hearts, Points(ResponderRange.GameOrBetter), Shape(5, 11)),
                Forcing(3, Suit.Spades, Points(ResponderRange.GameOrBetter), Shape(5, 11)),
                Signoff(3, Suit.Unknown, Points(ResponderRange.Game), LongestMajor(4))
            };
            this.NextConventionState = () => new NaturalNTOpenerRebid(type);
        }
    }

    public class NaturalNTOpenerRebid: NoTrumpNaturalBidder
    {

        public NaturalNTOpenerRebid(NTType type): base(type)
        {
            this.BidRules = new BidRule[]
            {
                Signoff(CallType.Pass, Points(OpenerRange.DontAcceptInvite), Partner(LastBid(2, Suit.Unknown))),
                Signoff(CallType.Pass, Partner(LastBid(2, Suit.Clubs))),
                Signoff(CallType.Pass, Partner(LastBid(2, Suit.Diamonds))),
                Signoff(CallType.Pass, Partner(LastBid(2, Suit.Hearts))),
                Signoff(CallType.Pass, Partner(LastBid(2, Suit.Spades))),

                Forcing(3, Suit.Hearts, Partner(LastBid(2, Suit.Unknown)), Points(OpenerRange.AcceptInvite), Shape(5)),
                Forcing(3, Suit.Spades, Partner(LastBid(2, Suit.Unknown)), Points(OpenerRange.AcceptInvite), Shape(5)),

                Signoff(3, Suit.Unknown, Points(OpenerRange.AcceptInvite), Partner(LastBid(2, Suit.Unknown))),
                Signoff(3, Suit.Unknown, Partner(LastBid(3, Suit.Hearts)), Shape(Suit.Hearts, 0, 2)),
                Signoff(3, Suit.Unknown, Partner(LastBid(3, Suit.Spades)), Shape(Suit.Spades, 0, 2)),

                Nonforcing(4, Suit.Hearts, Partner(LastBid(3, Suit.Hearts)), Shape(3, 5)),
                Nonforcing(4, Suit.Spades, Partner(LastBid(3, Suit.Spades)), Shape(3, 5))
            };
            this.NextConventionState = () => new NaturalNTResponderRebid(type);
        }
    }

    public class NaturalNTResponderRebid: NoTrumpNaturalBidder
    {
        public NaturalNTResponderRebid(NTType type) : base(type)
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
