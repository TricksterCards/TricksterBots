using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
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
            { ResponderRange.InviteOrBetter, And(HighCardPoints(8, 40), Points(8, 100)) },
            { ResponderRange.Game, Or(And(HighCardPoints(10, 15), Points(10, 15)), And(HighCardPoints(8,15), Points(11, 17))) },
            { ResponderRange.GameOrBetter, Or(And(HighCardPoints(10, 40), Points(10, 40)), And(HighCardPoints(8, 40), Points(11, 40))) },  
            { ResponderRange.GameIfSuperAccept, And(HighCardPoints(6, 20), Points(6, 20)) }
        };
        // TODO: Need to incorporate the "OR" thingie...
        /*
        private static Dictionary<ResponderRange, Constraint> Respond1NTPoints = new Dictionary<ResponderRange, Constraint>
        {
            { ResponderRange.LessThanInvite, Points(0, 7) },
            { ResponderRange.Invite, Points(8, 9) },
            { ResponderRange.InviteOrBetter, Points(8, 40) },
            { ResponderRange.Game, Points(10, 15) },
            { ResponderRange.GameOrBetter, Points(10, 40) },
            { ResponderRange.GameIfSuperAccept, Points(6, 20) }
        };
        */
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
            { ResponderRange.InviteOrBetter, And(HighCardPoints(8, 40), Points(8, 100)) },
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
            { ResponderRange.InviteOrBetter, And(HighCardPoints(10, 40), Points(10, 100)) },
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

        public OneNoTrumpBidder(NTType type)
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
            if (range == ResponderRange.Game) return DummyPoints(10,  100);
            Debug.Fail("Should never get to here - Range not supported");
            return DummyPoints(0, 0);
        }

        public static BidRule[] GetOpenRules()
        {
            return new OneNoTrumpBidder(NTType.Open1NT)._GetOpenRules();
        }

        public static BidRule[] GetOvercallRules()
        {
            return new OneNoTrumpBidder(NTType.Overcall1NT)._GetOpenRules();
        }

        public static BidRule[] GetBalancingRules()
        {
            // TODO: This is not exactly right.  Actual rules should specify that opps suit is stopped.  Does not need to be balanced, but helps.
            // TODO: Anyway, this needs to be rethought...
            return new OneNoTrumpBidder(NTType.Balancing1NT)._GetBalancingRules();
        }

        public BidRule[] _GetOpenRules()
        {
            return new BidRule[]
            {
                PartnerBids(1, Suit.Unknown, Respond),
                Nonforcing(1, Suit.Unknown, Points(OpenerRange.Open), Balanced(), PassEndsAuction(false))
            };
        }

        public BidRule[] _GetBalancingRules()
        {
            return new BidRule[]
            {
                PartnerBids(1, Suit.Unknown, Respond),
                // TODO: Perhaps more rules here for balancing but for now this is fine -- Balanced() is not necessary
                Nonforcing(1, Suit.Unknown, Points(OpenerRange.Open), PassEndsAuction(true))
            };
        }

        private PrescribedBids Respond()
        {
            var pb = new PrescribedBids();
            pb.Redirect(StaymanBidder.InitiateConvention(OpenType));
            pb.Redirect(TransferBidder.InitiateConvention(OpenType));
            pb.Redirect(Natural1NT.Respond(OpenType));
            return pb;
        }

    }

    public class NoTrump : Bidder
    {

        public static PrescribedBids Open()
        {
            var pb = new PrescribedBids();
            pb.BidRules.AddRange(OneNoTrumpBidder.GetOpenRules());
            pb.BidRules.AddRange(TwoNoTrumpBidder.GetOpenRules());


            return pb;


        }
        public static PrescribedBids StrongOvercall()
        {
            var pb = new PrescribedBids();
            pb.BidRules.AddRange(OneNoTrumpBidder.GetOvercallRules());
            return pb;
        }

        public static PrescribedBids BalancingOvercall()
        {
            var pb = new PrescribedBids();
            pb.BidRules.AddRange(OneNoTrumpBidder.GetBalancingRules());
            return pb;
        }

        /*
        private void Initiate(PrescribedBids pb)
        {
            pb.Redirects = new RedirectRule[]
            {

				Redirect(() => Natural1NT.Bidder(OneNoTrumpBidder.NTType.Balancing1NT),
						 Role(PositionRole.Overcaller, 1), PassEndsAuction(true)),

				Redirect(() => Natural1NT.Bidder(OneNoTrumpBidder.NTType.Open1NT), Role(PositionRole.Opener, 1)),


				Redirect(() => Natural1NT.Bidder(OneNoTrumpBidder.NTType.Overcall1NT),
                         Role(PositionRole.Overcaller, 1), PassEndsAuction(false)),

                Redirect(Natural2NT.DefaultBidderXXX, Role(PositionRole.Opener,1)),
                // TODO: NEED NATURAL 3NT TOO BUT FOR NOW JUST 2NT
            };
        }
        */
    }



    public class Natural1NT : OneNoTrumpBidder
    {
        public Natural1NT(NTType type) : base(type)
        {
        }

        public static PrescribedBidsFactory Respond(NTType type)
        {
            return new Natural1NT(type).NaturalResponse;
        }

        /*
        public PrescribedBids Respond()
        {
            var pb = new PrescribedBids();
            // TODO: Make these redirect rules conditional on some global state.  Conditions can be:
            //  Off
            //  Pass
            //  X
            //  2C
            // So basically just a condition based on the RHO bid and a global somewhere that has these bid options...
            switch (OpenType)
            {
                case NTType.Open1NT:
                    pb.Redirects = new RedirectRule[]
                    {
                        Redirect(() => StaymanBidder.InitiateConvention(OpenType), ConventionOn("Stayman1NTOpen")),
                        Redirect(() => TransferBidder.InitiateConvention(OpenType), ConventionOn("Transfer1NTOpen")),
						Redirect(NaturalResponse),
					};
                    break;

                case NTType.Overcall1NT:
                case NTType.Balancing1NT:
                    pb.Redirects = new RedirectRule[]
                    {
                        // TODO: This is SAYC SPECIFIC.  ONLY STAYAN WITH OVERCALL.
                        Redirect(() => StaymanBidder.InitiateConvention(OpenType)),
						Redirect(NaturalResponse),
					};
                    break;
            }
        }
        */

        private PrescribedBids NaturalResponse()
        {
            var pb = new PrescribedBids();
            pb.BidRules.AddRange(new BidRule[]
            {
                Signoff(Call.Pass, Points(ResponderRange.LessThanInvite)),

                Signoff(2, Suit.Clubs, Shape(5, 11), Points(ResponderRange.LessThanInvite)),
                Signoff(2, Suit.Diamonds, Shape(5, 11), Points(ResponderRange.LessThanInvite)),
                Signoff(2, Suit.Hearts, Shape(5, 11), Points(ResponderRange.LessThanInvite)),
                Signoff(2, Suit.Spades, Shape(5, 11), Points(ResponderRange.LessThanInvite)),

                Invitational(2, Suit.Unknown, Points(ResponderRange.Invite), LongestMajor(4)),
                // TODO: These natural bids are not exactly right....
                Forcing(3, Suit.Hearts, Points(ResponderRange.GameOrBetter), Shape(5, 11)),
                Forcing(3, Suit.Spades, Points(ResponderRange.GameOrBetter), Shape(5, 11)),
                Signoff(3, Suit.Unknown, Points(ResponderRange.Game), LongestMajor(4)),

            });
            pb.DefaultPartnerBidsFactory = OpenerRebid;
            return pb;
        }

        private PrescribedBids OpenerRebid()
        {
            var pb = new PrescribedBids();
            pb.BidRules.AddRange(new BidRule[]
            {
                Signoff(Call.Pass, Points(OpenerRange.DontAcceptInvite), Partner(LastBid(2, Suit.Unknown))),
                Signoff(Call.Pass, Partner(LastBid(2, Suit.Clubs))),
                Signoff(Call.Pass, Partner(LastBid(2, Suit.Diamonds))),
                Signoff(Call.Pass, Partner(LastBid(2, Suit.Hearts))),
                Signoff(Call.Pass, Partner(LastBid(2, Suit.Spades))),

                Forcing(3, Suit.Hearts, Partner(LastBid(2, Suit.Unknown)), Points(OpenerRange.AcceptInvite), Shape(5)),
                Forcing(3, Suit.Spades, Partner(LastBid(2, Suit.Unknown)), Points(OpenerRange.AcceptInvite), Shape(5)),

                Signoff(3, Suit.Unknown, Points(OpenerRange.AcceptInvite), Partner(LastBid(2, Suit.Unknown))),
                Signoff(3, Suit.Unknown, Partner(LastBid(3, Suit.Hearts)), Shape(Suit.Hearts, 0, 2)),
                Signoff(3, Suit.Unknown, Partner(LastBid(3, Suit.Spades)), Shape(Suit.Spades, 0, 2)),

                Nonforcing(4, Suit.Hearts, Partner(LastBid(3, Suit.Hearts)), Shape(3, 5)),
                Nonforcing(4, Suit.Spades, Partner(LastBid(3, Suit.Spades)), Shape(3, 5))
            });
            pb.DefaultPartnerBidsFactory = ResponderRebid;
            return pb;
        }
        private PrescribedBids ResponderRebid()
        {
            var pb = new PrescribedBids();
            pb.BidRules.AddRange(new BidRule[]
            {
                // TODO: Ideally this would be "Parther(ShowsShape(Hearts, 5)" Better than lastbid...
                Signoff(3, Suit.Unknown, Partner(LastBid(3, Suit.Hearts)), Shape(Suit.Hearts, 0, 2)),
                Signoff(3, Suit.Unknown, Partner(LastBid(3, Suit.Spades)), Shape(Suit.Spades, 0, 2)),


                Nonforcing(4, Suit.Hearts, Partner(LastBid(3, Suit.Hearts)), Shape(3, 4)),
                Nonforcing(4, Suit.Spades, Partner(LastBid(3, Suit.Spades)), Shape(3, 4))

            });
            return pb;
        }
    }

    // ********************************* MAYBE NEW FILE ********************
    public class TwoNoTrumpBidder : Bidder
    {
        protected static Constraint OpenPoints = And(HighCardPoints(20, 21), Points(20, 22));
        protected static Constraint RespondNoGame = And(HighCardPoints(0, 4), Points(0, 4));
        protected static Constraint RespondGame = Points(5, 10);
        //    public static Constraint RespondGameOrBetter = Points(5, 40);


        public static BidRule[] GetOpenRules()
        {

            return new BidRule[]
            {
                PartnerBids(2, Suit.Unknown, Respond),
                Nonforcing(2, Suit.Unknown, OpenPoints, Balanced())
            };
        }


        private static PrescribedBids Respond()
        {
            // TODO: Make these redirect rules conditional on some global state.  Conditions can be:
            //  Off
            //  Pass
            //  X
            //  3C
            // So basically just a condition based on the RHO bid and a global somewhere that has these bid options...
            var pb = new PrescribedBids();
            pb.Redirect(Stayman2NT.InitiateConvention);
            pb.Redirect(Transfer2NT.InitiateConvention);
            pb.Redirect(Natural2NT.NaturalResponse);
            return pb;
        }

    }

    public class Natural2NT : TwoNoTrumpBidder
    {
  

        public static PrescribedBids NaturalResponse()
        {
            var pb = new PrescribedBids();
            pb.BidRules.AddRange(new BidRule[]
            {
                Signoff(Call.Pass, RespondNoGame),

                // TODO: Perhaps bid BestSuit() of all the signoff suits... 
                Signoff(3, Suit.Clubs, RespondNoGame, Shape(5, 11), LongestMajor(4)),
                Signoff(3, Suit.Diamonds, RespondNoGame, Shape(5, 11), LongestMajor(4)),
                Signoff(3, Suit.Hearts, RespondNoGame, Shape(5, 11)),
                Signoff(3, Suit.Spades, RespondNoGame, Shape(5, 11)),

                Signoff(3, Suit.Unknown, RespondGame, LongestMajor(4)),

                Signoff(4, Suit.Hearts, RespondGame, Shape(5, 11), BetterThan(Suit.Spades)),
                Signoff(4, Suit.Spades, RespondGame, Shape(5, 11), BetterOrEqualTo(Suit.Hearts)),
            });
            return pb;
        }
    }
}
