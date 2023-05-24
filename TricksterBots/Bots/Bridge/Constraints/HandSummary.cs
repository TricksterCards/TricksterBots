using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using Trickster.Bots;
using Trickster.cloud;
using static Trickster.Bots.InterpretedBid;

namespace TricksterBots.Bots.Bridge
{
	public abstract class State
	{
        public enum CombineRule
        {
            Show,       // If left or right is null the use other.  If both non-null then use smallest min, largest max
            CommonOnly, // If either left or right is null, result is null.  Otherwise smallest min, largest max
            Merge,      // If either left or right is null then use other.  If both non-null then use largest min, smallest max
        }

        protected static (int Min, int Max)? CombineRange((int Min, int Max)? a, (int Min, int Max)? b, CombineRule cr)
        {
            if (a != null && b != null)
            {
                (int Min, int Max) rangeA = ((int Min, int Max))a;
                (int Min, int Max) rangeB = ((int Min, int Max))b;
                if (cr == CombineRule.Merge)
                {
                    return (Math.Max(rangeA.Min, rangeB.Min), Math.Min(rangeA.Max, rangeB.Max));
                }
                return (Math.Min(rangeA.Min, rangeB.Min), Math.Max(rangeA.Max, rangeB.Max));
            }
            // If we are only merging common properties then if either is null, the result is null
            if (cr == CombineRule.CommonOnly)
            {
                return null;
            }
            return (a == null) ? b : a;
        }



        protected static bool? CombineBool(bool? b1, bool? b2, CombineRule cr)
        {
            if (b1 == null)
            {
                return (cr == CombineRule.CommonOnly) ? null : b2;
            }
            if (b2 == null)
            {
                return (cr == CombineRule.CommonOnly) ? null : b1;
            }
            // TODO: Is this right?  We know nothing if conflicting information?  Seems reasonable...
            return b1;
        }

        protected static int? CombineInt(int? i1, int? i2, CombineRule cr)
        {
            if (i1 == null)
            {
                return (cr == CombineRule.CommonOnly) ? null : i2;
            }
            if (i2 == null)
            {
                return (cr == CombineRule.CommonOnly) ? null : i1;
            }
            // TODO: Is this right?  We know nothing if conflicting information?  Seems reasonable...
            return i1;
        }


    }

    public class HandSummary: State, IEquatable<HandSummary>
	{


		public class ShowState
		{
			public HandSummary HandSummary { get; private set; }
			public Dictionary<Suit, SuitSummary.ShowState> Suits { get; protected set; }

			public ShowState(HandSummary startState = null)
			{
			
				this.HandSummary = (startState == null) ? new HandSummary() : new HandSummary(startState);
				this.Suits = new Dictionary<Suit, SuitSummary.ShowState>();
				foreach (var strain in BasicBidding.Strains)
				{
					this.Suits[strain] = new SuitSummary.ShowState(HandSummary.Suits[strain]);
				}
			}

			public void ShowStartingPoints(int min, int max)
			{
				HandSummary.StartingPoints = CombineRange(HandSummary.StartingPoints, (min, max), CombineRule.Show);
			}
			public void ShowHighCardPoints(int min, int max)
			{
				HandSummary.HighCardPoints = CombineRange(HandSummary.HighCardPoints, (min, max), CombineRule.Show);
			}
			public void ShowIsBalanced(bool isBalanced)
			{
				// TODO: This needs to union?  What if one is true and one is false???
				HandSummary.IsBalanced = CombineBool(HandSummary.IsBalanced, isBalanced, CombineRule.Show);
			}

			public void ShowIsFlat(bool isFlat)
			{
				HandSummary.IsFlat = CombineBool(HandSummary.IsFlat, isFlat, CombineRule.Show);
			}

			public void ShowCountAces(int countAces)
			{
				HandSummary.CountAces = countAces;
			}

			public void ShowCountKings(int countKings)
			{
				HandSummary.CountKings = countKings;
			}

			public void Combine(HandSummary other, CombineRule combineRule)
			{
                HandSummary.Combine(other, combineRule);
            }

        }
		

