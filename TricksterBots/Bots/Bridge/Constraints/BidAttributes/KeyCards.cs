using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.cloud;

namespace BridgeBidding
{
	public class KeyCards : DynamicConstraint, IShowsState
	{
		HashSet<int> _count;
		Suit? _trumpSuit;
		bool? _haveQueen;
		public KeyCards(Suit? trumpSuit, bool? haveQueen, params int[] count)
		{
			_trumpSuit = trumpSuit;
			_haveQueen = haveQueen;
			_count = new HashSet<int>(count);
		}



		public override bool Conforms(Call call, PositionState ps, HandSummary hs)
		{
			var keyCards = hs.CountAces;
			if (_trumpSuit != null)
			{
				// First check the status of the queen, exiting early if possible.
				if (_haveQueen != null)
				{
					var q = hs.Suits[(Suit)_trumpSuit].HaveQueen;
					if (q == null) return true; // If we don't know, then it could be true
					if (q != _haveQueen) return false;
				}
				keyCards = hs.Suits[(Suit)_trumpSuit].KeyCards;
			}
			if (keyCards == null) return true;	// If we don't know, we don't know
			return _count.Intersect(hs.CountAces).Count() > 0;
		}

		public void ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
		{
			if (_trumpSuit is Suit suit)
			{
				showHand.Suits[suit].ShowKeyCards(_count);
				if (_haveQueen is bool haveQueen)
				{
					showHand.Suits[suit].ShowHaveQueen(haveQueen);
				}
			}
			else
			{
				showHand.ShowCountAces(_count);
			}	
		}
	}


	public class Kings : DynamicConstraint, IShowsState
	{
		HashSet<int> _count;

		public Kings(params int[] count)
		{
			_count = new HashSet<int>(count);
		}



		public override bool Conforms(Call call, PositionState ps, HandSummary hs)
		{
			if (hs.CountKings is HashSet<int> countKings) {
				return _count.Intersect(countKings).Count() > 0;
			}
			return true;	// If we don't know then we don't know...
		}

		public void ShowState(Call call, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
		{
			showHand.ShowCountKings(_count);
		}
	}

}
