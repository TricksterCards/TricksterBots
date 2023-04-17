using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots
{

	/*

	public class ShownState
	{
		public class SuitProperties
		{
			public (int Min, int Max) Shape { get; protected set; }
			// TODO: There are other properties like "Stopped", "Has Ace", "Quality" that can go here...
		}

		public ShownState()
		{
			this.Points = (0, int.MaxValue);
			this.Suits = new Dictionary<Suit, SuitProperties>();
			foreach (Suit suit in BasicBidding.BasicSuits)
			{
				Suits[suit] = new SuitProperties();
			}
		}



		// TODO: Potentially three different types of points
		public (int Min, int Max) Points { get; protected set; }

		public Dictionary<Suit, SuitProperties> Suits { get; }

	}

	public class ShowsState : ShownState
	{
		public ShowsShape(ShownState other)
		{
		}

		public void ShowsPoints(int min, int max)
		{
			Points = (Math.Max(min, Points.Min), Math.Min(max, Points.Max));
		}

		public void ShowsShape(Suit suit, int min, int max)
		{
			var curShape = Suits[suit].Shape;
			Suits[suit].Shape = (Math.Max(min, curShape.Min), Math.Min(max, curShape.Max));
			// TODO: Throw if max<min...
		}

		internal void Union(ShownState other)
		{
			_pointsMin = Math.Min(_pointsMin, other._pointsMin);
			_pointsMax = Math.Max(_pointsMax, other._pointsMax);
			foreach (Suit suit in SuitRank.stdSuits)
			{
				(int min, int max) shapeThis = this._suitShapes.TryGetValue(suit, out shapeThis) ? shapeThis : (0, 13);
				(int min, int max) shapeOther = other._suitShapes.TryGetValue(suit, out shapeOther) ? shapeOther : (0, 13);
				shapeThis.min = Math.Min(shapeThis.min, shapeOther.min);
				shapeThis.max = Math.Max(shapeThis.max, shapeOther.max);
				this._suitShapes[suit] = shapeThis;
			}
		}


	}
	*/

}