        public class SuitSummary: IEquatable<SuitSummary>
        {
            public class ShowState
            {
				private SuitSummary _suitSummary;

				internal ShowState(SuitSummary suitSummary)
                {
                    _suitSummary = suitSummary;
                }

                public void ShowShape(int min, int max)
                {
                    _suitSummary.Shape = CombineRange(_suitSummary.Shape, (min, max), CombineRule.Show);
                }

                public void ShowDummyPoints(int min, int max)
                {
                    _suitSummary.DummyPoints = CombineRange(_suitSummary.DummyPoints, (min, max), CombineRule.Show);
                }

                public void ShowLongHandPoints(int min, int max)
                {
                    _suitSummary.LongHandPoints = CombineRange(_suitSummary.LongHandPoints, (min, max), CombineRule.Show);
                }

                public void ShowQuality(SuitQuality min, SuitQuality max)
                {
                    _suitSummary._quality = CombineRange(_suitSummary._quality, ((int)min, (int)max), CombineRule.Show);
                }

			
				public void ShowKeycards(int a, int b)
				{
					// TODO: What to do here?????
					_suitSummary.Keycards = (a, b);
				}
				
				public void ShowHaveQueen(bool haveQueen)
				{
					_suitSummary.HaveQueen = CombineBool(_suitSummary.HaveQueen, haveQueen, CombineRule.Show);
				}
				public void ShowStopped(bool stopped)
				{
					_suitSummary.Stopped = CombineBool(_suitSummary.Stopped, stopped, CombineRule.Show);
				}

            }

            internal (int Min, int Max)? _quality;

            public (int Min, int Max)? Shape { get; protected set; }
            public (int Min, int Max)? DummyPoints { get; protected set; }

            public (int Min, int Max)? LongHandPoints { get; protected set; }


			public (int Min, int Max) GetShape()
			{
				if (Shape == null) return (0, 13);
				return ((int Min, int Max))Shape;
			}

			public (int Min, int Max) GetDummyPoints()
			{
				if (DummyPoints == null) return (0, int.MaxValue);
				return ((int Min, int Max))DummyPoints;
			}


			public (int Min, int Max) GetLongHandPoints()
			{
				if (LongHandPoints == null) return (0, int.MaxValue);
				return ((int, int))LongHandPoints;
			}
			public (SuitQuality Min, SuitQuality Max) GetQuality()
			{
				if (_quality == null) { return (SuitQuality.Poor, SuitQuality.Solid); }
				return ((SuitQuality, SuitQuality))Quality;
			}


            public (SuitQuality Min, SuitQuality Max)? Quality
            {
                get {
					if (_quality == null) { return null; }
					var q = ((int Min, int Max))_quality; 
					return ((SuitQuality)q.Min, (SuitQuality)q.Max);
				}
            }

            public (int A, int B)? Keycards
            {
                get; protected set;
            }

            public bool? HaveQueen { get; protected set; }

			public bool? Stopped { get; protected set; }

            public SuitSummary()
            {
				this.Shape = null;
				this.DummyPoints = null;
                this.LongHandPoints = null;
                this._quality = null;
                this.Keycards = null;
				this.HaveQueen = null;
				this.Stopped = null;
            }
            // TODO: There are other properties like "Stopped", "Has Ace", that can go here...

            public SuitSummary(SuitSummary other)
            {
                this.Shape = other.Shape;
                this.DummyPoints = other.DummyPoints;
                this.LongHandPoints = other.LongHandPoints;
                this._quality = other._quality;
				this.Keycards= other.Keycards;
				this.HaveQueen= other.HaveQueen;
				this.Stopped = other.Stopped;
            }
		
