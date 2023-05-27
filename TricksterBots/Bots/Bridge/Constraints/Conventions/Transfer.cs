using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;
using static TricksterBots.Bots.Bridge.OneNoTrumpBidder;

namespace TricksterBots.Bots.Bridge
{
	public class TransferBidder : OneNoTrumpBidder
	{
		public TransferBidder(NTType type) : base(type, Convention.Transfer, 1000) { }


		public static PrescribedBids InitiateConvention(NTType type)
		{
			var bidder = new TransferBidder(type);
			return new PrescribedBids(bidder, bidder.Initiate);
	    }

		private void Initiate(PrescribedBids pb)
		{
			pb.Bids = new BidRule[]
			{
				// For weak hands, transfer to longest major.
				// For invitational hands, 5/5 transfer to hearts then bid spades
				// For game-going hands 5/5 transfer to spades then bid 3H
				Forcing(2, Suit.Diamonds, Points(ResponderRange.LessThanInvite), Shape(Suit.Hearts, 5, 11), Better(Suit.Hearts, Suit.Spades), ShowsSuit(Suit.Hearts)),
				Forcing(2, Suit.Diamonds, Points(ResponderRange.Invite), Shape(Suit.Hearts, 5, 11), Shape(Suit.Spades, 0, 5), ShowsSuit(Suit.Hearts)),
				Forcing(2, Suit.Diamonds, Points(ResponderRange.GameOrBetter), Shape(Suit.Hearts, 5, 11), Shape(Suit.Spades, 0, 4), ShowsSuit(Suit.Hearts)),

				Forcing(2, Suit.Hearts, Points(ResponderRange.LessThanInvite), Shape(Suit.Spades, 5, 11), BetterOrEqual(Suit.Spades, Suit.Hearts), ShowsSuit(Suit.Spades)),
				Forcing(2, Suit.Hearts, Points(ResponderRange.Invite), Shape(Suit.Spades, 5, 11), Shape(Suit.Hearts, 0, 4), ShowsSuit(Suit.Spades)),
				Forcing(2, Suit.Hearts, Points(ResponderRange.GameOrBetter), Shape(Suit.Spades, 5, 11), ShowsSuit(Suit.Spades)),

				// TODO: Solid long minors are lots of tricks.  Need logic for those....

				Forcing(2, Suit.Spades, Points(ResponderRange.LessThanInvite), Shape(Suit.Clubs, 6, 11)),
				Forcing(2, Suit.Spades, Points(ResponderRange.LessThanInvite), Shape(Suit.Diamonds, 6, 11)),

			};
			pb.Partner(AcceptTransfer);
		}

		private void AcceptTransfer(PrescribedBids pb)
		{
			pb.Bids = new BidRule[]
			{
				Nonforcing(2, Suit.Hearts, Partner(LastBid(2, Suit.Diamonds)), Points(OpenerRange.LessThanSuperAccept)),
				Nonforcing(2, Suit.Hearts, Partner(LastBid(2, Suit.Diamonds)), Points(OpenerRange.SuperAccept), Shape(0, 3)),

				Nonforcing(2, Suit.Spades, Partner(LastBid(2, Suit.Hearts)), Points(OpenerRange.LessThanSuperAccept)),
				Nonforcing(2, Suit.Spades, Partner(LastBid(2, Suit.Hearts)), Points(OpenerRange.SuperAccept), Shape(0, 3)),

				Nonforcing(3, Suit.Clubs, Partner(LastBid(2, Suit.Spades))),

				Nonforcing(3, Suit.Hearts, ShowsTrump(), Partner(LastBid(2, Suit.Diamonds)), Points(OpenerRange.SuperAccept), Shape(4, 5)),

				Nonforcing(3, Suit.Spades, ShowsTrump(), Partner(LastBid(2, Suit.Hearts)), Points(OpenerRange.SuperAccept), Shape(4, 5)),
			};
			pb.Partner(ExplainTransfer);
		}

		private void ExplainTransfer(PrescribedBids pb)
		{
			pb.Bids = new BidRule[]
			{
				Signoff(Call.Pass, DefaultPriority - 1, Points(ResponderRange.LessThanInvite)),

				// This can happen if we are 5/5 with invitational hand. Show Spades
				// TODDO: Higher prioiryt than other bids.  Seems reasonable...
				Invitational(2, Suit.Spades, DefaultPriority + 1, Points(ResponderRange.LessThanInvite), Shape(5, 11)),

				Invitational(2, Suit.Unknown, Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 5)),
				Invitational(2, Suit.Unknown, Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 5)),

