using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static BridgeBidding.PairState;


namespace BridgeBidding
{
    // TODO: Line this up with trickser conventions, but re-declare for now for flexibility...
    public static class Convention
    {
        public static string Stayman = "Stayman";
        public static string Transfer = "Transfer";
        public static string Strong2Clubs = "Strong2Clubs";
        public static string UnusualNT = "UnusualNT";
        public static string Michaels = "Michaels";
        public static string TakeoutDouble = "TakeoutDouble";
    }


	public class BiddingState
    {

        //   public Dictionary<Convention, Bidder> Conventions { get; protected set; }


   //    private RedirectGroupXXX _baseBiddingXXX = new RedirectGroupXXX();
  
        public Dictionary<Direction, PositionState> Positions { get; }


        public PositionState Dealer { get; private set; }

        public PositionState NextToAct { get; private set; }

        public Dictionary<string, Call> Conventions { get; private set; }

        public Contract Contract { get; private set; }

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

        /*
        private static Dictionary<string, string> HACK_Conventions = new Dictionary<string, string>()
        {
            { "Stayman1NTOpen", "X" },
            { "Transfer1NTOpen", "X" },
            { "Stayman1NTOvercall", "X" },
            { "Transfer1NTOvercall", null },
            { "Stayman1NTBalancing", null },
            { "Transfer1NTBalancing", null },
            { "Stayman2NTOpen", "X" },
            { "Transfer2NTOpen", "X" },
            { "Stayman3NTOpen", "X" },
            { "Transfer3NTOpen", "X" },
            { "MichaelsCuebid", "1♠" }, // TODO: Only 1 level opening bids?
            { "UnusualNT", "1♠" }       // TODO: Same question here...  Only 1 of a suit?
        };
        */

        public BiddingState(Hand[] hands, Direction dealer, string vul /* Dictionary<string, string> conventions = null TODO: Add as parameter*/)
        {
            this.Positions = new Dictionary<Direction, PositionState>();
            this.Conventions = new Dictionary<string, Call>();
            this.Contract = new Contract();
            Debug.Assert(hands.Length == 4);
            var d = dealer;
            // TODO: Bidding system should be a parameter for each pair...  For now both are standard american
            IBiddingSystem biddingSystem = new StandardAmerican();
            var ns = new PairState(Pair.NorthSouth, biddingSystem);
            var ew = new PairState(Pair.EastWest, biddingSystem);
            for (int seat = 1; seat <= hands.Length; seat++)
            {
                PairState pairState = (d == Direction.North || d == Direction.South) ? ns : ew;
                this.Positions[d] = new PositionState(this, pairState, d, seat, IsVulnerable(vul, d), hands[seat - 1]);
                d = BridgeBidder.LeftHandOpponent(d);
            }
            this.Dealer = Positions[dealer];
            this.NextToAct = Dealer;



            // Hack for now.  TODO: Fill in this dictionary properly...
            Conventions["Stayman1NTOpen"] = Call.Double;
            Conventions["Transfer1NTOpen"] = Call.Double;
            Conventions["4WayTransfer1NTOpen"] = null;
            Conventions["Stayman2NTOpen"] = Call.Double;
            Conventions["Transfer2NTOpen"] = Call.Double;
            Conventions["Stayman1NTOvercall"] = Call.Double;
            Conventions["Transfer1NTOvercall"] = null;
            Conventions["Stayman1NTBalancing"] = Call.Double;
            Conventions["Transfer1NTBalancing"] = null;
            /*
            foreach (var convention in HACK_Conventions)
            {
                if (convention.Value == null)
                {
                    // TODO: Is this appropriate?  Seems a bit of a hack.  
                    this.Conventions[convention.Key] = new Bid(Call.NotActed);
                }
                else
                {
                    // TODO: What to do on failure?
                    this.Conventions[convention.Key] = Call.FromString(convention.Value);
                }
                    
            }
            */
        }



        public string SuggestBid(string[] history)
        {
            if (history != null)
            {
                foreach (var call in history)
                {
                    var bids = GetBidsForNextToAct();
                    var choice = bids.GetBidRuleSet(Call.FromString(call));
                    MakeBid(choice);
                }
            }
            var choices = GetBidsForNextToAct();
            var chosenCall = choices.BestCall;
            if (chosenCall == null)
            {
                chosenCall = Call.Pass;
                // TODO: Log something here...
            }
            MakeBid(choices.GetBidRuleSet(chosenCall));
            return chosenCall.ToString();
        }


        private void MakeBid(BidRuleSet bidRuleSet)
        {
            NextToAct.MakeBid(bidRuleSet);
            Contract.MakeCall(bidRuleSet.Call, NextToAct);
            NextToAct = NextToAct.LeftHandOpponent;
        }


        internal BidChoices GetBidsForNextToAct()
        {
            return NextToAct.GetBidChoices();
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
