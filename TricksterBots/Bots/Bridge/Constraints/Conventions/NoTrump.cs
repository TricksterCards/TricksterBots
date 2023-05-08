using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;
using static TricksterBots.Bots.Bridge.OneNoTrumpBidder;

namespace TricksterBots.Bots.Bridge
{
    public class OneNoTrumpBidder : Bidder
    {
        public enum NTType { Open1NT, Overcall1NT, Balancing1NT }
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
            { ResponderRange.Game, And(HighCardPoints(10, 15), Points(10, 15)) },
            { ResponderRange.GameOrBetter, And(HighCardPoints(10, 40), Points(10, 40)) },  
            { ResponderRange.GameIfSuperAccept, And(HighCardPoints(6, 20), Points(6, 20)) }
        };

        private static Dictionary<OpenerRange, Constraint> Overcall1NTPoints = new Dictionary<OpenerRange, Constraint>
        {
            { OpenerRange.Open, And(HighCardPoints(15, 18), Points(15, 19)) },
            { OpenerRange.DontAcceptInvite, And(HighCardPoints(15, 15), Points(15, 16)) },
            { OpenerRange.AcceptInvite, And(HighCardPoints(16, 18), Points(16, 19)) },
            { OpenerRange.LessThanSuperAccept, And(HighCardPoints(15, 16), Points(15, 17)) },
            { OpenerRange.SuperAccept, And(HighCardPoints(17, 18), Points(17, 19)) }
        };

        private static Dictionary<ResponderRange, Constraint> Advance1NTPoints = new Dictionary<ResponderRange, Constraint>
        {
            { ResponderRange.LessThanInvite, And(HighCardPoints(0, 7), Points(0, 8)) },
            { ResponderRange.Invite, And(HighCardPoints(8, 9), Points(8, 10)) },
            { ResponderRange.InviteOrBetter, And(HighCardPoints(8, 40), Points(8, int.MaxValue)) },
            { ResponderRange.Game, And(HighCardPoints(10, 15), Points(10, 15)) },
            { ResponderRange.GameOrBetter, And(HighCardPoints(10, 40), Points(10, 40)) },
            { ResponderRange.GameIfSuperAccept, And(HighCardPoints(6, 20), Points(6, 20)) }
        };


        private static Dictionary<OpenerRange, Constraint> BalancingOvercall1NTPoints = new Dictionary<OpenerRange, Constraint>
        {
            { OpenerRange.Open, And(HighCardPoints(13, 15), Points(13, 16)) },
            { OpenerRange.DontAcceptInvite, And(HighCardPoints(15, 15), Points(15, 16)) },
            { OpenerRange.AcceptInvite, And(HighCardPoints(16, 18), Points(16, 19)) },
            // We never want to super accept so make the superAccept range huge and the less than range cover everything...
            { OpenerRange.LessThanSuperAccept, And(HighCardPoints(13, 15), Points(13, 16)) },
            { OpenerRange.SuperAccept, And(HighCardPoints(40, 40), Points(40, 40)) }
        };


        private static Dictionary<ResponderRange, Constraint> BalancingAdvance1NTPoints = new Dictionary<ResponderRange, Constraint>
        {
            { ResponderRange.LessThanInvite, And(HighCardPoints(0, 9), Points(0, 10)) },
            { ResponderRange.Invite, And(HighCardPoints(10, 11), Points(10, 12)) },
            { ResponderRange.InviteOrBetter, And(HighCardPoints(10, 40), Points(10, int.MaxValue)) },
            { ResponderRange.Game, And(HighCardPoints(12, 15), Points(12, 15)) },
            { ResponderRange.GameOrBetter, And(HighCardPoints(12, 40), Points(12, 40)) },
            // Balancing 1NT does not super accept so make these values impossible
            { ResponderRange.GameIfSuperAccept, And(HighCardPoints(40, 40), Points(40, 40)) }
        };


        private static Dictionary<NTType, Dictionary<OpenerRange, Constraint>> openerRanges = new Dictionary<NTType, Dictionary<OpenerRange, Constraint>>
        {
            { NTType.Open1NT, Open1NTPoints },
            { NTType.Overcall1NT, Overcall1NTPoints },
            { NTType.Balancing1NT, BalancingOvercall1NTPoints }
        };

