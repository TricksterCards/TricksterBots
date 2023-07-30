using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
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

        public static BidRule[] GetOpenRules(PositionState ps)
        {
            return new OneNoTrumpBidder(NTType.Open1NT)._GetOpenRules(ps);
        }

        public static BidRule[] GetOvercallRules(PositionState ps)
        {
            return new OneNoTrumpBidder(NTType.Overcall1NT)._GetOvercallRules(ps);
        }

        public static BidRule[] GetBalancingRules(PositionState ps)
        {
            // TODO: This is not exactly right.  Actual rules should specify that opps suit is stopped.  Does not need to be balanced, but helps.
            // TODO: Anyway, this needs to be rethought...
            return new OneNoTrumpBidder(NTType.Balancing1NT)._GetBalancingRules(ps);
        }

        public BidRule[] _GetOpenRules(PositionState ps)
        {
            return new BidRule[]
            {
                PartnerBids(1, Suit.Unknown, GoodThrough(ps, OpenType.ToString()), ConventionalResponses),
                Nonforcing(1, Suit.Unknown, Points(OpenerRange.Open), Balanced())
            };
        }

        public BidRule[] _GetOvercallRules(PositionState ps)
        {
            return new BidRule[]
            {
                PartnerBids(1, Suit.Unknown, GoodThrough(ps, OpenType.ToString()), ConventionalResponses),
                // TODO: Perhaps more rules here for balancing but for now this is fine -- Balanced() is not necessary
                Nonforcing(1, Suit.Unknown, Points(OpenerRange.Open), PassEndsAuction(false))
            };
        }

        public BidRule[] _GetBalancingRules(PositionState ps)
        {
            return new BidRule[]
            {
                PartnerBids(1, Suit.Unknown, GoodThrough(ps, OpenType.ToString()), ConventionalResponses),
                // TODO: Perhaps more rules here for balancing but for now this is fine -- Balanced() is not necessary
                Nonforcing(1, Suit.Unknown, Points(OpenerRange.Open), PassEndsAuction(true))
            };
        }


        // If this 
        private BidChoices ConventionalResponses(PositionState ps)
        {
            var choices = new BidChoices(ps);
            choices.AddRules(StaymanBidder.InitiateConvention(OpenType));
     //       if (transferEalbed)
      //      {
                choices.AddRules(TransferBidder.InitiateConvention(OpenType));
            //      }
            // else choices.AddRules.Natural1NT.Respond(OpenType)
            // TODO: What about natural stuff?  3-level bids make no sense.  For now add them...
            choices.AddRules(Natural1NT.Respond(OpenType));
            return choices;

        }

    }

    public class NoTrump : Bidder
    {

        public static IEnumerable<BidRule> Open(PositionState ps)
        {
            var bids = new List<BidRule>();

            bids.AddRange(OneNoTrumpBidder.GetOpenRules(ps));
            bids.AddRange(TwoNoTrumpBidder.GetOpenRules(ps));

            return bids;
        }
        public static IEnumerable<BidRule> StrongOvercall(PositionState ps)
        {          
            return OneNoTrumpBidder.GetOvercallRules(ps);
        }

        public static IEnumerable<BidRule> BalancingOvercall(PositionState ps)
        {
            return OneNoTrumpBidder.GetBalancingRules(ps);
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

        public static IEnumerable<BidRule> Respond(NTType type)
        {
            return new Natural1NT(type).NaturalResponse();
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

        private IEnumerable<BidRule> NaturalResponse()
        {
            return new BidRule[]
            {
                Signoff(Bid.Pass, Points(ResponderRange.LessThanInvite)),

                DefaultPartnerBids(Bid.Pass, OpenerRebid),
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
        }

        private IEnumerable<BidRule> OpenerRebid(PositionState _)
        {
            return new BidRule[]
            {
                DefaultPartnerBids(Bid.Pass, ResponderRebid),

                Signoff(Bid.Pass, Points(OpenerRange.DontAcceptInvite), Partner(LastBid(2, Suit.Unknown))),
                Signoff(Bid.Pass, Partner(LastBid(2, Suit.Clubs))),
                Signoff(Bid.Pass, Partner(LastBid(2, Suit.Diamonds))),
                Signoff(Bid.Pass, Partner(LastBid(2, Suit.Hearts))),
                Signoff(Bid.Pass, Partner(LastBid(2, Suit.Spades))),

                Forcing(3, Suit.Hearts, Partner(LastBid(2, Suit.Unknown)), Points(OpenerRange.AcceptInvite), Shape(5)),
                Forcing(3, Suit.Spades, Partner(LastBid(2, Suit.Unknown)), Points(OpenerRange.AcceptInvite), Shape(5)),

                Signoff(3, Suit.Unknown, Points(OpenerRange.AcceptInvite), Partner(LastBid(2, Suit.Unknown))),
                Signoff(3, Suit.Unknown, Partner(LastBid(3, Suit.Hearts)), Shape(Suit.Hearts, 0, 2)),
                Signoff(3, Suit.Unknown, Partner(LastBid(3, Suit.Spades)), Shape(Suit.Spades, 0, 2)),

                Nonforcing(4, Suit.Hearts, Partner(LastBid(3, Suit.Hearts)), Shape(3, 5)),
                Nonforcing(4, Suit.Spades, Partner(LastBid(3, Suit.Spades)), Shape(3, 5))
            };
        }
        private IEnumerable<BidRule> ResponderRebid(PositionState _)
        {
            return new BidRule[]
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


        public static BidRule[] GetOpenRules(PositionState ps)
        {

            return new BidRule[]
            {
                // TODO: Systems on/off through here --- just like 1NT.....
                PartnerBids(2, Suit.Unknown, Bid.Double, Respond),
                Nonforcing(2, Suit.Unknown, OpenPoints, Balanced())
            };
        }


        private static BidChoices Respond(PositionState ps)
        {
            // TODO: Make these redirect rules conditional on some global state.  Conditions can be:
            //  Off
            //  Pass
            //  X
            //  3C
            // So basically just a condition based on the RHO bid and a global somewhere that has these bid options...
            var choices = new BidChoices(ps);
            choices.AddRules(Stayman2NT.InitiateConvention);
            choices.AddRules(Transfer2NT.InitiateConvention);
            choices.AddRules(Natural2NT.NaturalResponse);
            return choices;
        }

    }

    public class Natural2NT : TwoNoTrumpBidder
    {
  

        public static IEnumerable<BidRule> NaturalResponse(PositionState ps)
        {
            return new BidRule[]
            {
                Signoff(Bid.Pass, RespondNoGame),

                // TODO: Perhaps bid BestSuit() of all the signoff suits... 
                Signoff(3, Suit.Clubs, RespondNoGame, Shape(5, 11), LongestMajor(4)),
                Signoff(3, Suit.Diamonds, RespondNoGame, Shape(5, 11), LongestMajor(4)),
                Signoff(3, Suit.Hearts, RespondNoGame, Shape(5, 11)),
                Signoff(3, Suit.Spades, RespondNoGame, Shape(5, 11)),

                Signoff(3, Suit.Unknown, RespondGame, LongestMajor(4)),

                Signoff(4, Suit.Hearts, RespondGame, Shape(5, 11), BetterThan(Suit.Spades)),
                Signoff(4, Suit.Spades, RespondGame, Shape(5, 11), BetterOrEqualTo(Suit.Hearts)),
            };
         }
    }
}
