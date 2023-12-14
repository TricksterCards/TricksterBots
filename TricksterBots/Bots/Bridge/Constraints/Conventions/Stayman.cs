using System;
using System.Collections.Generic;

namespace BridgeBidding
{


    public class StaymanBidder : OneNoTrumpBidder
	{
 
        public StaymanBidder(NoTrumpDescription ntd) : base(ntd) { }

        public static BidRulesFactory InitiateConvention(NoTrumpDescription ntd)
        {
            return new StaymanBidder(ntd).Initiate;
        }

		private IEnumerable<BidRule> Initiate(PositionState ps)
		{
            // If there is a bid then it can only be 2C..
            Bid bidStayman = new Bid(2, Suit.Clubs);

            Call call = ps.RightHandOpponent.GetBidHistory(0).Equals(bidStayman) ? Bid.Double : bidStayman;
            return new BidRule[] {
                PartnerBids(call, Call.Double, Answer),
                Forcing(call, NTD.RR.InviteOrBetter, Shape(Suit.Hearts, 4), Shape(Suit.Spades, 0, 4), Flat(false), ShowsNoSuit()),
                Forcing(call, NTD.RR.InviteOrBetter, Shape(Suit.Spades, 4), Shape(Suit.Hearts, 0, 4), Flat(false), ShowsNoSuit()),
                Forcing(call, NTD.RR.InviteOrBetter, Shape(Suit.Hearts, 4), Shape(Suit.Spades, 5), ShowsNoSuit()),
                Forcing(call, NTD.RR.InviteOrBetter, Shape(Suit.Hearts, 5), Shape(Suit.Spades, 4), ShowsNoSuit()),
                
                Forcing(call, NTD.RR.LessThanInvite, Shape(Suit.Diamonds, 4, 5), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 4)),
            };
            // TODO: Need to add rules for garbage stayman if that is on, and for 4-way transfers if that is on...
		}

       
        public IEnumerable<BidRule> Answer(PositionState ps)
		{
            return new BidRule[] {

                PartnerBids(2, Strain.Diamonds, Call.Double, RespondTo2D),
				PartnerBids(2, Strain.Hearts, Call.Double, p => RespondTo2M(p, Suit.Hearts)),
				PartnerBids(2, Strain.Spades, Call.Double, p => RespondTo2M(p, Suit.Spades)),

				// TODO: Deal with interferenceDefaultPartnerBids(goodThrough: Bid.Double, Explain),

				// TODO: Are these bids truly forcing?  Not if garbage stayman...
				Forcing(2, Suit.Diamonds, Shape(Suit.Hearts, 0, 3), Shape(Suit.Spades, 0, 3), ShowsNoSuit()),
				Forcing(2, Suit.Hearts, Shape(4, 5), LongerOrEqualTo(Suit.Spades)),
                Forcing(2, Suit.Spades, Shape(4, 5), LongerThan(Suit.Hearts))
            };
        }


        public IEnumerable<BidRule> RespondTo2D(PositionState ps)
        {
            var bids = new List<BidRule>
            {
                // TODO: Points 0-7 defined as garbage range...
                Signoff(Call.Pass, NTD.RR.LessThanInvite),

                PartnerBids(3, Strain.Hearts, Call.Double, p => GameNewMajor(p, Suit.Hearts)),
                PartnerBids(3, Strain.Spades, Call.Double, p => GameNewMajor(p, Suit.Spades)),
                // If we have a 5 card suit and want to go to game then show that suit.
                Forcing(3, Suit.Spades, NTD.RR.GameOrBetter, Shape(5)),
				Forcing(3, Suit.Hearts, NTD.RR.GameOrBetter, Shape(5)),

                // These show invitational 5/4
                PartnerBids(2, Strain.Hearts, Call.Double, p => PlaceConractNewMajor(p, Suit.Hearts)),
				PartnerBids(2, Strain.Spades, Call.Double, p => PlaceConractNewMajor(p, Suit.Spades)),
				Invitational(2, Suit.Hearts, NTD.RR.InviteGame, Shape(5)),
				Invitational(2, Suit.Spades, NTD.RR.InviteGame, Shape(5)),

                PartnerBids(2, Strain.NoTrump, Call.Double, PlaceContract2NTInvite),
				Invitational(2, Strain.NoTrump, ShowsTrump(), NTD.RR.InviteGame),

                Signoff(3, Strain.NoTrump, ShowsTrump(), NTD.RR.Game),

                // TODO: Point ranges - Need to figure out where these...
                Invitational(4, Strain.NoTrump, ShowsTrump(), PairPoints((30, 31)))
			};
            bids.AddRange(Gerber.InitiateConvention(ps));
            return bids;
        }

