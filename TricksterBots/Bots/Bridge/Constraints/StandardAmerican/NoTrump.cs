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
using static TricksterBots.Bots.Bridge.NoTrumpDescription;


namespace TricksterBots.Bots.Bridge
{

    public class NoTrumpDescription : Bidder
    {
        public struct OpenerRanges
        {
            public Constraint Open;
            public Constraint DontAcceptInvite;
            public Constraint AcceptInvite;
            public Constraint LessThanSuperAccept;
            public Constraint SuperAccept;
        }
        public struct ResponderRanges
        {

            public Constraint LessThanInvite;
            public Constraint InviteGame;
            public Constraint InviteOrBetter;
            public Constraint Game;
            public Constraint GameOrBetter;
            public Constraint GameIfSuperAccept;
            public Constraint InviteSlam;
            public Constraint SmallSlam;
            public Constraint GrandSlam;

            public Constraint GameAsDummy;
            public Constraint InviteAsDummy;
        }

        public String OpenType;

        public OpenerRanges OR;
        public ResponderRanges RR;

    }

    public class Open1NTDescription : NoTrumpDescription
    {
        public Open1NTDescription()
        {
            OpenType = "Open1NT";
            
            OR.Open = And(HighCardPoints(15, 17), Points(15, 18));
            
            OR.DontAcceptInvite = And(HighCardPoints(15, 15), Points(15, 16));
            OR.AcceptInvite = And(HighCardPoints(16, 17), Points(16, 18));
            OR.LessThanSuperAccept = And(HighCardPoints(15, 16), Points(15, 17));
            OR.SuperAccept = And(HighCardPoints(17, 17), Points(17, 18));
           

            RR.LessThanInvite = Points(0, 7);
            RR.InviteGame = Points(8, 9);
            RR.InviteOrBetter = Points(8, 40);
            RR.Game = Points(10, 15);
            RR.GameOrBetter = Points(10, 40);
            RR.GameIfSuperAccept = Points(6, 15);
            RR.InviteSlam = Points(16, 17);
            RR.SmallSlam = Points(18, 19);
            RR.GrandSlam = Points(20, 40);

            // TODO: This dummy stuff seems poorly thought out.  Perhaps just plain old points...
            RR.GameAsDummy = DummyPoints(10, 15);
            RR.InviteAsDummy = DummyPoints(8, 9);
        }
    }

	public class Overcall1NTDescription : NoTrumpDescription
	{
		public Overcall1NTDescription()
		{
			OpenType = "Overcall1NT";

			OR.Open =               And(HighCardPoints(15, 18), Points(15, 19));
			OR.DontAcceptInvite =   And(HighCardPoints(15, 15), Points(15, 16));
			OR.AcceptInvite =       And(HighCardPoints(16, 18), Points(16, 19));
			OR.LessThanSuperAccept= And(HighCardPoints(15, 16), Points(15, 17));
			OR.SuperAccept =        And(HighCardPoints(18, 19), Points(18, 20));


			RR.LessThanInvite =     Points(0, 7);
			RR.InviteGame =         Points(8, 9);
			RR.InviteOrBetter =     Points(8, 40);
			RR.Game =               Points(10, 15);
			RR.GameOrBetter =       Points(10, 40);
			RR.GameIfSuperAccept =  Points(6, 15);
            RR.InviteSlam =         Points(16, 17);
			RR.SmallSlam =          Points(18, 19);
			RR.GrandSlam =          Points(20, 40);

			RR.GameAsDummy = DummyPoints(10, 15);
			RR.InviteAsDummy = DummyPoints(8, 9);

		}
	}


	public class Balancing1NTDescription : NoTrumpDescription
	{
		public Balancing1NTDescription()
		{
			OpenType = "Balancing1NT";

			OR.Open = And(HighCardPoints(13, 15), Points(13, 16));
			OR.DontAcceptInvite = And(HighCardPoints(13, 14), Points(13, 15));
			OR.AcceptInvite = And(HighCardPoints(15, 15), Points(15, 16));
            OR.LessThanSuperAccept = HighCardPoints(13, 15);    // NEVER super accept...
            OR.SuperAccept = HighCardPoints(40, 40);            // NEVER super accept




            // TODO: Review all of the balancing ranges..  They seem busted...

            RR.LessThanInvite = Points(0, 10);
            RR.InviteGame = Points(11, 12);
			RR.InviteOrBetter = Points(11, 40);

            // ALL OF THE FOLLOWINg WILL NEVER HAPPEN - PASSED HAND IMPOSSIBLE ....
			RR.Game = Points(13, 15);
			RR.GameOrBetter = Points(10, 40);
            RR.GameIfSuperAccept = Points(40, 40);
			RR.InviteSlam = Points(40, 40);
			RR.SmallSlam = Points(40, 40);
			RR.GrandSlam = Points(40, 40);

			RR.GameAsDummy = DummyPoints(13, 15);
			RR.InviteAsDummy = DummyPoints(11, 12);

		}
	}


	public class OneNoTrumpBidder : Bidder
    {

		public static OneNoTrumpBidder Open = new OneNoTrumpBidder(new Open1NTDescription());
        public static OneNoTrumpBidder Overcall = new OneNoTrumpBidder(new Overcall1NTDescription());
        public static OneNoTrumpBidder Balancing = new OneNoTrumpBidder(new Balancing1NTDescription());


        public NoTrumpDescription NTD;
        
        protected OneNoTrumpBidder(NoTrumpDescription ntd)
        { 
            NTD = ntd;
        }  

