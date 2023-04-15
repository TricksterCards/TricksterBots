using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{

	public class SuitSummary
	{
		public (int Min, int Max) Shape { get; protected set; }
		public (int Min, int Max) DummyPoints { get; protected set; }

		public (int Min, int Max) LongHandPoints { get; protected set; }

		public (SuitQuality Min, SuitQuality Max) Quality { get; protected set; }


		public SuitSummary()
		{
			this.Shape = (0, 13);
			this.DummyPoints = (0, 40);
			this.LongHandPoints = (0, 40);
			this.Quality = (SuitQuality.Poor, SuitQuality.Solid);
		}
		// TODO: There are other properties like "Stopped", "Has Ace", that can go here...
	}

	public class ModifiableSuitSummary : SuitSummary
	{
		public void ShowShape(int min, int max)
		{
			Shape = (Math.Max(min, Shape.Min), Math.Min(max, Shape.Max));
		}
		public void ShowDummyPoints(int min, int max)
		{
			DummyPoints = (Math.Max(min, DummyPoints.Min), Math.Min(max, DummyPoints.Max)); 
		}
		public void ShowLongHandPoints(int min, int max)
		{
			LongHandPoints = (Math.Max(min, LongHandPoints.Min), Math.Min(max, LongHandPoints.Max));
		}
		public void ShowQuality(SuitQuality min, SuitQuality max)
		{
			int iNewMin = Math.Max((int)min, (int)Quality.Min);
			int iCurMax = Math.Min((int)max, (int)Quality.Max);
			SuitQuality newMin = (SuitQuality)iNewMin;
			SuitQuality newMax = (SuitQuality)iCurMax;
			Quality = (newMin, newMax);
		}
	}

	public class HandSummary
	{
		protected Dictionary<Suit, ModifiableSuitSummary> _modifiableSuits;

		public (int Min, int Max) OpeningPoints { get; protected set; }

		public bool? IsBalanced { get; protected set; }

		public bool? IsFlat { get; protected set; }

		public Dictionary<Suit, SuitSummary> Suits { get; protected set; }

		public HandSummary()
		{
			this.OpeningPoints = (0, int.MaxValue);
			this.IsBalanced = null;
			this.IsFlat = null;
			this.Suits = new Dictionary<Suit, SuitSummary>();
			this._modifiableSuits = new Dictionary<Suit, ModifiableSuitSummary>();
			foreach (Suit suit in BasicBidding.BasicSuits)
			{
				var suitSummary = new ModifiableSuitSummary();
				Suits[suit] = suitSummary;
				_modifiableSuits[suit] = suitSummary;
			}
			var ss = new ModifiableSuitSummary();
			Suits[Suit.Unknown] = ss;
			_modifiableSuits[Suit.Unknown] = ss;

			// TODO: Think this through...
		}

	}


	public class ModifiableHandSummary : HandSummary
	{
		public void ShowOpeningPoints(int min, int max)
		{
			OpeningPoints = (Math.Max(min, OpeningPoints.Min), Math.Min(max, OpeningPoints.Max));
		}
		public void ShowIsBalanced(bool isBalanced)
		{
			IsBalanced = isBalanced;
		}
		public void ShowIsFlat(bool isFlat)
		{
			IsFlat = isFlat;
		}
		public Dictionary<Suit, ModifiableSuitSummary> ModifiableSuits => this._modifiableSuits;
	}
}
