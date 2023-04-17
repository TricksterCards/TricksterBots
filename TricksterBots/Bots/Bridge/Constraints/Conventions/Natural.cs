using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Trickster.Bots;
using Trickster.cloud;
using TricksterBots.Bots.Bridge.TricksterBots.Bots.Bridge;

namespace TricksterBots.Bots.Bridge
{

    public class Natural : Bidder
    {
        private NaturalOpen _open;
        private NaturalRespond _respond;

        public Natural() : base(BidConvention.None, 1000)
        {
            this._open = new NaturalOpen();
            this._respond = new NaturalRespond();
        }

        internal Natural(PositionRole role) : base(BidConvention.None, 1000)
        {
            this._open = null;
            this._respond = null;
            
        }

        public (int, int) Open1Suit = (13, 21);
        public (int, int) Open1NT = (15, 17);
        public (int, int) Open2Suit = (5, 10);
        public (int, int) Open2NT = (20, 21);
        public (int, int) OpenStrong = (22, int.MaxValue);
        public (int, int) LessThanOpen = (0, 12);

        // TODO: How much information should be added to call and how much should you be able to get from bidding summary?
        // Perhaps all data should come back from bidding state....  No Role here...
		public override IEnumerable<BidRule> GetRules(PositionState positionState)
		{
            if (positionState.Role == PositionRole.Opener)
            {
                return this._open.GetRules(positionState);
            } 
            else if (positionState.Role == PositionRole.Responder)
            {
                return this._respond.GetRules(positionState);
            }

			throw new NotImplementedException();
		}




	}



   
}
