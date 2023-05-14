using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.Contracts;
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
    public enum Convention { Natural, Competative, StrongOpen, NT, Stayman, Transfer, TakeoutDouble };



    
	public class Contract
	{
		public Bid Bid;    // Bid must be either Call.NotActed or .Pass or .Bid
		public PositionState By;
		public bool Doubled;
		public bool Redoubled;

        public Contract()
        {
            this.Bid = new Bid(Call.NotActed, BidForce.Nonforcing);
            this.By = null;
            this.Doubled = false;
            this.Redoubled = false;
        }
	}


    public class RedirectGroupXXX
    {
        private List<PrescribedBidsFactory> _bidders = new List<PrescribedBidsFactory>();

        public void Add(PrescribedBidsFactory bid)
        {
            _bidders.Add(bid);
        }

        // TODO: Is this the best way to do this?  Seems kind of inefficient but OK fo now
        // always adding base biddders to partner bidder group...  
        public void Add(RedirectGroupXXX other)
        {
            foreach (var bidder in other._bidders)
            {
                Add(bidder);
            }
        }

        public Dictionary<Bid, BidRuleGroup> GetBids(PositionState ps)
        {
            Dictionary<Bid, BidRuleGroup> bids = new Dictionary<Bid, BidRuleGroup>();
            foreach (var bidder in _bidders)
            {
                var newBids = bidder().GetBids(ps);
                if (newBids != null)    // TODO: Why ever getting null?? Is this acceptable?
                {
                    foreach (var newBid in newBids)
                    {
                        if (!bids.ContainsKey(newBid.Key))
                        {
                            bids[newBid.Key] = newBid.Value;
                        }
                    }
                }
            }
            return bids;
        }
    }

    public class BiddingState
    {

        //   public Dictionary<Convention, Bidder> Conventions { get; protected set; }


        private RedirectGroupXXX _baseBiddingXXX = new RedirectGroupXXX();

        public Dictionary<Direction, PositionState> Positions { get; }


        public PositionState Dealer { get; private set; }

        public PositionState NextToAct { get; private set; }

        public bool PassEndsAuction()
        {
            var position = NextToAct;
            int countPasses = 0;
            while (countPasses < 3)
            {
                position = position.RightHandOpponent;
                var bid = position.GetBidHistory(0);
                if (bid.Call != Call.Pass)
                {
                    // If there has been a bid (or X or XX) followed by two passes then next pass ends auction
                    if (bid.Call == Call.NotActed) { 
                        return false;
                    }
                    return countPasses == 2;
                }
                countPasses++;
            }
            // 3 passes in a row, so next one will end auction with pass-out.
            return true;

        }

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
                if (bid.Call == Call.NotActed)
                {
                    Debug.Assert(contract.Bid.Call == Call.NotActed);
                    break;
                }
                if (bid.Call == Call.Pass)
                {
                    countPasses++;
                    if (countPasses == 4)
                    {
                        contract.Bid = bid;
                        break;
                    }
                }
                else
                { 
                    countPasses = 0;
                }
                if (bid.Call == Call.Bid)
                {
                    contract.Bid = bid;
                    contract.By = position;
                    break;
                }
                if (bid.Call == Call.Double)
                {
                    contract.Doubled = true;
                }
                if (bid.Call == Call.Redouble)
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
            this._baseBiddingXXX.Add(Natural.DefaultBidderXXX);
            this._baseBiddingXXX.Add(OpenAndOvercallNoTrump.DefaultBidderXXX);
            this._baseBiddingXXX.Add(StrongBidder.InitiateConvention);
            this._baseBiddingXXX.Add(TakeoutDouble.InitiateConvention);
            this._baseBiddingXXX.Add(Compete.DefatulBidderXXX);
            
        }



        internal Dictionary<Bid, BidRuleGroup> AvailableBids(PositionState ps)
        {
            // TODO: Always creating a new object and then copying all the default bidders into
            // that group so not the best implementation, but whatever...
            var bidders = new RedirectGroupXXX();
            // TODO: Clean this up.  Not the prettiest code but whatever...
            PrescribedBidsFactory nextState = ps.GetPartnerNextState();
            if (nextState != null)
            {
                bidders.Add(nextState);
            }
           
            bidders.Add(_baseBiddingXXX);
            var bids = bidders.GetBids(ps);

			if (!bids.ContainsKey(Bid.Pass))
			{
				// TODO: How bad is this?  Is this an emergency?
				// TODO: Always add a pass at the end of this function no matter what?  
				// If forcing then isn't really on optoin.  Is this the right place anyway?
				//Debug.WriteLine("** CREATING PASS SINCE NONE SPECIFIED **");
				var ruleGroup = new BidRuleGroup(Bid.Pass, Convention.Natural, null);
				ruleGroup.Add(new BidRule(Bid.Pass, 0, new Constraint[0]));
				bids[Bid.Pass] = ruleGroup;
			}
            return bids;

		}
		/*
		internal Dictionary<Bid, BidRuleGroup> AvailableBids(PositionState ps)
		{
			var options = new BidOptions();
			var bidRules = new List<BidRule>();

			var bidders = new List<PrescribedBids>();


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
							options.Add(bidder, NextToAct);
						}
					}
					// TODO: Deal with redirect.  Where?  Here, or in Applies?  Not sure. 
				}
				bidders = redirect;
			}
			return options.GetChoices();
		}
		*/

		public string GetHackBid(string[] history, string expected)
        {
            /*
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
            */
            // If there is any history then we need to get those bids first and at the end evaluate the hand
            if (history != null)
            {
                foreach (var b in history)
                {
                   // Debug.WriteLine($"--- Historical: {b}");
                    var bid = Bid.FromString(b);
                    var o = AvailableBids(NextToAct);
                    BidRuleGroup choice;
                    if (o.TryGetValue(bid, out choice) == false)
                    {
                        // TODO: THIS IS SUPER IMPORTANT TO GET BUT NEED TO IGNORE IT FOR A WHILE.

                        // TURN THIS BACK ON AT SOME POINT!  
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

          //  if (bidRule.Bid.ToString() != expected)
         //   {
          //      Debug.WriteLine($"SUCCESS - Got expected {bidRule.Bid}");
          //  }
          //  else
          //  {
          //      Debug.WriteLine($"******** ERROR!  Expected {expected} but got {bidRule.Bid}");
          //  }
            //Debug.WriteLine("");

            NextToAct = NextToAct.LeftHandOpponent;
            return bidRule.Bid.ToString();
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
