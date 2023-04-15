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
		}

		public override bool Conforms(Bid bid, HandSummary _, PositionState positionState)
		{
			return seats.Contains(positionState.Seat);
		}
	}
}