        public IEnumerable<BidRule> RespondTo2M(PositionState _, Suit major)
        {
            var majorStrain = Call.SuitToStrain(major);
            return new BidRule[]
            {

                Signoff(Call.Pass, NTD.RR.LessThanInvite),

                Signoff(4, major, Shape(4, 5), NTD.RR.GameAsDummy, ShowsTrump()),
                PartnerBids(3, majorStrain, Call.Double, p => PlaceContractMajorInvite(p, major)),
                Invitational(3, major, Shape(4, 5), NTD.RR.InviteAsDummy, ShowsTrump()),

                PartnerBids(3, Strain.NoTrump, Call.Double, CheckOpenerSpadeGame),
                Signoff(3, Strain.NoTrump, NTD.RR.Game, Shape(major, 0, 3)),

				PartnerBids(2, Strain.NoTrump, Call.Double, PlaceContract2NTInvite),
				Invitational(2, Strain.NoTrump, NTD.RR.InviteGame, Shape(major, 0, 3))
			};
		}
        /*
        public IEnumerable<BidRule> Explain(PositionState _)
        {
            return new BidRule[] {
                DefaultPartnerBids(Bid.Double, PlaceContract), 

                // TODO: Points 0-7 defined as garbage range...
                Signoff(Call.Pass, NTD.RR.LessThanInvite),   // Garbage stayman always passes...

                // If we have a 5 card suit and want to go to game then show that suit.
                Forcing(3, Suit.Spades, NTD.RR.GameOrBetter, Shape(5), Partner(LastBid(2, Suit.Diamonds))),
                Forcing(3, Suit.Hearts, NTD.RR.GameOrBetter, Shape(5), Partner(LastBid(2, Suit.Diamonds))),


				// These show invitational 5/4
                Invitational(2, Suit.Hearts, NTD.RR.InviteGame, Shape(5), Partner(LastBid(2, Suit.Diamonds))),
                Invitational(2, Suit.Spades, NTD.RR.InviteGame, Shape(5), Partner(LastBid(2, Suit.Diamonds))),

                Invitational(2, Suit.Unknown, ShowsTrump(), NTD.RR.InviteGame, Partner(LastBid(2, Suit.Diamonds))),
                Invitational(2, Suit.Unknown, ShowsTrump(), NTD.RR.InviteGame, Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 0, 3)),
                Invitational(2, Suit.Unknown, ShowsTrump(), NTD.RR.InviteGame, Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 0, 3)),


                Invitational(3, Suit.Hearts, ShowsTrump(), NTD.RR.InviteAsDummy, Partner(LastBid(2, Suit.Hearts)), Shape(4, 5)),
                Invitational(3, Suit.Spades, ShowsTrump(), NTD.RR.InviteAsDummy, Partner(LastBid(2, Suit.Spades)), Shape(4, 5)),


                // Prioritize suited contracts over 3NT bid by placing these rules first...
                Signoff(4, Suit.Hearts, ShowsTrump(), NTD.RR.GameAsDummy, Partner(LastBid(2, Suit.Hearts)), Shape(4, 5)),
                Signoff(4, Suit.Spades, ShowsTrump(), NTD.RR.GameAsDummy, Partner(LastBid(2, Suit.Spades)), Shape(4, 5)), 

                // TODO: After changeover is done an tests are working again, change all of these rules to simply
                // Signoff(3, Suit.Unknown, ShowsTrump(), Points(ResponderRange.Game), Fit(Suit.Hearts, false), Fit(Suit.Spades, false)),
                Signoff(3, Suit.Unknown, ShowsTrump(), NTD.RR.Game, Partner(LastBid(2, Suit.Diamonds))),
                Signoff(3, Suit.Unknown, ShowsTrump(), NTD.RR.Game, Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 0, 3)),
                Signoff(3, Suit.Unknown, ShowsTrump(), NTD.RR.Game, Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 0, 3)),

            };
        }
        */


