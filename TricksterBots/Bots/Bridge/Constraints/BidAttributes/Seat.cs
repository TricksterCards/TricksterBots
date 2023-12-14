using System;
using System.Collections.Generic;
using System.Linq;


namespace BridgeBidding
{
	public class Seat : StaticConstraint
	{
		private int[] seats;
		public Seat(params int[] seats)
		{
			this.seats = seats;
		}

		public override bool Conforms(Call call, PositionState ps)
		{
			return this.seats.Contains(ps.Seat);
		}
	}
}
