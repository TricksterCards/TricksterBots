using System;
using System.Collections.Generic;

using static BridgeBidding.OneNoTrumpBidder;

namespace BridgeBidding
{
	public class TransferBidder : OneNoTrumpBidder
	{
		public TransferBidder(NoTrumpDescription ntd) : base(ntd) { }


		public static BidRulesFactory InitiateConvention(NoTrumpDescription ntd)
		{
			return new TransferBidder(ntd).Initiate;
		}

		private IEnumerable<BidRule> Initiate(PositionState _)
		{
			return new BidRule[] {
				DefaultPartnerBids(Bid.Pass, AcceptTransfer),
				DefaultPartnerBids(Bid.Double, AcceptAfterX),
				// For weak hands, transfer to longest major.
				// For invitational hands, 5/5 transfer to hearts then bid spades
				// For game-going hands 5/5 transfer to spades then bid 3H
				Forcing(2, Strain.Diamonds, NTD.RR.LessThanInvite, Shape(Suit.Hearts, 5, 11), Better(Suit.Hearts, Suit.Spades), ShowsSuit(Suit.Hearts)),
				Forcing(2, Strain.Diamonds, NTD.RR.InviteGame, Shape(Suit.Hearts, 5, 11), Shape(Suit.Spades, 0, 5), ShowsSuit(Suit.Hearts)),
				Forcing(2, Strain.Diamonds, NTD.RR.GameOrBetter, Shape(Suit.Hearts, 5, 11), Shape(Suit.Spades, 0, 4), ShowsSuit(Suit.Hearts)),

				Forcing(2, Strain.Hearts, NTD.RR.LessThanInvite, Shape(Suit.Spades, 5, 11), BetterOrEqual(Suit.Spades, Suit.Hearts), ShowsSuit(Suit.Spades)),
				Forcing(2, Strain.Hearts, NTD.RR.InviteGame, Shape(Suit.Spades, 5, 11), Shape(Suit.Hearts, 0, 4), ShowsSuit(Suit.Spades)),
				Forcing(2, Strain.Hearts, NTD.RR.GameOrBetter, Shape(Suit.Spades, 5, 11), ShowsSuit(Suit.Spades)),

				// TODO: Solid long minors are lots of tricks.  Need logic for those....

				Forcing(2, Strain.Spades, NTD.RR.LessThanInvite, Shape(Suit.Clubs, 6, 11)),
				Forcing(2, Strain.Spades, NTD.RR.LessThanInvite, Shape(Suit.Diamonds, 6, 11))
			};
		}

		private IEnumerable<BidRule> AcceptTransfer(PositionState _)
		{
			return new BidRule[] {
				DefaultPartnerBids(Bid.Double, ExplainTransfer),
				Nonforcing(3, Strain.Hearts, ShowsTrump(), Partner(LastBid(2, Suit.Diamonds)), NTD.OR.SuperAccept, Shape(4, 5)),
				Nonforcing(3, Strain.Spades, ShowsTrump(), Partner(LastBid(2, Suit.Hearts)), NTD.OR.SuperAccept, Shape(4, 5)),

				Nonforcing(2, Strain.Hearts, Partner(LastBid(2, Suit.Diamonds))),
				Nonforcing(2, Strain.Spades, Partner(LastBid(2, Suit.Hearts))),
				Nonforcing(3, Strain.Clubs, Partner(LastBid(2, Suit.Spades)))
			};
		}

		private IEnumerable<BidRule> AcceptAfterX(PositionState ps)
		{

			var bids = new List<BidRule> {
				 PartnerBids(Call.Pass, Call.Pass, OpenerShowsTwo),
				Nonforcing(Call.Pass, Partner(LastBid(2, Suit.Diamonds)), Shape(Suit.Hearts, 0, 2)),
				Nonforcing(Call.Pass, Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Spades, 0, 2)),
			};
			bids.AddRange(AcceptTransfer(ps));
			return bids;
		}

		private IEnumerable<BidRule> OpenerShowsTwo(PositionState ps)
		{
			// TODO: Need to either bid our suit or NT if stopped...  Partner (opener) has passed transfer
			// showing exactly two of the major being transferred to...
			throw new NotImplementedException();
		}

