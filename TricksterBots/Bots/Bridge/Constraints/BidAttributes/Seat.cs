using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;

namespace TricksterBots.Bots.Bridge
{
	public class Seat : Constraint
	{
		private int[] seats;
		public Seat(params int[] seats)
		{
			this.seats = seats;
			this.StaticConstraint = true;
		}

		public override bool Conforms(Call call, PositionState ps, HandSummary hs)
		{
			return seats.Contains(ps.Seat);
		}
	}
}
