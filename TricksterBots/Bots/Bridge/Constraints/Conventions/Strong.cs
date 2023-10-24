using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class Strong2Clubs : Bidder
    {

        protected static (int, int) StrongOpenRange = (22, 40);
        protected static (int, int) GameInHand = (25, 40);
        protected static (int, int) PositiveResponse = (8, 18);
        protected static (int, int) Waiting = (0, 18);

        protected static (int, int) RespondBust = (0, 4);
        protected static (int, int) RespondSuitNotBust = (5, 7);
        protected static (int, int) RespondNTNotBust = (5, 9);  // TODO: Is this point range right???




        public static IEnumerable<BidRule> Open(PositionState _)

        {
            return new BidRule[] {
                // TODO: Interference here...
                DefaultPartnerBids(Call.Pass, Respond),
                Forcing(2, Suit.Clubs, Points(StrongOpenRange), ShowsNoSuit())
            };
    
        }

        private static IEnumerable<BidRule> Respond(PositionState _)
        {
            return new BidRule[] {
                DefaultPartnerBids(Bid.Pass, OpenerRebidPositiveResponse),
                Forcing(2, Suit.Hearts, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(2, Suit.Spades, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(2, Suit.Unknown, Points(PositiveResponse), Balanced()),
                Forcing(3, Suit.Clubs, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),
                Forcing(3, Suit.Diamonds, Points(PositiveResponse), Shape(5, 11), Quality(SuitQuality.Good, SuitQuality.Solid)),

                PartnerBids(2, Suit.Diamonds, Bid.Pass, OpenerRebidWaiting), 
                // TODO: Interference...
                Forcing(2, Suit.Diamonds, Points(Waiting), ShowsNoSuit()),

            };
        }

        private static IEnumerable<BidRule> OpenerRebidWaiting(PositionState ps)
        {
            var bids = new List<BidRule>();
            bids.AddRange(TwoNoTrump.After2COpen.Bids(ps));
            bids.AddRange(ThreeNoTrump.After2COpen.Bids(ps));
            bids.AddRange(new BidRule[]
            {
                DefaultPartnerBids(Bid.Pass, Responder2ndBid),
                Forcing(2, Suit.Hearts, Shape(5, 11)),
                Forcing(2, Suit.Spades, Shape(5, 11)),
                Forcing(3, Suit.Clubs, Shape(5, 11)),
                Forcing(3, Suit.Diamonds, Shape(5, 11))
            });
            return bids;
            // TODO: Next state, more bids, et.....
        }

        private static IEnumerable<BidRule> OpenerRebidPositiveResponse(PositionState ps)
        {
            var bids = new List<BidRule>();
            bids.AddRange(Blackwood.InitiateConvention(ps));
            bids.AddRange(new BidRule[]
            {
                // Highest priority is to support responder's suit...
                DefaultPartnerBids(Bid.Pass, Responder2ndBid),

                Forcing(3, Suit.Hearts, Fit(), ShowsTrump()),
                Forcing(3, Suit.Spades, Fit(), ShowsTrump()),
                Forcing(4, Suit.Clubs, Fit(), ShowsTrump()),
                Forcing(4, Suit.Diamonds, Fit(), ShowsTrump()),

				Forcing(2, Suit.Spades, Shape(5, 11)),
	// TODO: What about 2NT??			Forcing(2, Suit.Unknown, Balanced(), Points(Rebid2NT)),
				Forcing(3, Suit.Clubs, Shape(5, 11)),
                Forcing(3, Suit.Diamonds, Shape(5, 11)),
                Forcing(3, Suit.Hearts, Shape(5, 11)),
                Forcing(3, Suit.Spades, Jump(0), Shape(5, 11)),
              // TODO: 3 NT>>>  Forcing(3, Suit.Unknown, Jump(0)),
                Forcing(4, Suit.Clubs, Shape(5, 11), Jump(0)),
              

			});
            return bids;
        }

        private static BidChoices Responder2ndBid(PositionState ps)
        {
            var choices = new BidChoices(ps);
            choices.AddRules(Blackwood.InitiateConvention);
            choices.AddRules(new BidRule[]
            {
                DefaultPartnerBids(Bid.Pass, OpenerPlaceContract),
                Forcing(3, Suit.Hearts, Fit(), ShowsTrump()),
                Forcing(3, Suit.Spades, Fit(), ShowsTrump()),
                Forcing(4, Suit.Clubs, Fit(), ShowsTrump()),
                Forcing(4, Suit.Diamonds, Fit(), ShowsTrump()),

                // Now show a bust hand by bidding cheapest minor with less 0-4 points
                PartnerBids(3, Suit.Clubs, Call.Double, PartnerIsBust),
                PartnerBids(3, Suit.Diamonds, Call.Double, PartnerIsBust, Partner(LastBid(3, Suit.Clubs))),
                Forcing(3, Suit.Clubs, ShowsNoSuit(), Points(RespondBust)),
                Forcing(3, Suit.Diamonds, Partner(LastBid(3, Suit.Clubs)), ShowsNoSuit(), Points(RespondBust)),

                // Show a 5 card major if we have one.
                Forcing(3, Suit.Hearts, Shape(5, 11), Points(RespondSuitNotBust)),
                Forcing(3, Suit.Spades, Shape(5, 11), Points(RespondSuitNotBust)),

                // Final bid if we're 
                Signoff(3, Suit.Unknown, Points(RespondNTNotBust)) 

            });
            return choices;
        }

        private static IEnumerable<BidRule> OpenerPlaceContract(PositionState ps)
        {
            var bids = new List<BidRule>();
            bids.AddRange(Blackwood.InitiateConvention(ps));
            // TODO: Perhaps gerber too???  Not sure...
            bids.AddRange( new BidRule[] 
            {
				Signoff(4, Suit.Hearts, Fit(), ShowsTrump()),  // TODO: Limit points...???
				Signoff(4, Suit.Spades, Fit(), ShowsTrump()),
				Forcing(4, Suit.Clubs, Fit(), ShowsTrump()),
				Forcing(4, Suit.Diamonds, Fit(), ShowsTrump()),
			});
            return bids;
        }
		private static IEnumerable<BidRule> PartnerIsBust(PositionState ps)
		{
			var bids = new List<BidRule>();
			bids.AddRange(Blackwood.InitiateConvention(ps));
			// TODO: Perhaps gerber too???  Not sure...
			bids.AddRange(new BidRule[]
			{
				Signoff(4, Suit.Hearts, LastBid(2, Suit.Hearts), Points(GameInHand)),
				Signoff(4, Suit.Spades, LastBid(2, Suit.Spades), Points(GameInHand)),
                Signoff(5, Suit.Clubs, LastBid(3, Suit.Clubs), Shape(7, 11), Points(GameInHand)),
                Signoff(5, Suit.Diamonds, LastBid(3, Suit.Diamonds), Shape(7, 11), Points(GameInHand)),

                Signoff(3, Suit.Unknown, Points(GameInHand)),

                // Bust partner so return to or original suit...
                Signoff(3, Suit.Hearts, Rebid()),
                Signoff(3, Suit.Spades, Rebid()),
                Signoff(4, Suit.Clubs, Rebid()),
                Signoff(4, Suit.Diamonds, Rebid())
			});
			return bids;
		}

	}

}

