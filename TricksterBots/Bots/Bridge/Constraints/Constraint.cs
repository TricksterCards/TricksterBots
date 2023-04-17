using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;


namespace TricksterBots.Bots.Bridge
{




    public abstract class Constraint
    {
        public abstract bool Conforms(Bid bid, PositionState ps, HandSummary hs, BiddingSummary bs);

	}


    public interface IShowsState 
    {
        void Update(Bid bid, PositionState ps, HandSummary hs, BiddingSummary bs);
	}

  

 
    /*

        public enum Convention
        {
            Stayman,
            JacobyTransfers,
            MichaelsCuebid,
            UnsualNoTrump
        }

        class NoTrumpResponse
        {
            static BidRule[] GenerateBids()
            {
                BidRule[] bids = {
                    new BidRule(2, Suit.Clubs, 100, new Points(PointRange.NTNoInvite), new Shape(Suit.Diamonds, 4, 6), new Shape(Suit.Hearts, 4, 4), new Shape(Suit.Spades, 4, 4)),
                    new BidRule(2, Suit.Clubs, 100, new Points(PointRange.NTInviteOrBetter), new Shape(Suit.Hearts, 4, 4), new Shape(Suit.Spades, 4, 5), new Flat(false)),
                    new BidRule(2, Suit.Clubs, 100, new Points(PointRange.NTInviteOrBetter), new Shape(Suit.Hearts, 0, 3), new Shape(Suit.Spades, 4, 4), new Flat(false)),
                    new BidRule(2, Suit.Diamonds, 100, new Shape(Suit.Hearts, 5)),
                    new BidRule(2, Suit.Hearts, 100, new Shape(Suit.Spades, 5)),
                    new BidRule(2, Suit.Spades, 100, new Points(PointRange.NTNoInvite), new Shape(Suit.Clubs, 6, 6), new Quality(SuitQuality.Decent, Suit.Clubs)),
                    new BidRule(2, Suit.Spades, 100, new Points(PointRange.NTNoInvite), new Shape(Suit.Clubs, 7)),
                    new BidRule(2, Suit.Spades, 100, new Points(PointRange.NTNoInvite), new Shape(Suit.Diamonds, 6, 6), new Quality(SuitQuality.Decent, Suit.Diamonds)),
                    new BidRule(2, Suit.Spades, 100, new Points(PointRange.NTNoInvite), new Shape(Suit.Diamonds, 7)),
                    new BidRule(2, Suit.Unknown, 100, new Points(PointRange.NTInvitational))
                };
                return bids;
            }
        }

        class StaymanResponse
        {
            static BidRule[] GenerateBids()
            {
                BidRule[] bids =
                {
                    new BidRule(2, Suit.Diamonds, 0, new Shape(Suit.Hearts, 0, 3), new Shape(Suit.Spades, 0, 3)),
                    new BidRule(2, Suit.Hearts, 0, new Shape(Suit.Hearts, 4)),
                    new BidRule(2, Suit.Spades, 0, new Shape(Suit.Hearts, 0, 3), new Shape(Suit.Spades, 4))
                };
                return bids;
            }
        }

        class TransferReponse
        {
            static BidRule[] GenerateBids()
            {
                BidRule[] bids =
                {
                    new BidRule(2, Suit.Hearts, 0, new PartnerBid(2, Suit.Diamonds)),
                    new BidRule(2, Suit.Spades, 0, new PartnerBid(2, Suit.Hearts)),
                    new BidRule(3, Suit.Hearts, 100, new PartnerBid(2, Suit.Diamonds), new Points(PointRange.MaxNTOpener), new Shape(4,5)),
                    new BidRule(3, Suit.Spades, 100, new PartnerBid(2, Suit.Hearts), new Points(PointRange.MaxNTOpener), new Shape(4,5))
                };
                return bids;
            }

        }
    */

}


