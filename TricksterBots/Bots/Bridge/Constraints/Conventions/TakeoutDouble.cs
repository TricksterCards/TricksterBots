using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots.Bridge
{
    public class TakeoutDouble: Bidder
    {
        private static (int, int) TakeoutRange = (12, 16);
        private static (int, int) StrongTakeout = (17, 100);
        private static (int, int) MinimumTakeout = (11, 16);
        private static (int, int) MediumTakeout = (17, 19);
        private static (int, int) MaximumTakeout = (20, 100);



        public static IEnumerable<BidRule> InitiateConvention(PositionState ps)
        {
            var bids = new List<BidRule>();
            if (ps.IsOpponentsContract)
            {
                var contractBid = ps.BiddingState.Contract.Bid;
                if (contractBid.Level <= 3 && contractBid.Suit is Suit suit)
                {
                    bids.AddRange(Takeout(ps, contractBid.Level));
                }
            }
            return bids;
        }


        private static IEnumerable<BidRule> Takeout(PositionState ps, int Level)
        {
            var bids = new List<BidRule>
            {
                PartnerBids(Call.Double, Call.Pass, Respond),
                Forcing(Bid.Double, Points(StrongTakeout))
			};


			var rule = Forcing(Bid.Double, Points(TakeoutRange), BidAvailable(4, Suit.Clubs));
			var oppsSummary = PairSummary.Opponents(ps);
			foreach (var s in BasicBidding.BasicSuits)
			{
                if (oppsSummary.ShownSuits.Contains(s))
                {
                    rule.AddConstraint(Shape(s, 0, 4));
                }
                else
                {
					rule.AddConstraint(Shape(s, 3, 4));
				}
			}
			bids.Add(rule);
              
            // TODO: Should this be 2 or 1 for MinBidLevel?  Or is this really based on opponent bids?
            // If opponenets have shown a weak hand...
            // If opponents have shown a strong hand...
            // If opponents have bid twice...

 
            return bids;

        }

        public static (int, int) MinLevel = (0, 8);
        public static (int, int) NoTrump1 = (6, 10);
        public static (int, int) NoTrump2 = (11, 12);
        public static (int, int) InviteLevel = (9, 11);
        public static (int, int) GameLevel = (12, 40);
        public static (int, int) Game3NT = (13, 40);

        private static BidChoices Respond(PositionState ps)
        {
            var choices = new BidChoices(ps);
            choices.AddRules(new BidRule[]
            {
                DefaultPartnerBids(goodThrough: new Bid(4, Suit.Hearts), DoublerRebid),

                Signoff(Call.Pass, RuleOf9()),
                // TODO: FOR NOW WE WILL JUST BID AT THE NEXT LEVEL REGARDLESS OF POINTS...
                // TODO: Need LongestSuit()...
                // TODO: Should this be TakeoutSuit()...
                Nonforcing(1, Suit.Diamonds, TakeoutSuit(), Points(MinLevel)),
                Nonforcing(1, Suit.Hearts, TakeoutSuit(), Points(MinLevel)),
                Nonforcing(1, Suit.Spades, TakeoutSuit(), Points(MinLevel)),


                Nonforcing(1, Suit.Unknown, Balanced(), OppsStopped(), Points(NoTrump1)),

                Nonforcing(2, Suit.Clubs, TakeoutSuit(), Points(MinLevel)),
                Nonforcing(2, Suit.Diamonds, TakeoutSuit(), Jump(0), Points(MinLevel)),
                Nonforcing(2, Suit.Diamonds, TakeoutSuit(), Jump(1), Points(InviteLevel)),
                Nonforcing(2, Suit.Hearts, TakeoutSuit(), Jump(0), Points(MinLevel)),
                Nonforcing(2, Suit.Hearts, TakeoutSuit(), Jump(1), Points(InviteLevel)),
                Nonforcing(2, Suit.Spades, TakeoutSuit(), Jump(0), Points(MinLevel)),
                Nonforcing(2, Suit.Spades, TakeoutSuit(), Jump(1), Points(InviteLevel)),


                Nonforcing(2, Suit.Unknown, Balanced(), OppsStopped(), Points(NoTrump2)),

                // TODO: Game bids
                Signoff(4, Suit.Hearts, TakeoutSuit(), Points(GameLevel)),
                Signoff(4, Suit.Spades, TakeoutSuit(), Points(GameLevel)),

                Signoff(3, Suit.Unknown, Balanced(), OppsStopped(), Points(Game3NT))
            }) ;
            // Many strong bids can be done with pure competition.
            // TODO: Think through this - is this really what we want?
           //  FOR NOW TAKE THIS OUT AND TRY TO COVER THE BASES... choices.AddRules(Compete.CompBids);
            return choices;         
        }




        private static IEnumerable<BidRule> DoublerRebid(PositionState ps)
        {
            return new BidRule[]
            {
     
                DefaultPartnerBids(Call.Double, AdvancerRebid),


                // TODO: Clean this up... For now just majors...  Clean up range...
                Signoff(4, Suit.Hearts, Fit(), Partner(HasShownSuit()), PairPoints((25, 30))),
                Signoff(4, Suit.Spades, Fit(), Partner(HasShownSuit()), PairPoints((25, 30))),

                // CANT BE - Invitational(2, Suit.Clubs, RaisePartner(), Points(MediumTakeout)),
                Invitational(2, Suit.Diamonds, RaisePartner(), DummyPoints(MediumTakeout)),
                Invitational(2, Suit.Hearts,   RaisePartner(), DummyPoints(MediumTakeout)),
                Invitational(2, Suit.Spades,   RaisePartner(), DummyPoints(MediumTakeout)),

                Invitational(3, Suit.Clubs,    RaisePartner(), DummyPoints(MediumTakeout)),
                Invitational(3, Suit.Diamonds, RaisePartner(), DummyPoints(MediumTakeout)),
                Invitational(3, Suit.Diamonds, RaisePartner(2), DummyPoints(MaximumTakeout)),
                Invitational(3, Suit.Hearts,   RaisePartner(), DummyPoints(MediumTakeout)),
                Invitational(3, Suit.Hearts,   RaisePartner(2), DummyPoints(MaximumTakeout)),
                Invitational(3, Suit.Spades,   RaisePartner(), DummyPoints(MediumTakeout)),
                Invitational(3, Suit.Spades,   RaisePartner(2), DummyPoints(MaximumTakeout)),
                // TODO: Bid new suits for strong hands...  Bid NT?  

                // TODO: Forcing?  What to do here...
                // TODO: What is the lowest suit we could do here?  1C X Pass 1D is all I can think of...
                Invitational(1, Suit.Hearts, Shape(5, 11), Points(MediumTakeout)),
                Invitational(1, Suit.Spades, Shape(5, 11), Points(MediumTakeout)),
                Invitational(2, Suit.Clubs, Shape(5, 11), Points(MediumTakeout)),
                Invitational(2, Suit.Diamonds, Shape(5, 11), Points(MediumTakeout)),
                Invitational(2, Suit.Hearts, Jump(0), Shape(5, 11), Points(MediumTakeout)),
                Invitational(2, Suit.Spades, Jump(0), Shape(5, 11), Points(MediumTakeout)),

                // TODO: Need stronger bids here...

                Signoff(Call.Pass, Points(MinimumTakeout)),

                // TODO: THIS IS WHERE I START OFF - BB2 - DEAL 21 NEEDS STRONG RESPONSE...
            };
        }

        private static IEnumerable<BidRule> AdvancerRebid(PositionState ps)
        {
            return Compete.CompBids(ps);
        }
        // TODO: Interference...
        /*
        private static PrescribedBids RespondWithInterference()
        {
            var pb = new PrescribedBids();
            pb.BidRules.Add(Signoff(Bid.Pass, new Constraint[0]));   // TODO: Do something here
            return pb;
        }
        */
    }
}