        //******************** 2nd bid of opener.

        // Bid sequence was 1NT/2C/2X/
        public IEnumerable<BidRule> CheckOpenerSpadeGame(PositionState ps)
        {
            return new BidRule[]
            {
                Signoff(4, Suit.Spades, Fit(), ShowsTrump()),
                Signoff(Call.Pass)
            };
        }

        public IEnumerable<BidRule> GameNewMajor(PositionState ps, Suit major)
        {
            return new BidRule[]
            {
                Signoff(4, major, Fit(), ShowsTrump()),
                Signoff(3, Strain.NoTrump)
            };
        }

        public IEnumerable<BidRule> PlaceConractNewMajor(PositionState ps, Suit major)
        {
            return new BidRule[]
            {
                Signoff(Call.Pass, NTD.OR.DontAcceptInvite, Fit(major)),    // TODO: Need to use dummy points here...
                Signoff(2, Strain.NoTrump, NTD.OR.DontAcceptInvite),
                Signoff(4, major, Fit(), ShowsTrump(), NTD.OR.AcceptInvite),
                Signoff(3, Strain.NoTrump, ShowsTrump(), NTD.OR.AcceptInvite)
            };
        }

        public IEnumerable<BidRule> PlaceContract2NTInvite(PositionState ps)
        {
            return new BidRule[]
            {
				PartnerBids(3, Strain.Spades, Bid.Double, CheckSpadeGame),
                // This is possible to know we have a fit if partner bid stayman, we respond hearts,
                Nonforcing(3, Suit.Spades, Break(false, "3NT"), NTD.OR.DontAcceptInvite, Fit(), ShowsTrump()),


                Signoff(4, Suit.Spades, NTD.OR.AcceptInvite, Fit(), ShowsTrump()),

                Signoff(3, Strain.NoTrump, NTD.OR.AcceptInvite)
			};

        }