        public IEnumerable<BidRule> Bids(PositionState ps)
        {
            if (ps.Role == PositionRole.Opener && ps.RoleRound == 1)
            {
                return new BidRule[]
                {
                    PartnerBids(1, Suit.Unknown, Call.Double, ConventionalResponses),
                    Nonforcing(1, Suit.Unknown, NTD.OR.Open, Balanced())
                };
            }
            if (ps.Role == PositionRole.Overcaller && ps.RoleRound == 1)
            {
				if (ps.BiddingState.Contract.PassEndsAuction && NTD.OpenType == "Balancing1NT")
				{
                    return new BidRule[]
                    {
                        PartnerBids(1, Suit.Unknown, Call.Double, ConventionalResponses),
                        // TODO: Perhaps more rules here for balancing but for now this is fine -- Balanced() is not necessary
                        Nonforcing(1, Suit.Unknown, NTD.OR.Open, PassEndsAuction(true))
                    };
				}
                else if (NTD.OpenType == "Overcall1NT")
                {
                    return new BidRule[]
                    {
                        PartnerBids(1, Suit.Unknown, Call.Double, ConventionalResponses),
                        Nonforcing(1, Suit.Unknown, NTD.OR.Open, Balanced(), OppsStopped(), PassEndsAuction(false))
                    };
                }
			}
            return new BidRule[0];
		}


        // If this 
        private BidChoices ConventionalResponses(PositionState ps)
        {
            var choices = new BidChoices(ps);
            choices.AddRules(StaymanBidder.InitiateConvention(NTD));
            choices.AddRules(TransferBidder.InitiateConvention(NTD));
            choices.AddRules(Gerber.InitiateConvention(ps));
            choices.AddRules(Natural1NT.Respond(NTD));
            return choices;
        }

    }


    public class NoTrump : Bidder
    {

        public static IEnumerable<BidRule> Open(PositionState ps)
        {
            var bids = new List<BidRule>();

            bids.AddRange(OneNoTrumpBidder.Open.Bids(ps));
            bids.AddRange(TwoNoTrump.Open.Bids(ps));

            return bids;
        }


        public static IEnumerable<BidRule> StrongOvercall(PositionState ps)
        {          
            return OneNoTrumpBidder.Overcall.Bids(ps);
        }

        public static IEnumerable<BidRule> BalancingOvercall(PositionState ps)
        {
            return OneNoTrumpBidder.Balancing.Bids(ps);
        }

      
    }



    public class Natural1NT : OneNoTrumpBidder
    {
        public Natural1NT(NoTrumpDescription ntd) : base(ntd)
        {
        }

        public static IEnumerable<BidRule> Respond(NoTrumpDescription ntd)
        {
            return new Natural1NT(ntd).NaturalResponse();
        }



        private IEnumerable<BidRule> NaturalResponse()
        {
            return new BidRule[]
            {

                DefaultPartnerBids(Bid.Pass, OpenerRebid),
                Signoff(2, Suit.Clubs, Shape(5, 11), NTD.RR.LessThanInvite),
                Signoff(2, Suit.Diamonds, Shape(5, 11), NTD.RR.LessThanInvite),
                Signoff(2, Suit.Hearts, Shape(5, 11), NTD.RR.LessThanInvite),
                Signoff(2, Suit.Spades, Shape(5, 11), NTD.RR.LessThanInvite),

                Invitational(2, Suit.Unknown, NTD.RR.InviteGame, LongestMajor(4)),
                // TODO: These natural bids are not exactly right....
                Forcing(3, Suit.Hearts, NTD.RR.GameOrBetter, Shape(5, 11)),
                Forcing(3, Suit.Spades, NTD.RR.GameOrBetter, Shape(5, 11)),
                Signoff(3, Suit.Unknown, NTD.RR.Game, LongestMajor(4)),

                Invitational(4, Suit.Unknown, NTD.RR.InviteSlam), // TODO: Any shape stuff here???

                Signoff(6, Suit.Unknown, Flat(), NTD.RR.SmallSlam),
                Signoff(6, Suit.Unknown, Balanced(), Shape(Suit.Hearts, 2, 3), Shape(Suit.Spades, 2, 3), NTD.RR.SmallSlam),

                Signoff(Bid.Pass, NTD.RR.LessThanInvite),


            };
        }

        private IEnumerable<BidRule> OpenerRebid(PositionState _)
        {
            return new BidRule[]
            {
                DefaultPartnerBids(Bid.Pass, ResponderRebid),

                Signoff(Call.Pass, NTD.OR.DontAcceptInvite, Partner(LastBid(2, Suit.Unknown))),
                Signoff(Call.Pass, Partner(LastBid(2, Suit.Clubs))),
                Signoff(Call.Pass, Partner(LastBid(2, Suit.Diamonds))),
                Signoff(Call.Pass, Partner(LastBid(2, Suit.Hearts))),
                Signoff(Call.Pass, Partner(LastBid(2, Suit.Spades))),

                Forcing(3, Suit.Hearts, Partner(LastBid(2, Suit.Unknown)), NTD.OR.AcceptInvite, Shape(5)),
                Forcing(3, Suit.Spades, Partner(LastBid(2, Suit.Unknown)), NTD.OR.AcceptInvite, Shape(5)),

                Signoff(3, Suit.Unknown, NTD.OR.AcceptInvite, Partner(LastBid(2, Suit.Unknown))),
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
  
}