		private IEnumerable<BidRule> ExplainTransfer(PositionState _)
		{
			return new BidRule[] {
				DefaultPartnerBids(Bid.Double, OpenerRebid),

				// This can happen if we are 5/5 with invitational hand. Show Spades
				// TODDO: Higher prioiryt than other bids.  Seems reasonable...
				Invitational(2, Strain.Spades, NTD.RR.InviteGame, Shape(5, 11)),

				Forcing(3, Strain.Hearts, NTD.RR.GameOrBetter, Partner(LastBid(2, Strain.Spades)), Shape(5)),
				Signoff(4, Strain.Hearts, NTD.RR.GameIfSuperAccept, Partner(LastBid(3, Strain.Hearts))),
				Signoff(4, Strain.Spades, NTD.RR.GameIfSuperAccept, Partner(LastBid(3, Strain.Spades))),

				Invitational(2, Strain.NoTrump, NTD.RR.InviteGame, Partner(LastBid(2, Strain.Hearts)), Shape(Suit.Hearts, 5)),
				Invitational(2, Strain.NoTrump, NTD.RR.InviteGame, Partner(LastBid(2, Strain.Spades)), Shape(Suit.Spades, 5)),

				Signoff(3, Strain.Diamonds, Partner(LastBid(3, Strain.Clubs)), Shape(Suit.Diamonds, 6, 11)),


				Invitational(3, Strain.Hearts, NTD.RR.InviteGame, Partner(LastBid(2, Strain.Hearts)), Shape(6, 11)),
				Invitational(3, Strain.Spades, NTD.RR.InviteGame, Partner(LastBid(2, Strain.Spades)), Shape(6, 11)),

				Signoff(3, Strain.NoTrump, NTD.RR.Game, Partner(LastBid(2, Strain.Hearts)), Shape(Suit.Hearts, 5)),
				Signoff(3, Strain.NoTrump, NTD.RR.Game, Partner(LastBid(2, Strain.Spades)), Shape(Suit.Spades, 5)),

				Signoff(4, Strain.Hearts, NTD.RR.Game, Partner(LastBid(2, Strain.Hearts)), Shape(6, 11)),


				Signoff(4, Strain.Spades, NTD.RR.Game, Partner(LastBid(2, Strain.Spades)), Shape(6,11)),


				Signoff(Call.Pass, NTD.RR.LessThanInvite)
            // TODO: SLAM BIDDING.  REMEMBER RANGES NEED TO BE DIFFERENT IF SUPER ACCEPTED...
            };
		}


		private IEnumerable<BidRule> OpenerRebid(PositionState _)
		{
			return new BidRule[] {
				// TODO: Make lower priority???  
				Signoff(Bid.Pass, LastBid(3, Strain.Clubs), Partner(LastBid(3, Strain.Diamonds))),


				// If we have a 5 card suit then show it if invited.  
				PartnerBids(3, Strain.Hearts, Call.Double, PlaceGameContract, LastBid(2, Strain.Spades)),
				PartnerBids(3, Strain.Spades, Call.Double, PlaceGameContract, LastBid(2, Strain.Hearts)),
				Forcing(3, Strain.Hearts, NTD.OR.AcceptInvite, LastBid(2, Strain.Spades), Shape(5), Shape(Suit.Spades, 2)),
				Forcing(3, Strain.Spades, NTD.OR.AcceptInvite, LastBid(2, Strain.Hearts), Shape(5), Shape(Suit.Hearts, 2)),


				Signoff(3, Strain.Hearts, NTD.OR.DontAcceptInvite, LastBid(2, Strain.Hearts), Shape(3, 5)),
                Signoff(3, Strain.Spades, NTD.OR.DontAcceptInvite, LastBid(2, Strain.Spades), Shape(3, 5)),

				
				// TODO: Really want to work off of "Partner Shows" instead of PartnerBid...
                Signoff(4, Strain.Hearts, NTD.OR.AcceptInvite, Fit()),
				//Signoff(4, Strain.Hearts, Points(OpenerRange.AcceptInvite), LastBid(2, Strain.Hearts), Partner(LastBid(2, Strain.NoTrump)), Shape(3, 5)),
				//Signoff(4, Strain.Hearts, LastBid(2, Strain.Hearts), Partner(LastBid(3, Strain.NoTrump)), Shape(3, 5)),
				Signoff(4, Strain.Hearts, LastBid(2, Strain.Spades), Partner(LastBid(3, Strain.Hearts)), Shape(3, 5), BetterOrEqual(Suit.Hearts, Suit.Spades)),


				Signoff(4, Strain.Spades, NTD.OR.AcceptInvite, Partner(LastBid(3, Strain.Spades))),
				Signoff(4, Strain.Spades, NTD.OR.AcceptInvite, LastBid(2, Strain.Spades), Partner(LastBid(2, Strain.NoTrump)), Shape(3, 5)),
				Signoff(4, Strain.Spades, LastBid(2, Strain.Spades), Partner(LastBid(3, Strain.NoTrump)), Shape(3, 5)),
				Signoff(4, Strain.Spades, LastBid(2, Strain.Hearts), Partner(LastBid(3, Strain.Spades)), Shape(3, 5), Better(Suit.Spades, Suit.Hearts)),


                // Didn't fine a suit to play in, so bid game if we have the points...
                Signoff(3, Strain.NoTrump,  NTD.OR.AcceptInvite),


                // TODO: SLAM BIDDING...
				// GERBER!  
                // I Think here we will just defer to competative bidding.  Then ranges don't matter.  We just look for 
                // shown values and shapes.  By this point everything is pretty clear.  The only thing is do we have a shown
                // fit or is it a known fit.  Perhaps competative bidding can handle this...  

                Signoff(Call.Pass, NTD.OR.DontAcceptInvite)

			};
		}

