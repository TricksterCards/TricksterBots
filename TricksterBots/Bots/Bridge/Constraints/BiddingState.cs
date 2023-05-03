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
    public enum Convention { Natural, StrongOpen, NT, Stayman, Transfer, TakeoutDouble };



    
	public class Contract
	{
		public Bid Bid;    // Bid must be either CallType.NotActed or .Pass or .Bid
		public PositionState By;
		public bool Doubled;
		public bool Redoubled;

        public Contract()
        {
            this.Bid = new Bid(CallType.NotActed, BidForce.Nonforcing);
            this.By = null;
            this.Doubled = false;
            this.Redoubled = false;
        }
	}

    public class BiddingState
    {

        //   public Dictionary<Convention, Bidder> Conventions { get; protected set; }

        public List<Bidder> DefaultBidders = new List<Bidder>();

        public Dictionary<Direction, PositionState> Positions { get; }


        public PositionState Dealer { get; private set; }

        public PositionState NextToAct { get; private set; }

        public Contract GetContract()
        {
            Contract contract = new Contract();
            int historyLevel = 0;
            var position = NextToAct;
            int countPasses = 0;
            while (true)
            {
                position = position.RightHandOpponent;
                var bid = position.GetBidHistory(historyLevel);
                if (bid.CallType == CallType.NotActed)
                {
                    Debug.Assert(contract.Bid.CallType == CallType.NotActed);
                    break;
                }
                if (bid.CallType == CallType.Pass)
                {
                    countPasses++;
                    if (countPasses == 4)
                    {
                        contract.Bid = bid;
                        break;
                    }
                    else
                    {
                        countPasses = 0;
                    }
                }
                if (bid.CallType == CallType.Bid)
                {
                    contract.Bid = bid;
                    contract.By = position;
                    break;
                }
                if (bid.CallType == CallType.Double)
                {
                    contract.Doubled = true;
                }
                if (bid.CallType == CallType.Redouble)
                {
                    contract.Redoubled = true;
                }

                if (position == NextToAct)
                {
                    historyLevel += 1;
                }
            }
            return contract;
        }

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

            /*
            this.Conventions = new Dictionary<Convention, Bidder>();
            this.Conventions[Convention.Natural] = new NaturalOpen();
            this.Conventions[Convention.StrongOpen] = new StrongOpen();
            this.Conventions[Convention.Stayman] = new InitiateStayman();

            this.Conventions[Convention.Transfer] = new NaturalOvercall(); // TODO: HACK HACK HACK HACK!
            */
            this.DefaultBidders.Add(Natural.Bidder());
            this.DefaultBidders.Add(NoTrumpConventions.Bidder(NoTrumpBidder.NTType.Open1NT));
            this.DefaultBidders.Add(Strong.Bidder());
        }


        internal Dictionary<Bid, BidRuleGroup> AvailableBids(PositionState ps)
        {
            var options = new BidOptions();
            var bidRules = new List<BidRule>();

            var bidders = new List<Bidder>();


            if (ps.Partner.PartnerNextState != null)
            {
                bidders.Add(ps.Partner.PartnerNextState());
            }
            bidders.AddRange(DefaultBidders);

            // Now we look at every bidder.  For each one:
            //    1. If they don't conform then skip to next one
            //    2. If they do conform then see if is a redirect.  If so ignore rules
            //    3. Otherwise, if they conform and there is not a redirect then append the bid rules.

            // Look at the Partner's previous bid (if there is one) and if there
            // is a bidder specified, then do that first --- Need to redirect, etc here too but not yet...

            while (bidders.Count > 0)
            {
                var redirect = new List<Bidder>();
                foreach (var bidder in bidders)
                {
                    if (bidder.Applies(NextToAct))
                    {
                        bool didRedirect = false;
                        if (bidder.Redirects != null)
                        {
                            foreach (var r in bidder.Redirects)
                            {
                                var redirectBidder = r.Redirect(NextToAct);
                                if (redirectBidder != null)
                                {
                                    redirect.Add(redirectBidder);
                                    didRedirect = true;
                                }
                            }
                        }
                        if (!didRedirect && bidder.BidRules != null)
                        {
                            options.Add(bidder.Convention, bidder.NextConventionState, bidder.BidRules, NextToAct);
                        }
                    }
                    // TODO: Deal with redirect.  Where?  Here, or in Applies?  Not sure. 
                }
                bidders = redirect;
            }
            return options.GetChoices();
        }


        public Bid GetHackBid(string[] history, string expected)
        {
            Debug.WriteLine($"==== START TEST ==== Expect {expected}");
            if (history == null)
            {
                Debug.WriteLine("No historical bids.");
            }
            else
            {
                Debug.Write("Bids: ");
                foreach (var bidString in history)
                {
                    Debug.Write($"{bidString} ");
                }
                Debug.WriteLine("");
            }
            // If there is any history then we need to get those bids first and at the end evaluate the hand
            if (history != null)
            {
                foreach (var b in history)
                {
                    Debug.WriteLine($"--- Historical: {b}");
                    var bid = Bid.FromString(b);
                    var o = AvailableBids(NextToAct);
                    BidRuleGroup choice;
                    if (o.TryGetValue(bid, out choice) == false)
                    {
                        Debug.WriteLine($"*** ERROR: Did not find {b} in bid optoins.  Constructing a bid with state information");
                        var rule = new BidRule(bid, 1, new Constraint[0]);
                        choice = new BidRuleGroup(bid, Convention.Natural, null);
                        choice.Add(rule);
                    }
                    NextToAct.MakeBid(choice);
                    NextToAct = NextToAct.LeftHandOpponent;
                }
            }
            // Now we are actually ready to look at a hand and do somethihg

            var options = AvailableBids(NextToAct);
            var bidRule = NextToAct.ChooseBid(options);
            NextToAct.MakeBid(bidRule);

            if (bidRule.Bid.ToString() == expected)
            {
                Debug.WriteLine($"SUCCESS - Got expected {bidRule.Bid}");
            }
            else
            {
                Debug.WriteLine($"******** ERROR!  Expected {expected} but got {bidRule.Bid}");
            }
            Debug.WriteLine("");

            NextToAct = NextToAct.LeftHandOpponent;
            return bidRule.Bid;
        }


        internal void UpdateStateFromFirstBid()
        {
            for (int i = 0; i < 1000; i++)
            {
                var position = Dealer;
                var bidIndex = 0;
                bool someStateChanged = false;
                bool posStateChanged;
                while (position.UpdateBidIndex(bidIndex, out posStateChanged))
                {
                    someStateChanged |= posStateChanged;
                    position = position.LeftHandOpponent;
                    if (position == Dealer) { bidIndex++; }
                }
                if (!someStateChanged) { return; }
            }
            Debug.Assert(false);    // In infinite loop of updating but getting nowhere...
        }
    }
}
