using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Trickster.cloud;

namespace Trickster.Bots
{
    public enum Direction { North, East, South, West }
    public enum Vulnerability { None, Favorable, BothVul, Unfavorable }

    public enum Pair { NorthSouth, EastWest }


    /// <summary>
    ///     Based on ACBL Bridge Series "Bidding in the 21st Century" updated 2010 edition
    /// </summary>
    public class BasicBidding
    {
        public static readonly Suit[] BasicSuits = { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades };
        public static readonly Suit[] MajorSuits = { Suit.Hearts, Suit.Spades };
        public static readonly Suit[] Strains = { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades, Suit.Unknown };

		public static readonly Direction[] Directions = { Direction.North, Direction.East, Direction.South, Direction.West };

        public static Direction Partner(Direction direction)
        {
            return Directions[((int)direction + 2) % 4];
        }

        public static Direction RightHandOpponent(Direction direction)
        {
			return Directions[((int)direction + 3) % 4];
		}

		public static Direction LeftHandOpponent(Direction direction)
		{
			return Directions[((int)direction + 1) % 4];
		}


		public static int ComputeDistributionPoints(Hand hand)
        {
            var distributionPoints = 0;

            //  add for long suit length (adding one for each card over 4 in each suit)
            distributionPoints += BasicSuits.Select(suit => hand.Count(c => c.suit == suit)).Where(count => count >= 5).Sum(count => count - 4);

            return distributionPoints;
        }

        // TODO: This makes no sense unless you know the trump suit.  If you are void in the
        // trump suit then adding 5 points for the void is 100% incorrect.  
        public static int ComputeDummyPoints(Hand hand)
        {
            //  used instead of distribution points when responding to a major suit opening
            var dummyPoints = 0;

            //  add 5 for each void suit
            dummyPoints += BasicSuits.Select(suit => hand.Count(c => c.suit == suit)).Where(count => count == 0).Sum(count => 5);

            //  add 3 for each singleton suit
            dummyPoints += BasicSuits.Select(suit => hand.Count(c => c.suit == suit)).Where(count => count == 1).Sum(count => 3);

            //  add 1 for each doubleton suit
            dummyPoints += BasicSuits.Select(suit => hand.Count(c => c.suit == suit)).Where(count => count == 2).Sum(count => 1);

            return dummyPoints;
        }

        public static int ComputeHighCardPoints(IEnumerable<Card> hand)
        {
            var highCardPoints = 0;

            //  basic points for high cards
            highCardPoints += hand.Count(c => c.rank == Rank.Ace) * 4;
            highCardPoints += hand.Count(c => c.rank == Rank.King) * 3;
            highCardPoints += hand.Count(c => c.rank == Rank.Queen) * 2;
            highCardPoints += hand.Count(c => c.rank == Rank.Jack);

            return highCardPoints;
        }

		public static int ComputeHighCardPoints(Hand hand, Suit suit)
		{
			var highCardPoints = 0;

			//  basic points for high cards
			highCardPoints += hand.Count(c => (c.rank == Rank.Ace && c.suit == suit)) * 4;
			highCardPoints += hand.Count(c => (c.rank == Rank.King && c.suit == suit)) * 3;
			highCardPoints += hand.Count(c => (c.rank == Rank.Queen && c.suit == suit)) * 2;
			highCardPoints += hand.Count(c => (c.rank == Rank.Jack && c.suit == suit));

			return highCardPoints;
		}


		public static int DummyPoints(Hand hand, Suit trumpSuit)
        {
            var trumpCount = hand.Count(c => c.suit == trumpSuit);
			int adjust = 0;
            if (trumpCount >= 3)
            {
                int[] bonus = { 3, 2, 1 };
                if (trumpCount > 3)
                {
                    bonus[0] = 5;
                    bonus[1] = 3;
                }
                // This is the Audrey Grant version of counting as dummy.  Don't care about shortness of honors...
                foreach (Suit suit in BasicBidding.BasicSuits)
                {
                    var count = hand.Count(c => c.suit == suit);
                    if (count < 3)
                    {
                        adjust += bonus[count];
                    }
                }
            }
            return adjust;
        }


        public static Dictionary<Suit, int> CountsBySuit(Hand hand)
        {
            var counts = hand.GroupBy(c => c.suit).ToDictionary(g => g.Key, g => g.Count());

            //  initialize the missing suits to zero
            foreach (var suit in BasicSuits.Where(suit => !counts.ContainsKey(suit)))
                counts[suit] = 0;

            return counts;
        }