        private static Dictionary<NTType, Dictionary<ResponderRange, Constraint>> responderRanges = new Dictionary<NTType, Dictionary<ResponderRange, Constraint>>
        {
            { NTType.Open1NT, Respond1NTPoints },
            { NTType.Overcall1NT, Advance1NTPoints },
            { NTType.Balancing1NT, BalancingAdvance1NTPoints }
        };

        public NTType OpenType { get; private set; }

        public OneNoTrumpBidder(NTType type, Convention convention, int priority) : base(convention, priority)
        {
            this.OpenType = type;
        }

        public Constraint Points(OpenerRange range)
        {
            return openerRanges[OpenType][range];
        }

        public Constraint Points(ResponderRange range)
        {
            return responderRanges[OpenType][range];
        }

        public Constraint DummyPoints(ResponderRange range)
        {
            // TODO: THIS IS ALL MESSED UP - FIX IT!!!  
            // TODO: Clean up.  Make a table
            if (range == ResponderRange.Invite) return DummyPoints(8, 9);
            if (range == ResponderRange.InviteOrBetter) return DummyPoints(8, 15);
            if (range == ResponderRange.Game) return DummyPoints(10,  int.MaxValue);
            Debug.Assert(false);
            return DummyPoints(0, 0);
        }
    }



    public class Natural1NTBidder : OneNoTrumpBidder
    {
        public Natural1NTBidder(NTType type) : base(type, Convention.Natural, 100) { }
    }

    public class OpenAndOvercallNoTrump : Bidder
    {
        public static Bidder Bidder() => new OpenAndOvercallNoTrump();
        public OpenAndOvercallNoTrump() : base(Convention.Natural, 100)
        {
            this.Redirects = new RedirectRule[]
            {
                Redirect(Natural1NT.Bidder(OneNoTrumpBidder.NTType.Open1NT), Role(PositionRole.Opener, 1)),
                Redirect(Natural1NT.Bidder(OneNoTrumpBidder.NTType.Overcall1NT),
                         Role(PositionRole.Overcaller, 1), PassEndsAuction(false)),
                Redirect(Natural1NT.Bidder(OneNoTrumpBidder.NTType.Balancing1NT),
                         Role(PositionRole.Overcaller, 1), PassEndsAuction(true)),
                Redirect(Natural2NT.Bidder(), Role(PositionRole.Opener,1)),
                // TODO: NEED NATURAL 3NT TOO BUT FOR NOW JUST 2NT
            };
        }
    }

    public class Natural1NT : Natural1NTBidder
    {
        public static BidderFactory Bidder(NTType type) { return () => new Natural1NT(type); }

        private Natural1NT(NTType type) : base(type)
        {
            this.BidRules = new BidRule[]
            {
                // TODO: If the type is Overcall then we need to check for suit stopped... But anyway thats in the future
                Nonforcing(1, Suit.Unknown, DefaultPriority + 10, Points(OpenerRange.Open), Balanced())
            };
            SetPartnerBidder(() => new Conventions1NT(type));
        }
    }

    public class Conventions1NT : OneNoTrumpBidder
    { 
        public Conventions1NT(NTType type) : base(type, Convention.NT, 1000)
        {
            // TODO: Make these redirect rules conditional on some global state.  Conditions can be:
            //  Off
            //  Pass
            //  X
            //  2C
            // So basically just a condition based on the RHO bid and a global somewhere that has these bid options...
            switch (type)
            {
                case NTType.Open1NT:
                    this.Redirects = new RedirectRule[]
                    {
                        Redirect(() => new NaturalResponseTo1NT(type)),
                        Redirect(() => new InitiateStayman(type)),
                        Redirect(() => new InitiateTransfer(type))
                    };
                    break;

                case NTType.Overcall1NT:
                case NTType.Balancing1NT:
                    this.Redirects = new RedirectRule[]
                    {
                        // TODO: This is SAYC SPECIFIC.  ONLY STAYAN WITH OVERCALL.
                        Redirect(() => new NaturalResponseTo1NT(type)),
                        Redirect(() => new InitiateStayman(type)),
                    };
                    break;
            }
        }
    }

 
    public class NaturalResponseTo1NT: Natural1NTBidder
    {
        public NaturalResponseTo1NT(NTType type) : base(type)
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
                Signoff(3, Suit.Unknown, Points(ResponderRange.Game), LongestMajor(4)),

            };
            SetPartnerBidder(() => new Natural1NTOpenerRebid(type));
        }
    }

    public class Natural1NTOpenerRebid: Natural1NTBidder
    {

        public Natural1NTOpenerRebid(NTType type): base(type)
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
            SetPartnerBidder(() => new Natural1NTResponderRebid(type));
        }
    }

    public class Natural1NTResponderRebid: Natural1NTBidder
    {
        public Natural1NTResponderRebid(NTType type) : base(type)
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

    // ********************************* MAYBE NEW FILE ********************
    public class TwoNoTrumpBidder : Bidder
    {
        protected static Constraint OpenPoints = And(HighCardPoints(20, 21), Points(20, 22));
        protected static Constraint RespondNoGame = And(HighCardPoints(0, 4), Points(0, 4));
        protected static Constraint RespondGame = Points(5, 10);
    //    public static Constraint RespondGameOrBetter = Points(5, 40);

        public TwoNoTrumpBidder(Convention convention, int priority) : base(convention, priority)
        {
          
        }
    }

    public class Natural2NTBidder : TwoNoTrumpBidder
    {
        public Natural2NTBidder() : base(Convention.Natural, 100) { }
    }


    public class Natural2NT : Natural2NTBidder
    {
        public static BidderFactory Bidder() { return () => new Natural2NT(); }

        private Natural2NT()
        {
            this.BidRules = new BidRule[]
            {
                Nonforcing(2, Suit.Unknown, DefaultPriority + 10, OpenPoints, Balanced())
            };
            SetPartnerBidder(() => new Conventions2NT());
        }
    }

    public class Conventions2NT : TwoNoTrumpBidder
    {
        public Conventions2NT() : base(Convention.NT, 1000)
        {
            // TODO: Make these redirect rules conditional on some global state.  Conditions can be:
            //  Off
            //  Pass
            //  X
            //  3C
            // So basically just a condition based on the RHO bid and a global somewhere that has these bid options...
            this.Redirects = new RedirectRule[]
            {
                Redirect(() => new NaturalResponseTo2NT()),
                Redirect(() => new InitiateStayman2NT()),
                Redirect(() => new InitiateTransfer2NT())
            };
        }
    }

    public class NaturalResponseTo2NT: Natural2NTBidder
    {
        public NaturalResponseTo2NT()
        {
            this.BidRules = new BidRule[]
            {
                Signoff(CallType.Pass, 0, RespondNoGame),

                // TODO: Perhaps bid BestSuit() of all the signoff suits... 
                Signoff(3, Suit.Clubs, RespondNoGame, Shape(5, 11), LongestMajor(4)),
                Signoff(3, Suit.Diamonds, RespondNoGame, Shape(5, 11), LongestMajor(4)),
                Signoff(3, Suit.Hearts, RespondNoGame, Shape(5, 11)),
                Signoff(3, Suit.Spades, RespondNoGame, Shape(5, 11)),

                Signoff(3, Suit.Unknown, RespondGame, LongestMajor(4)),

                Signoff(4, Suit.Hearts, RespondGame, Shape(5, 11), BetterThan(Suit.Spades)),
                Signoff(4, Suit.Spades, RespondGame, Shape(5, 11), BetterOrEqualTo(Suit.Hearts)),
            };
            SetPartnerBidder(() => new Natural2NTOpenerRebid());
        }
    }

    public class Natural2NTOpenerRebid: Natural2NTBidder
    {
        public Natural2NTOpenerRebid()
        {
            // TODO: Need more responses here??? Maybe its always compete()?  Not exactly sure...
            this.BidRules = new BidRule[]
            {
                Signoff(CallType.Pass, 0, new Constraint[0])
            };
        }
    }

}
