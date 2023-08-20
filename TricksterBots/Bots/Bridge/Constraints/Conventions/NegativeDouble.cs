using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class NegativeDouble : Respond
    {
        
        // IMPORTANT:  The caller needs to have set a default bid handler for the BidChoices object
        // before adding these rules.  Also, for the sequence 1m-1H-X this code will also define the
        // exclusive rule for bidding 1S.  This will be handled by the logic that does not add existing
        // bids, but it is something to be aware of...

        // This method should only be called when a responder bid over an opener
        // TODO: Is there a negative double for 1NT - I think only suits....
        public static IEnumerable<BidRule> InitiateConvention(PositionState ps)
        {
            // TODO: Need to implement doubles beyond 1 level.

            Debug.Assert(ps.BiddingState.Contract.IsOpponents(ps));
            var bids = new List<BidRule>();
            var contractBid = ps.BiddingState.Contract.Bid;
            if (contractBid != null && contractBid.Level == 1 && contractBid.Strain != Strain.NoTrump)
            {
                var overcallSuit = contractBid.Suit;
                var openSuit = ((Bid)ps.Partner.LastCall).Suit;
                if (overcallSuit == Suit.Diamonds)
                {
                    Debug.Assert(openSuit == Suit.Clubs);
                    bids.Add(Forcing(Call.Double, Points(Respond1Level), Shape(Suit.Hearts, 4), Shape(Suit.Spades, 4), ShowsSuit(Suit.Hearts), ShowsSuit(Suit.Spades)));
                }
                else if (overcallSuit == Suit.Hearts)
                {
                    bids.Add(Forcing(Call.Double, Points(Respond1Level), Shape(Suit.Spades, 4), ShowsSuit(Suit.Spades)));
                    bids.Add(Forcing(1, Suit.Spades, Points(Respond1Level), Shape(5, 11)));
                }
                else if (openSuit == Suit.Hearts)   // If this is the case we opened 1H and 1S overcall
                {
                    bids.Add(Forcing(Call.Double, Points(NewSuit2Level), Shape(Suit.Clubs, 4, 9), Shape(Suit.Diamonds, 4, 9), ShowsSuit(Suit.Clubs), ShowsSuit(Suit.Diamonds)));
                }
                else
                {
                    bids.Add(Forcing(Call.Double, Points(Respond1Level), Shape(Suit.Hearts, 4), ShowsSuit(Suit.Hearts)));
                    // TODO: Raise1 Point range name is lame.  Clean this up - shows 6-19 points...
                    bids.Add(Forcing(Call.Double, Points(Raise1), Shape(Suit.Hearts, 5, 11), ShowsSuit(Suit.Hearts)));
                }
            }
            return bids;
        }
    }
}
