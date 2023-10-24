using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public class ThreeNoTrump : Bidder
	{
		public Constraint OpenPoints = And(HighCardPoints(25, 27), Points(25, 28));
		public Constraint RespondNoSlam = Points(0, 5);	// TODO: More slam stuff...

		//    public static Constraint RespondGameOrBetter = Points(5, 40);

		public static ThreeNoTrump Open = new ThreeNoTrump();
		public static ThreeNoTrump After2COpen = new ThreeNoTrump();	



		public BidRule[] Bids(PositionState ps)
		{

			return new BidRule[]
			{
                // TODO: Systems on/off through here --- just like 1NT.....
                PartnerBids(3, Suit.Unknown, Bid.Double, Respond),
				Nonforcing(3, Suit.Unknown, OpenPoints, Balanced())
			};
		}


		private BidChoices Respond(PositionState ps)
		{
			var choices = new BidChoices(ps);
			choices.AddRules(Gerber.InitiateConvention);
			//TODO: Stayman over 3NT is odd... choices.AddRules(new Stayman3NT(this).InitiateConvention);
			choices.AddRules(new Transfer3NT(this).InitiateConvention);
			choices.AddRules(new Natural3NT(this).Response);
			return choices;
		}

	}

	public class Natural3NT : Bidder
	{
		private ThreeNoTrump NTB;
		public Natural3NT(ThreeNoTrump ntb)
		{
			this.NTB = ntb;
		}

		public IEnumerable<BidRule> Response(PositionState ps)
		{
			return new BidRule[]
			{
			     // TODO: Perhaps bid BestSuit() of all the signoff suits... 
             	Signoff(4, Suit.Hearts, NTB.RespondNoSlam, Shape(5, 11)),
				Signoff(4, Suit.Spades, NTB.RespondNoSlam, Shape(5, 11)),

				Signoff(Bid.Pass, NTB.RespondNoSlam),
			};
		}
	}

}