        public static bool Is4333(Hand hand)
        {
            return Is4333(CountsBySuit(hand));
        }

        public static bool Is4333(Dictionary<Suit, int> counts)
        {
            bool found4 = false;
			foreach (var suit in BasicSuits)
            {
                if (counts[suit] > 4) return false;
                if (counts[suit] == 4)
                {
                    if (found4) return false;
                    found4 = true;
                }
            }
            return true;
		}

        public static int LosingTrickCount(Hand hand)
        {
            var counts = CountsBySuit(hand);
            var losers = 0;
			foreach (var suit in BasicSuits)
			{
                // TODO: This seems inefficient.  Should probably hand.Contains... but need to figure out what's right.
                var hasAce = hand.Count(c => (c.suit == suit && c.rank == Rank.Ace)) == 1;
				var hasKing = hand.Count(c => (c.suit == suit && c.rank == Rank.King)) == 1;
				var hasQueen = hand.Count(c => (c.suit == suit && c.rank == Rank.Queen)) == 1;
				switch (counts[suit])
                {
                    case 0: break;
                    case 1:
                        if (!hasAce) { losers++; }
                        break;
                    case 2:
						if (!hasAce) { losers++; }
						if (!hasKing) { losers++; }
						break;
                    default:
						if (!hasAce) { losers++; }
						if (!hasKing) { losers++; }
						if (!hasQueen) { losers++; }
						break;
                }
			}
            return losers;
		}



        public int BergenPointAdjust(Hand hand)
        {
            var adjust = 0;
            int countOvervalued = hand.Count(c => (c.rank == Rank.Queen || c.rank == Rank.Jack));
            int countUndervalued = hand.Count(c => (c.rank == Rank.Ace || c.rank == Rank.Ten));
            if (countUndervalued - countOvervalued >= 3) { adjust += 1; }
            if (countOvervalued - countUndervalued >= 3) { adjust -= 1; }
            // TODO: More stuff here...
            if (adjust > 0 && BasicBidding.Is4333(hand)) { adjust -= 1; }
            return adjust;
        }

		public static bool IsMajor(Suit suit)
        {
            return (suit == Suit.Hearts || suit == Suit.Spades);
        }

        public static bool IsMinor(Suit suit)
        {
            return (suit == Suit.Clubs || suit == Suit.Diamonds);
        }
        public static Suit OtherMajor(Suit major) 
        {
            // TODO: Assert or throw that major is either Hearts or Spades.  Don't know pattern we are supposed to use
            return major == Suit.Hearts ? Suit.Spades : Suit.Hearts;
        }

        public static bool HasStopper(Hand hand, Suit suit)
        {
            //  A, Kx, Qxx, or Jxxx
            return hand.Count(c => c.suit == suit && c.rank >= Rank.Jack && hand.Count(c2 => c2.suit == suit && c2.rank != c.rank) >= Rank.Ace - c.rank) > 0;
        }

        public static bool IsBalanced(Hand hand)
        {
            var suitCounts = hand.GroupBy(c => c.suit).Select(g => new { suit = g.Key, count = g.Count() }).OrderBy(sc => sc.count).ToList();
            return suitCounts.Count == 4 && suitCounts[0].count >= 2 && suitCounts[1].count >= 3;
        }

        public static bool IsFlat(Hand hand)
        {
            return CountsBySuit(hand).Values.OrderByDescending(c => c).ToList()[3] == 3;
        }

        public static bool IsGoodSuit(Hand hand, Suit suit)
        {
            //  TODO: should we consider a hand "good" if we have more than the minimum count (requires extra argument)?
            //if (minimum > 0 && CountsBySuit(hand)[suit] > minimum)
            //    return true;

            //  otherwise if we have two of the top three Honors or three of the top five Honors in a suit, then it is considered "good"
            return hand.Count(c => c.suit == suit && c.rank >= Rank.Queen) >= 2 || hand.Count(c => c.suit == suit && c.rank >= Rank.Ten) >= 3;
        }

        private static bool HasSingletonHonor(Hand hand)
        {
            var counts = CountsBySuit(hand);
            return hand.Any(c => counts[c.suit] == 1 && Rank.Jack <= c.rank && c.rank <= Rank.King);
        }
    }
}