using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class TakeoutSuit : Constraint, IShowsState
    {
        private Suit? _suit;
  
        public TakeoutSuit(Suit? suit)
        {
            this._suit = suit;
            this.OnceAndDone = false;
        }



        // TODO: Move this to BasicBidding after massive merge
        public static Suit HigherRanking(Suit s1, Suit s2)
        {
            Debug.Assert(s1 != s2);
            Debug.Assert(s1 == Suit.Clubs || s1 == Suit.Diamonds || s1 == Suit.Hearts || s1 == Suit.Spades);
            Debug.Assert(s2 == Suit.Clubs || s2 == Suit.Diamonds || s2 == Suit.Hearts || s2 == Suit.Spades);
            switch (s1)
            {
                case Suit.Clubs:
                    return s2;
                case Suit.Diamonds:
                    return (s2 == Suit.Clubs) ? s1 : s2;
                case Suit.Hearts:
                    return (s2 == Suit.Spades) ? s2 : s1;
                case Suit.Spades:
                    return s1;
            }
            throw new ArgumentException();  // TODO: Is this OK?  Is it right?
        }

        public override bool Conforms(Bid bid, PositionState ps, HandSummary hs)
        {
            var suit = bid.SuitIfNot(_suit);
            var oppsSuits = PairSummary.Opponents(ps).ShownSuits;
            if (oppsSuits.Contains(suit)) { return false; }
            foreach (var other in BasicBidding.BasicSuits)
            {
                if (other != suit && !oppsSuits.Contains(other))
                {
                    // TODO: This may not be ideal but we always will prefer the higher ranking
                    // suit if all other things are equal.  Perhaps if low point range we would
                    // want to prefer lower suit.  
                    var betterSuit = new IsBetterSuit(suit, other, HigherRanking(suit, other), false);
                    if (!betterSuit.Conforms(bid, ps, hs)) { return false; }
                }
            }
            return true;
        }

        // TODO: This is not exactly right.  We PROBABLY have at least 4 in the suit...

        public void ShowState(Bid bid, PositionState ps, HandSummary.ShowState showHand, PairAgreements.ShowState showAgreements)
        {
            var suit = bid.SuitIfNot(_suit);
            showHand.Suits[suit].ShowShape(4, 11);
        }
    }

}
