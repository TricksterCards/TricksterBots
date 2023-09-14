using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;

namespace TricksterBots.Bots.Bridge
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
			return seats.Contains(ps.Seat);
		}
	}
}
