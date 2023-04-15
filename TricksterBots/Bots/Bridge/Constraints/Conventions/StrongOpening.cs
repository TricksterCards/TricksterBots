﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	public class StrongOpening : Bidder
	{
		private static (int, int) StrongOpenRange = (22, 40);



		public StrongOpening() : base(BidConvention.StrongOpening, 5000) { }
		public override IEnumerable<BidRule> GetRules(BidXXX xxx, Direction direction, BiddingSummary biddingSummary)
		{

			if (xxx.Role == PositionRole.Opener && xxx.Round == 1)
			{
				return InitiateStrongOpen();
			}
			return new BidRule[0];
		}

		public BidRule[] InitiateStrongOpen()
		{
			BidRule[] rules =
			{
				Rule(2, Suit.Clubs, Points(StrongOpenRange)),
				// TODO: Need to look at quick tricks.  Someting like
				//Rule(2, Suit.Clubs, QuickTricks(10));
			};
			return rules;
		}
	}
}