		// Only a change of suit by opener after invitation will get to this code.  Figure out where to play
		private IEnumerable<BidRule> PlaceGameContract(PositionState _)
		{
			return new BidRule[] {
				// If partner has shown 5 hearts or 5 spades then this is game force contract so place
				// it in NT or 4 of their suit.

                Signoff(4, Strain.Hearts,  Fit(), ShowsTrump()),
				Signoff(4, Strain.Spades, Fit(), ShowsTrump()),

				Signoff(3, Strain.NoTrump)
			};
		}
	}

	public class Transfer2NT : Bidder
	{
		private TwoNoTrump NTB;
		public Transfer2NT(TwoNoTrump ntb)
		{
			this.NTB = ntb;
		}

		public IEnumerable<BidRule> InitiateConvention(PositionState _)
		{
			return new BidRule[] {
				DefaultPartnerBids(Bid.Double, AcceptTransfer),
				// TODO: Need to deal with 5/5 invite, etc.  For now just basic transfers work
				Forcing(3, Strain.Diamonds, Shape(Suit.Hearts, 5, 11), Better(Suit.Hearts, Suit.Spades)),

				Forcing(3, Strain.Hearts, Shape(Suit.Spades, 5, 11), BetterOrEqual(Suit.Spades, Suit.Hearts))

			};
		}
		private IEnumerable<BidRule> AcceptTransfer(PositionState _)
		{
			return new BidRule[] {
				DefaultPartnerBids(Bid.Double, ExplainTransfer),

				Nonforcing(3, Strain.Hearts, Partner(LastBid(3, Strain.Diamonds))),
				Nonforcing(3, Strain.Spades, Partner(LastBid(3, Strain.Hearts)))
			};
		}

		private IEnumerable<BidRule> ExplainTransfer(PositionState _)
		{
			return new BidRule[] {
				DefaultPartnerBids(Bid.Double, PlaceContract),
				Signoff(Bid.Pass, NTB.RespondNoGame),

				Nonforcing(3, Strain.NoTrump, NTB.RespondGame, Partner(LastBid(3, Strain.Hearts)), Shape(Suit.Hearts, 5)),
				Nonforcing(3, Strain.NoTrump, NTB.RespondGame, Partner(LastBid(3, Strain.Spades)), Shape(Suit.Spades, 5)),

				Signoff(4, Strain.Hearts, NTB.RespondGame, Partner(LastBid(3, Strain.Hearts)), Shape(6, 11)),
				Signoff(4, Strain.Spades, NTB.RespondGame, Partner(LastBid(3, Strain.Spades)), Shape(6, 11))

			};
		}

		private static IEnumerable<BidRule> PlaceContract(PositionState _)
		{
			return new BidRule[] {
				Signoff(4, Strain.Hearts, Fit()),
				Signoff(4, Strain.Spades, Fit()),
				Signoff(Bid.Pass)
			};
		}
	}

	public class Transfer3NT : Bidder
	{
		private ThreeNoTrump NTB;

		public Transfer3NT(ThreeNoTrump ntb)
		{
			this.NTB = ntb;
		}

		public IEnumerable<BidRule> InitiateConvention(PositionState ps)
		{
			return new BidRule[]
			{
				PartnerBids(4, Strain.Diamonds, Call.Double, p => AcceptTransfer(p, Strain.Hearts)),
				PartnerBids(4, Strain.Hearts, Call.Double, p => AcceptTransfer(p, Strain.Spades)),
				Forcing(4, Strain.Diamonds, Shape(Suit.Hearts, 5, 11)),
				Forcing(4, Strain.Hearts, Shape(Suit.Spades, 5, 11)),
			};
		}

		public IEnumerable<BidRule> AcceptTransfer(PositionState ps, Strain strain)
		{
			return new BidRule[]
			{
				Invitational(4, strain)
			};
		}

	}
}
