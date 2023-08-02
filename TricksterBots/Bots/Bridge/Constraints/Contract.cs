using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TricksterBots.Bots.Bridge
{
    public struct Contract
    {
        public Bid Bid;    // Bid must be either Call.NotActed or .Pass or .Bid
        public PositionState By;
        public bool Doubled;
        public bool Redoubled;

		public Contract(Bid bid, PositionState by, bool doubled, bool redoubled)
		{
			this.Bid = bid;
			this.By = by;
			this.Doubled = doubled;
			this.Redoubled = redoubled;
		}

		//       public Contract()
		//       {
		//           this.Bid = new Bid(Call.NotActed);
		//          this.By = null;
		//           this.Doubled = false;
		//           this.Redoubled = false;
		//       }
	}
}
