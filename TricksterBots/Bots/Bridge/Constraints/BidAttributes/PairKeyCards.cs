using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
	internal class PairKeyCards : DynamicConstraint
	{
		int[] _count;
		Suit? _trumpSuit;
		bool? _hasQueen;
		
		public PairKeyCards(Suit? trumpSuit, bool? hasQueen, params int[] count)
		{
			_trumpSuit = trumpSuit;
			_hasQueen = hasQueen;
			_count = count;
		}
		public int TotalKeyCards
		{
			get { return _trumpSuit == null ? 4 : 5; }
		}

		// TODO: Implement ShowState??? Is that necessary?  
		public override bool Conforms(Call call, PositionState ps, HandSummary hs)
		{
			var ourKeyCards = hs.CountAces;
			var partnerKeyCards = ps.Partner.PublicHandSummary.CountAces;
			if (_trumpSuit is Suit suit)
			{
				ourKeyCards = hs.Suits[suit].KeyCards;
				partnerKeyCards = ps.Partner.PublicHandSummary.Suits[suit].KeyCards;
			}
			if (ourKeyCards == null)
			{
				if (partnerKeyCards == null) return true;   // We know nothing..
				return _count.Max() >= partnerKeyCards.Min();
			}
			if (partnerKeyCards == null)
			{
				return _count.Max() >= ourKeyCards.Min();
			}
			if (_hasQueen != null)
			{
				throw new NotImplementedException();
			}
			foreach (var ourCount in ourKeyCards)
			{
				foreach (var partnerCount in partnerKeyCards)
				{
					if (_count.Contains(ourCount + partnerCount)) return true;
				}
			}
			return false;
		}
	}
}