        public IEnumerable<BidRule> PlaceContractMajorInvite(PositionState ps, Suit major)
        {
			return new BidRule[]
            {
				Signoff(4, major, NTD.OR.AcceptInvite, Fit(), ShowsTrump()),
            };

		}
		/*
        public IEnumerable<BidRule> PlaceContract(PositionState _)
        {
            return new BidRule[] {
				// These rules deal with a 5/4 invitational that we want to reject.  Leave contract in bid suit
				// if we have 3.  Otherwise put in NT
				Signoff(Bid.Pass, NTD.OR.DontAcceptInvite,  // TODO: Should check for dummy points...
                                    Fit(Suit.Hearts), Partner(LastBid(2, Suit.Hearts))),
                Signoff(Bid.Pass, NTD.OR.DontAcceptInvite,
                                    Fit(Suit.Spades), Partner(LastBid(2, Suit.Spades))),

                Signoff(2, Suit.Unknown, NTD.OR.DontAcceptInvite),



                Signoff(3, Suit.Unknown, NTD.OR.AcceptInvite, Partner(LastBid(2, Suit.Unknown))),
                Signoff(3, Suit.Unknown, LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Hearts)),
                            Shape(Suit.Hearts, 2)),
                Signoff(3, Suit.Unknown, NTD.OR.AcceptInvite, LastBid(2, Suit.Diamonds),
                                    Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 2)),
                Signoff(3, Suit.Unknown,  LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Spades)),
                            Shape(Suit.Spades, 2)),
                Signoff(3, Suit.Unknown, NTD.OR.AcceptInvite, LastBid(2, Suit.Diamonds),
                        Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 2)),



                Signoff(4, Suit.Hearts, NTD.OR.AcceptInvite, Fit()),
               //TODO: I think above rule ocvers itl.. Signoff(4, Suit.Hearts, LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Hearts)), Shape(3)),


                Signoff(4, Suit.Spades, NTD.OR.AcceptInvite, Partner(LastBid(3, Suit.Spades)), Fit()),
                Signoff(4, Suit.Spades, NTD.OR.AcceptInvite, Fit()),
                Signoff(4, Suit.Spades, Partner(LastBid(3, Suit.Unknown)), Fit()),
                Signoff(4, Suit.Spades, LastBid(2, Suit.Diamonds), Partner(LastBid(3, Suit.Spades)), Shape(3))
            };
        }
        */
		public IEnumerable<BidRule> CheckSpadeGame(PositionState _)
        {
            return new BidRule[] {
                Signoff(4, Suit.Spades, ShowsTrump(), NTD.RR.GameAsDummy, Shape(4, 5))
            };
		}
	}


    //*********************************************************************************************

    // TODO: Maybe move thse 2NT stayman...
    public class Stayman2NT: Bidder
    {
        private TwoNoTrump NTB;

        public Stayman2NT(TwoNoTrump ntb)
        {
            this.NTB = ntb;
        }

        public IEnumerable<BidRule> InitiateConvention(PositionState ps) 
        {
            // If there is a bid then it can only be 3C..
            Bid bidStayman = new Bid(3, Suit.Clubs);

            Call call = ps.RightHandOpponent.GetBidHistory(0).Equals(bidStayman) ? Bid.Double : bidStayman;
            return new BidRule[] {
                PartnerBids(call, Bid.Double, Answer),
                Forcing(call, NTB.RespondGame, Shape(Suit.Hearts, 4), Flat(false)),
                Forcing(call, NTB.RespondGame, Shape(Suit.Spades, 4), Flat(false)),
                Forcing(call, NTB.RespondGame, Shape(Suit.Hearts, 4), Shape(Suit.Spades, 5)),
                Forcing(call, NTB.RespondGame, Shape(Suit.Hearts, 5), Shape(Suit.Spades, 4))
                // TODO: The following rule is "Garbage Stayman"
                //Forcing(2, Suit.Clubs, Points(NTLessThanInvite), Shape(Suit.Diamonds, 4, 5), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 4)),
            };
        }
        public IEnumerable<BidRule> Answer(PositionState _)
        {
            return new BidRule[] {
                DefaultPartnerBids(Call.Double, ResponderRebid),

                Forcing(3, Suit.Diamonds, Shape(Suit.Hearts, 0, 3), Shape(Suit.Spades, 0, 3)),

                // If we are 4-4 then hearts bid before spades.  Can't be 5-5 or wouldn't be balanced.
                Forcing(3, Suit.Hearts, Shape(4, 5), LongerOrEqualTo(Suit.Spades)),
                Forcing(3, Suit.Spades, Shape(4, 5), LongerThan(Suit.Hearts))
            };
        }

        public static IEnumerable<BidRule> ResponderRebid(PositionState _)
        {
            return new BidRule[] {
                DefaultPartnerBids(Call.Double, OpenerRebid),

                Forcing(3, Strain.Hearts, Shape(5), Partner(LastBid(3, Suit.Diamonds))),
                Forcing(3, Strain.Spades, Shape(5), Partner(LastBid(3, Suit.Diamonds))),

                Signoff(3, Strain.NoTrump, Fit(Suit.Hearts, false), Fit(Suit.Spades, false)),
                Signoff(4, Strain.Hearts, Fit()),
                Signoff(4, Strain.Spades, Fit())
            };
        }
    
        public static IEnumerable<BidRule> OpenerRebid(PositionState _)
        { 
            return new BidRule[] {
                Signoff(3, Strain.NoTrump, Fit(Suit.Hearts, false), Fit(Suit.Spades, false)),
                Signoff(4, Strain.Hearts, Fit()),
                Signoff(4, Strain.Spades, Fit())
            };
        }
    }

}
