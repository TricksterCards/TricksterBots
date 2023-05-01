using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{

	public class HandSummary: IEquatable<HandSummary>
	{

        public class SuitSummary: IEquatable<SuitSummary>
        {
            internal (int Min, int Max) _quality;

            public (int Min, int Max) Shape { get; set; }
            public (int Min, int Max) DummyPoints { get; set; }

            public (int Min, int Max) LongHandPoints { get; set; }

            public (SuitQuality Min, SuitQuality Max) Quality
            {
                get { return ((SuitQuality)_quality.Min, (SuitQuality)_quality.Max); }
                set { _quality.Min = (int)value.Min; _quality.Max = (int)value.Max; }
            }

            public (int A, int B)? Keycards
            {
                get; set;
            }

            public bool? HaveQueen { get; set; }

            public SuitSummary()
            {
                this.Shape = (0, 13);
                this.DummyPoints = (0, 40);
                this.LongHandPoints = (0, 40);
                this.Quality = (SuitQuality.Poor, SuitQuality.Solid);
                this.Keycards = null;
            }
            // TODO: There are other properties like "Stopped", "Has Ace", that can go here...

            public SuitSummary(SuitSummary other)
            {
                this.Shape = other.Shape;
                this.DummyPoints = other.DummyPoints;
                this.LongHandPoints = other.LongHandPoints;
                this.Quality = other.Quality;
            }
		
            internal void Union(SuitSummary other)
            {
                this.Shape = UnionRange(this.Shape, other.Shape);
                this.DummyPoints = UnionRange(this.DummyPoints, other.DummyPoints);
                this.LongHandPoints = UnionRange(this.LongHandPoints, other.LongHandPoints);
                this._quality = UnionRange(this._quality, other._quality);
                this.HaveQueen = UnionBool(this.HaveQueen, other.HaveQueen);
                if (this.Keycards == null || other.Keycards == null)
                {
                    this.Keycards = null;
                }
                else
                {
                    Debug.Assert(this.Keycards == other.Keycards);
                }
            }


            internal void Intersect(SuitSummary other)
            {
                this.Shape = IntersectRange(this.Shape, other.Shape);
                this.DummyPoints = IntersectRange(this.DummyPoints, other.DummyPoints);
                this.LongHandPoints = IntersectRange(this.LongHandPoints, other.LongHandPoints);
                this._quality = IntersectRange(this._quality, other._quality);
            }

            public bool Equals(SuitSummary other)
            {
                return (this.Shape == other.Shape &&
					    this.DummyPoints == other.DummyPoints &&
						this.LongHandPoints == other.LongHandPoints &&
						this._quality == other._quality);
            }
        }

		public (int Min, int Max) HighCardPoints { get; set; }
		public (int Min, int Max) StartingPoints { get; set; }

		public bool? IsBalanced { get; set; }

		public bool? IsFlat { get; set; }

		// TODO: Perhaps things like this:
		public int? CountAces { get; set; }
			
		public int? CountKings { get; set; }

		public Dictionary<Suit, SuitSummary> Suits { get; protected set; }

		public HandSummary()
		{
			this.HighCardPoints = (0, 40);
			this.StartingPoints = (0, int.MaxValue);
			this.IsBalanced = null;
			this.IsFlat = null;
			this.CountAces = null;
			this.CountKings = null;
			this.Suits = new Dictionary<Suit, SuitSummary>();
			foreach (Suit suit in BasicBidding.Strains)
			{
				Suits[suit] = new SuitSummary();
			}
		}

		public HandSummary(HandSummary other)
		{
			this.HighCardPoints = other.HighCardPoints;
			this.StartingPoints = other.StartingPoints;
			this.IsBalanced = other.IsBalanced;
			this.IsFlat = other.IsFlat;
			this.CountAces = other.CountAces;
			this.CountKings = other.CountKings;
			this.Suits = new Dictionary<Suit, SuitSummary>();
			foreach (Suit suit in BasicBidding.Strains)
			{
				Suits[suit] = new SuitSummary(other.Suits[suit]);
			}
		}


		private static (int Min, int Max) UnionRange((int Min, int Max) r1, (int Min, int Max) r2)
		{
			return (Math.Min(r1.Min, r2.Min), Math.Max(r1.Max, r2.Max));
		}

		private static bool? UnionBool(bool? b1, bool? b2)
		{
			return (b1 == null || b2 == null || b1 != b2) ? null : b1;
		}

		private static int? UnionInt(int? i1, int? i2)
		{
			return (i1 == null || i2 == null || i1 != i2) ? null : i1;
		}


		public void Union(HandSummary other)
		{
			this.HighCardPoints = UnionRange(this.HighCardPoints, other.HighCardPoints);
			this.StartingPoints = UnionRange(this.StartingPoints, other.StartingPoints);
			this.IsBalanced = UnionBool(this.IsBalanced, other.IsBalanced);
			this.IsFlat = UnionBool(this.IsFlat, other.IsFlat);
			this.CountAces = UnionInt(this.CountAces, other.CountAces);
			this.CountKings = UnionInt(this.CountKings, other.CountKings);
			foreach (var suit in BasicBidding.Strains)
			{
				this.Suits[suit].Union(other.Suits[suit]);
			}
		}

		public void TrimShape()
		{
		
			int claimed = 0;
			foreach (var suit in BasicBidding.BasicSuits)
			{
				claimed += Suits[suit].Shape.Min;
			}
			foreach (var suit in BasicBidding.BasicSuits)
			{
				var shape = Suits[suit].Shape;
				if (shape.Max + claimed - shape.Min > 13)
				{
					var newMax = 13 - claimed + shape.Min;
					Suits[suit].Shape = (shape.Min, newMax);
				}
			}
		
		}

		private static (int Min, int Max) IntersectRange((int Min, int Max) r1, (int Min, int Max) r2)
		{
			return (Math.Max(r1.Min, r2.Min), Math.Min(r1.Max, r2.Max));
		}
		private static bool? IntersectBool(bool? b1, bool? b2)
		{ 
			return (b1 == null || b2 == null || b1 != b2) ? null : b1;
		}

		private static int? IntersectInt(int? v1, int? v2)
		{
			return (v1 == null || v2 == null || v1 != v2) ? null : v1;
		}

		public void Intersect(HandSummary other)
		{
			this.HighCardPoints = IntersectRange(this.HighCardPoints, other.HighCardPoints);
			this.StartingPoints = IntersectRange(this.StartingPoints, other.StartingPoints);
			this.IsBalanced = IntersectBool(this.IsBalanced, other.IsBalanced);
			this.IsFlat = IntersectBool(this.IsFlat, other.IsFlat);
			this.CountAces = IntersectInt(this.CountAces, other.CountAces);
			this.CountKings = IntersectInt(this.CountKings, other.CountKings);
			foreach (var suit in BasicBidding.Strains)
			{
				this.Suits[suit].Intersect(other.Suits[suit]);
			}
		}

        public bool Equals(HandSummary other)
        {
			if (this.HighCardPoints != other.HighCardPoints ||
				this.StartingPoints != other.StartingPoints ||
				this.IsBalanced != other.IsBalanced ||
				this.IsFlat != other.IsFlat ||
				this.CountAces != other.CountAces ||
				this.CountKings != other.CountKings) { return false; }
			foreach (var suit in BasicBidding.Strains)
			{
				if (!this.Suits[suit].Equals(other.Suits[suit])) return false;
			}
			return true;
        }
    }

}
