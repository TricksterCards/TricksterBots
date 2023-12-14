using System;
using System.Collections.Generic;
using System.Linq;

namespace BridgeBidding
{


	public enum Direction { North = 0, East = 1, South = 2, West = 3 }

	public static class BridgeBidder
	{
		public static string SuggestBid(string[] hands, string dealer, string vul, string[] historyStrings)
		{
			if (hands == null || hands.Length != 4)
			{
				throw new ArgumentException("hand parameter must contain 4 entries");
			}
			var handList = new List<Hand>();
			for (int i = 0; i < hands.Length; i++)
			{
				if (hands[i] != null)
				{
					handList.Add(Hand.FromTricksterFormat(hands[i]));
				}
				else
				{
					handList.Add(null);
				}	

			}

			var biddingState = new BiddingState(handList.ToArray(), Direction.North, vul);

			var bid = biddingState.SuggestBid(historyStrings);

			return bid.ToString();
		}

		public static Direction Partner(Direction direction)
		{
			return (Direction)(((int)direction + 2) % 4);
		}

		public static Direction RightHandOpponent(Direction direction)
		{
			return (Direction)(((int)direction + 3) % 4);
		}

		public static Direction LeftHandOpponent(Direction direction)
		{
			return (Direction)(((int)direction + 1) % 4);
		}

	}
}