				Signoff(3, Suit.Diamonds, Partner(LastBid(3, Suit.Clubs)), Shape(Suit.Diamonds, 6, 11)),

				// Need to bid 3 hearts with 5/5 and game-going values.
				Invitational(3, Suit.Hearts, Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Hearts)), Shape(6, 11)),
				Forcing(3, Suit.Hearts, DefaultPriority + 1, Points(ResponderRange.GameOrBetter), Partner(LastBid(2, Suit.Spades)), Shape(5)),

				Invitational(3, Suit.Spades, Points(ResponderRange.Invite), Partner(LastBid(2, Suit.Spades)), Shape(6, 11)),

				Signoff(3, Suit.Unknown, Points(ResponderRange.Game), Partner(LastBid(2, Suit.Hearts)), Shape(Suit.Hearts, 5)),
				Signoff(3, Suit.Unknown, Points(ResponderRange.Game), Partner(LastBid(2, Suit.Spades)), Shape(Suit.Spades, 5)),

				Signoff(4, Suit.Hearts, Points(ResponderRange.Game), Partner(LastBid(2, Suit.Hearts)), Shape(6, 11)),
				Signoff(4, Suit.Hearts, DefaultPriority + 1, Points(ResponderRange.GameIfSuperAccept), Partner(LastBid(3, Suit.Hearts))),

				Signoff(4, Suit.Spades, Points(ResponderRange.Game), Partner(LastBid(2, Suit.Spades)), Shape(6,11)),
				Signoff(4, Suit.Spades, DefaultPriority + 1, Points(ResponderRange.GameIfSuperAccept), Partner(LastBid(3, Suit.Spades)))

				// TODO: SLAM BIDDING.  REMEMBER RANGES NEED TO BE DIFFERENT IF SUPER ACCEPTED...
			};
			pb.Partner(OpenerRebid);
		}
	

		private void OpenerRebid(PrescribedBids pb)
		{
			pb.Bids = new BidRule[]
			{
				// TODO: Make lower priority???  
				Signoff(Call.Pass, DefaultPriority - 1, Points(OpenerRange.DontAcceptInvite)),
				Signoff(Call.Pass, DefaultPriority + 1, LastBid(3, Suit.Clubs), Partner(LastBid(3, Suit.Diamonds))),

//Signoff(3, Suit.Hearts, Points(OpenerRange.DontAcceptInvite), LastBid(2, Suit.Hearts), Flat(false), Shape(3)),
                Signoff(3, Suit.Hearts, Points(OpenerRange.DontAcceptInvite), LastBid(2, Suit.Hearts), Shape(3, 5)),
				Forcing(3, Suit.Hearts, Points(OpenerRange.AcceptInvite), LastBid(2, Suit.Spades), Shape(5), Shape(Suit.Spades, 2)),

  //              Signoff(3, Suit.Spades, Points(OpenerRange.DontAcceptInvite), LastBid(2, Suit.Spades), Flat(false), Shape(3)),
                Signoff(3, Suit.Spades, Points(OpenerRange.DontAcceptInvite), LastBid(2, Suit.Spades), Shape(3, 5)),
				Forcing(3, Suit.Spades, Points(OpenerRange.AcceptInvite), LastBid(2, Suit.Hearts), Shape(5), Shape(Suit.Hearts, 2)),

				// TODO: Perhaps priority makes this the last choice... Maybe not.   May need lots of exclusions...
				// TODO: Maybe if partner shows exactly 5 and we are flat then we bid to 3NT instead of accepting
				// even if we have three  What about 4 of them?  Probably safest in suit.... 
				Signoff(3, Suit.Unknown, DefaultPriority - 10, Points(OpenerRange.AcceptInvite)),

				// TODO: Really want to work off of "Partner Shows" instead of PartnerBid...
                Signoff(4, Suit.Hearts, Points(OpenerRange.AcceptInvite), Partner(LastBid(3, Suit.Hearts))),
				Signoff(4, Suit.Hearts, Points(OpenerRange.AcceptInvite), LastBid(2, Suit.Hearts), Partner(LastBid(2, Suit.Unknown)), Shape(3, 5)),
				Signoff(4, Suit.Hearts, LastBid(2, Suit.Hearts), Partner(LastBid(3, Suit.Unknown)), Shape(3, 5)),
				Signoff(4, Suit.Hearts, LastBid(2, Suit.Spades), Partner(LastBid(3, Suit.Hearts)), Shape(3, 5), BetterOrEqual(Suit.Hearts, Suit.Spades)),


				Signoff(4, Suit.Spades, Points(OpenerRange.AcceptInvite), Partner(LastBid(3, Suit.Spades))),
				Signoff(4, Suit.Spades, Points(OpenerRange.AcceptInvite), LastBid(2, Suit.Spades), Partner(LastBid(2, Suit.Unknown)), Shape(3, 5)),
				Signoff(4, Suit.Spades, LastBid(2, Suit.Spades), Partner(LastBid(3, Suit.Unknown)), Shape(3, 5)),
				Signoff(4, Suit.Spades, LastBid(2, Suit.Hearts), Partner(LastBid(3, Suit.Spades)), Shape(3, 5), Better(Suit.Spades, Suit.Hearts))

				// TODO: SLAM BIDDING...
				// I Think here we will just defer to competative bidding.  Then ranges don't matter.  We just look for 
				// shown values and shapes.  By this point everything is pretty clear.  The only thing is do we have a shown
				// fit or is it a known fit.  Perhaps competative bidding can handle this...  
		
			};
			pb.Partner(PlaceGameContract);
		}

		private void PlaceGameContract(PrescribedBids pb)
		{
			pb.Bids = new BidRule[]
			{
				Signoff(Call.Pass, DefaultPriority - 10),
				// If partner has shown 5 hearts or 5 spades then this is game force contract so place
				// it in NT or 4 of their suit.
				Signoff(3, Suit.Unknown, Partner(HasMinShape(Suit.Hearts, 5)), Shape(Suit.Hearts, 2)),
				Signoff(3, Suit.Unknown, Partner(HasMinShape(Suit.Spades, 5)), Shape(Suit.Spades, 2)),

				Signoff(4, Suit.Hearts, Partner(HasMinShape(5)), Shape(3, 5)),
				Signoff(4, Suit.Spades, Partner(HasMinShape(5)), Shape(3, 5)),

			};
		}
	}

    public class Transfer2NT : TwoNoTrumpBidder
    {
        private Transfer2NT() : base(Convention.Transfer, 1000) { }

		public static PrescribedBids InitiateConvention()
        {
            var bidder = new Transfer2NT();
            return new PrescribedBids(bidder, bidder.Initiate);
        }
	    private void Initiate(PrescribedBids pb)
        {
            pb.Bids = new BidRule[]
            {
				// TODO: Need to deal with 5/5 invite, etc.  For now just basic transfers work
                Forcing(3, Suit.Diamonds, Shape(Suit.Hearts, 5, 11), Better(Suit.Hearts, Suit.Spades)),

                Forcing(3, Suit.Hearts, Shape(Suit.Spades, 5, 11), BetterOrEqual(Suit.Spades, Suit.Hearts)),
           
            };
			pb.Partner(AcceptTransfer);

        }
		private void AcceptTransfer(PrescribedBids pb)
		{		
			pb.Bids = new BidRule[]
			{
                Nonforcing(3, Suit.Hearts, Partner(LastBid(3, Suit.Diamonds))),
                Nonforcing(3, Suit.Spades, Partner(LastBid(3, Suit.Hearts)))
            };
			pb.Partner(ExplainTransfer);
		}

		private void ExplainTransfer(PrescribedBids pb)
		{
			pb.Bids = new BidRule[]
			{
				Signoff(Call.Pass, RespondNoGame),

				Nonforcing(3, Suit.Unknown, RespondGame, Partner(LastBid(3, Suit.Hearts)), Shape(Suit.Hearts, 5)),
				Nonforcing(3, Suit.Unknown, RespondGame, Partner(LastBid(3, Suit.Spades)), Shape(Suit.Spades, 5)),

				Signoff(4, Suit.Hearts, RespondGame, Partner(LastBid(3, Suit.Hearts)), Shape(6, 11)),
				Signoff(4, Suit.Spades, RespondGame, Partner(LastBid(3, Suit.Spades)), Shape(6, 11))

            };
			pb.Partner(PlaceContract);
		}

		private void PlaceContract(PrescribedBids pb)
		{
			pb.Bids = new BidRule[]
			{
				Signoff(Call.Pass, DefaultPriority - 10),
				Signoff(4, Suit.Hearts, Fit()),
				Signoff(4, Suit.Spades, Fit())
			};
		}
	}
}
