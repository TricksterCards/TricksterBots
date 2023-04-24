using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    // TODO: Line this up with trickser conventions, but re-declare for now for flexibility...
    public enum Convention { Natural, StrongOpen, Stayman, Transfer };

    public class BiddingState
    {

        public Dictionary<Convention, Bidder> Conventions { get; protected set; }

        public Dictionary<Direction, PositionState> Positions { get; }


        public PositionState Dealer { get; private set; }

        public PositionState NextToAct { get; private set; }



        public static bool IsVulnerable(string vul, Direction direction)
        {
            switch (vul)
            {
                case "None":
                case "Love":
                case "-":
                    return false;
                case "All":
                case "Both":
                    return true;
                case "NS":
                    return (direction == Direction.North || direction == Direction.South);
                case "EW":
                    return (direction == Direction.East || direction == Direction.West);
                default:
                    // TODO: Throw???
                    Debug.Assert(false);    // Should never get here...
                    return false;
            }

        }

        public BiddingState(Hand[] hands, Direction dealer, string vul)
        {
            this.Positions = new Dictionary<Direction, PositionState>();
            Debug.Assert(hands.Length == 4);
            var d = dealer;
            for (int i = 0; i < hands.Length; i++)
            {
                this.Positions[d] = new PositionState(this, d, i + 1, IsVulnerable(vul, d), hands[i]);
                d = BasicBidding.LeftHandOpponent(d);
            }
            this.Dealer = Positions[dealer];
            this.NextToAct = Dealer;

            this.Conventions = new Dictionary<Convention, Bidder>();
            this.Conventions[Convention.Natural] = new NaturalOpen();
            this.Conventions[Convention.StrongOpen] = new StrongOpen();
            this.Conventions[Convention.Stayman] = new InitiateStayman();

            this.Conventions[Convention.Transfer] = new NaturalOvercall(); // TODO: HACK HACK HACK HACK!


        }


        internal Dictionary<Bid, BidRuleGroup> AvailableBids()
        {
            var bidRules = new List<BidRule>();
            // Look at the Partner's previous bid (if there is one) and if there
            // is a bidder specified, then do that first --- Need to redirect, etc here too but not yet...
           
            foreach (var bidder in this.Conventions.Values)
            {
                if (bidder.Applies(NextToAct))
                {
                    bidRules.AddRange(bidder.BidRules);
                }
                // TODO: Deal with redirect.  Where?  Here, or in Applies?  Not sure. 

            }
            return BidRuleGroup.BidsGrouped(bidRules);


        }

        public Bid GetHackBid(string[] history, string expected)
        {
            Debug.WriteLine($"======================= {expected}");
            // If there is any history then we need to get those bids first and at the end evaluate the hand
            if (history != null)
            {
                foreach (var b in history)
                {
                    Debug.WriteLine($"+++ HISTORY: {b}");
                    var bid = Bid.FromString(b);
                    var o = AvailableBids();
                    if (o == null || o.ContainsKey(bid) == false)
                    {
                        Debug.Write("GJLGLGJ");
                    }
                    NextToAct.MakeBid(o[bid]);
                    NextToAct = NextToAct.LeftHandOppenent;
                }
            }
            // Now we are actually ready to look at a hand and do somethihg

            var options = AvailableBids();
            var bidRule = NextToAct.ChooseBid(options);


            if (bidRule.Bid.ToString() == expected)
            {
                Debug.WriteLine(bidRule.Bid);
            }
            else
            {
                Debug.WriteLine($"Expected {expected} but got {bidRule.Bid}");
            }
            NextToAct.MakeBid(bidRule);
            NextToAct = NextToAct.LeftHandOppenent;
            return bidRule.Bid;
        }
    }
}
