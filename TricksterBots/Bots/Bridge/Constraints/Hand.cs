using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace BridgeBidding
{
	public class Hand : List<Card>
	{
		public Hand() { }


	
		public static Hand FromTricksterFormat(string tricksterHand)
		{
			var hand = new Hand();
			for (int i = 0; i < tricksterHand.Length; i += 2)
			{
				Card card = Card.FromTricksterFormat(tricksterHand.Substring(i, 2));
				hand.Add(card);
			}
			return hand;
		}

		public int HighCardPoints(Suit? suit = null)
		{
			var highCardPoints = 0;

			if (suit == null)
			{
				//  basic points for high cards
				highCardPoints += this.Count(c => c.Rank == Rank.Ace) * 4;
				highCardPoints += this.Count(c => c.Rank == Rank.King) * 3;
				highCardPoints += this.Count(c => c.Rank == Rank.Queen) * 2;
				highCardPoints += this.Count(c => c.Rank == Rank.Jack);
			} 
			else
			{
				highCardPoints += this.Count(c => (c.Rank == Rank.Ace && c.Suit == suit)) * 4;
				highCardPoints += this.Count(c => (c.Rank == Rank.King && c.Suit == suit)) * 3;
				highCardPoints += this.Count(c => (c.Rank == Rank.Queen && c.Suit == suit)) * 2;
				highCardPoints += this.Count(c => (c.Rank == Rank.Jack && c.Suit == suit));

			}
			return highCardPoints;
		}

		public int LengthPoints()
		{
			//  add for long suit length (adding one for each card over 4 in each suit)
			return Card.Suits.Select(suit => this.Count(c => c.Suit == suit)).Where(count => count >= 5).Sum(count => count - 4);
		}

		public Dictionary<Suit, int> CountsBySuit()
		{
			var counts = this.GroupBy(c => c.Suit).ToDictionary(g => g.Key, g => g.Count());

			//  initialize the missing suits to zero
			foreach (var suit in Card.Suits.Where(suit => !counts.ContainsKey(suit)))
				counts[suit] = 0;

			return counts;
		}

		public bool IsBalanced
		{
			get
			{
				var suitCounts = this.GroupBy(c => c.Suit).Select(g => new { suit = g.Key, count = g.Count() }).OrderBy(sc => sc.count).ToList();
				return suitCounts.Count == 4 && suitCounts[0].count >= 2 && suitCounts[1].count >= 3;
			}
		}


		public bool Is4333
		{
			get
			{
				var counts = CountsBySuit();
				bool found4 = false;
				foreach (var suit in Card.Suits)
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
		}

		public bool IsGoodSuit(Suit suit)
		{
			//  TODO: should we consider a hand "good" if we have more than the minimum count (requires extra argument)?
			//if (minimum > 0 && CountsBySuit(hand)[suit] > minimum)
			//    return true;

			//  otherwise if we have two of the top three Honors or three of the top five Honors in a suit, then it is considered "good"
			return this.Count(c => c.Suit == suit && c.Rank >= Rank.Queen) >= 2 || this.Count(c => c.Suit == suit && c.Rank >= Rank.Ten) >= 3;
		}

	}
}
