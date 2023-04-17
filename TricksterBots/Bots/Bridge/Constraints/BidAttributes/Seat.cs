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

		public override bool Conforms(Bid bid, PositionState ps, HandSummary hs, BiddingSummary bs)
		{
			return seats.Contains(ps.Seat);
		}
	}
}