            internal void Combine(SuitSummary other, CombineRule cr)
            {
                this.Shape = CombineRange(this.Shape, other.Shape, cr);
                this.DummyPoints = CombineRange(this.DummyPoints, other.DummyPoints, cr);
                this.LongHandPoints = CombineRange(this.LongHandPoints, other.LongHandPoints, cr);
                this._quality = CombineRange(this._quality, other._quality, cr);
                this.HaveQueen = CombineBool(this.HaveQueen, other.HaveQueen, cr);
				this.Stopped = CombineBool(this.Stopped, other.Stopped, cr);
                if (this.Keycards == null)
                { 
                    this.Keycards = (cr == CombineRule.CommonOnly) ? null : other.Keycards;
                }
                else
                {
					if (other.Keycards == null)
					{
						if (cr == CombineRule.CommonOnly) { this.Keycards = null; }
					}
					else
					{
						// TODO: What is the right thing here?  Both are non-null.  Do they have to be the same?
						// if they are not then what to do
                        Debug.Assert(this.Keycards == other.Keycards);
                    }
                }
            }

/*
            internal void Intersect(SuitSummary other)
            {
                this.Shape = IntersectRange(this.Shape, other.Shape);
                this.DummyPoints = IntersectRange(this.DummyPoints, other.DummyPoints);
                this.LongHandPoints = IntersectRange(this.LongHandPoints, other.LongHandPoints);
                this._quality = IntersectRange(this._quality, other._quality);
				this.HaveQueen = IntersectBool(this.HaveQueen, other.HaveQueen);
				this.Stopped = IntersectBool(this.Stopped, other.Stopped);
            }
*/
            public bool Equals(SuitSummary other)
            {
                return (this.Shape == other.Shape &&
					    this.DummyPoints == other.DummyPoints &&
						this.LongHandPoints == other.LongHandPoints &&
						this._quality == other._quality);
				// TODO: HaveQueen??? Stopped???
            }
        }

		public (int Min, int Max)? HighCardPoints { get; protected set; }
		public (int Min, int Max)? StartingPoints { get; protected set; }

		public (int Min, int Max) GetHighCardPoints()
		{
			if (HighCardPoints == null) { return (0, int.MaxValue); }
			return ((int, int))HighCardPoints;
		}

		public (int Min, int Max) GetStartingPoints()
        {
            if (StartingPoints == null) { return (0, int.MaxValue); }
            return ((int, int))StartingPoints;
        }

        public bool? IsBalanced { get; protected set; }

		public bool? IsFlat { get; protected set; }

		// TODO: Perhaps things like this:
		public int? CountAces { get; protected set; }
			
		public int? CountKings { get; protected set; }

		public Dictionary<Suit, SuitSummary> Suits { get; protected set; }

		public HandSummary()
		{
			this.HighCardPoints = null; //(0, 40);
			this.StartingPoints = null; // (0, int.MaxValue);
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



			/*
		protected static (int Min, int Max) ShowRange((int Min, int Max)? r1, (int Min, int Max) r2)
		{
            if (r1 == null) { return r2; }
			(int Min, int Max) range1 = ((int Min, int Max))r1;
            return (Math.Min(range1.Min, r2.Min), Math.Max(range1.Max, r2.Max));
        }
			*/





		protected void Combine(HandSummary other, CombineRule cr)
		{
			this.HighCardPoints = CombineRange(this.HighCardPoints, other.HighCardPoints, cr);
			this.StartingPoints = CombineRange(this.StartingPoints, other.StartingPoints, cr);
			this.IsBalanced = CombineBool(this.IsBalanced, other.IsBalanced, cr);
			this.IsFlat = CombineBool(this.IsFlat, other.IsFlat, cr);
			this.CountAces = CombineInt(this.CountAces, other.CountAces, cr);
			this.CountKings = CombineInt(this.CountKings, other.CountKings, cr);
			foreach (var suit in BasicBidding.Strains)
			{
				this.Suits[suit].Combine(other.Suits[suit], cr);
			}
		}


		// TODO: Probably move this to hand evaluation???  Not sure where it should live...
		public void TrimShape()
		{
			/* -- Move this to hand evaluation??  I think so...
			int claimed = 0;
			foreach (var suit in BasicBidding.BasicSuits)
			{
				claimed += Suits[suit].GetShape().Min;
			}
			foreach (var suit in BasicBidding.BasicSuits)
			{
				var shape = Suits[suit].GetShape();
				if (shape.Max + claimed - shape.Min > 13)
				{
					var newMax = 13 - claimed + shape.Min;
					Suits[suit].Shape = (shape.Min, newMax);
				}
			}
			*/
		}

/*
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
*/
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
