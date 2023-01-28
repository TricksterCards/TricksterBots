using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Policy;
using Trickster.cloud;
using TricksterBots.Bots;
using static Trickster.Bots.InterpretedBid;
using static TricksterBots.Bots.NoTrump;

namespace Trickster.Bots
{
    internal class JacobyTransfer
    {
        // The caller of this function has determined that NoTrump systems are on, so there is no need to check
        // for interference.
        public static void InitiateTransfer(InterpretedBid call, NoTrump nt)
        {
            if (call.Level == nt.Level + 1)
            {
                if (call.Suit == Suit.Diamonds || call.Suit == Suit.Hearts)
                { 
					/*
					call.BidMessage = BidMessage.Forcing;
					var transferSuit = call.Suit == Suit.Diamonds ? Suit.Hearts : Suit.Spades;
                    call.SetHandShape(transferSuit, 5, 13);
                    call.Description = $"Transfer to {Card.SuitSymbol(transferSuit)}; 5+ {transferSuit}";
                    call.PartnersCall = c => AcceptMajorTransfer(c, nt, transferSuit);
                    */
					var transferSuit = call.Suit == Suit.Diamonds ? Suit.Hearts : Suit.Spades;
					call.SetHandShape(transferSuit, 5, 13);
					nt.Forcing(call, HandRange.ResponderAll, c => AcceptMajorTransfer(c, nt, transferSuit),
                        $"Transfer to {Card.SuitSymbol(transferSuit)}; 5+ {transferSuit}");
			    }
                else if (!nt.Use4WayTansfers && call.Suit == Suit.Spades)
                {
					call.HandShape[Suit.Hearts].Max = 4;
					call.HandShape[Suit.Spades].Max = 4;
                    call.Validate = hand =>
                    {
                        //  validate matched hands have 6+ cards in a minor
                        var counts = BasicBidding.CountsBySuit(hand);
                        return counts[Suit.Clubs] >= 6 || counts[Suit.Diamonds] >= 6;
                    };
                    nt.Forcing(call, HandRange.ResponderNoGame, c => AcceptMinorTransfer(c, nt, Suit.Clubs),
                        " to 3♣; 6+ Clubs or Diamonds");
				}
                else if (nt.Use4WayTansfers && (call.Suit == Suit.Spades || call.Suit == Suit.Unknown))
                {
                    var transferSuit = call.Suit == Suit.Spades ? Suit.Clubs : Suit.Diamonds;
					call.HandShape[Suit.Hearts].Max = 4;
					call.HandShape[Suit.Spades].Max = 4;
                    call.HandShape[transferSuit].Min = 6;
                    nt.Forcing(call, HandRange.ResponderNoGame, c => AcceptMinorTransfer(c, nt, transferSuit));
                    // TODO: Also do this with slam invitational points.  
				}
            }
        }



        private static void AcceptMajorTransfer(InterpretedBid call, NoTrump nt, Suit transferSuit)
        {

            // If there is any other interference then punt
            // TODO: Perhaps look for opportunities to super-accept...
            if (call.RhoBid)
            {
                CompetitiveAuction.HandleInterference(call);
                return;
            }

            // If RHO doubled then conditionally accept the transfer.  Pass if only two cards in the suit.
            int minKnown = 2;
            if (call.RhoDoubled)
            {
                if (call.IsPass)
                {
                    call.SetHandShape(transferSuit, 2);
                    // YES!  This pass is forcing...
                    nt.Forcing(call, HandRange.OpenerAll, c => DescribeTransfer(c, nt, transferSuit, minKnown),
                        $"pass transfer to {transferSuit} indicating no fit after opponent X");
                    return;
                }
                minKnown = 3;
            }

            if (call.Is(nt.Level + 1, transferSuit))
            {
                call.SetHandShape(transferSuit, minKnown, 5);
                nt.NonForcing(call, HandRange.OpenerAll, c => DescribeTransfer(c, nt, transferSuit, minKnown),
                    $"Accept transfer to {transferSuit}");
            }
            if (nt.Level == 1 && call.Is(3, transferSuit))
            {
                call.SetHandShape(transferSuit, 4, 5);
				nt.Invitational(call, HandRange.OpenerMaximum, c => DescribeTransfer(c, nt, transferSuit, 4),
					$"Super-Accept transfer to {transferSuit}; 4+{transferSuit} and maximum hand");
                call.Validate = hand => { return !BasicBidding.Is4333(hand); };
			}
        }

        private static void AcceptMinorTransfer(InterpretedBid call, NoTrump nt, Suit transferSuit)
        {
            // TODO: Think about all interference here...  Do we actually accept the transfer?  Or just pass.....
            // If RHO doubled then conditionally accept the transfer.
            // 
            // Especially true for 4-way transfers.
            
            // If there is any  interference then 
            if (call.RhoBid)
            {
                CompetitiveAuction.HandleInterference(call);
            }
            // TODO: in 4-way transfers playing LC standard you only accept IFF maximum hand, otherwise bid at lowest suit...
            else if (call.Is(nt.Level + 2, transferSuit))
            {
                nt.NonForcing(call, HandRange.OpenerAll, c => CompleteMinorTransfer(c, nt, transferSuit));
            }
        }

        private static void CompleteMinorTransfer(InterpretedBid call, NoTrump nt, Suit transferSuit)
        {
            // TODO: Need to do correct stuff for 4-way NOT DONE.
            // We will ignore double
            if (call.RhoBid)
            {
                CompetitiveAuction.HandleInterference(call);
            }
            else if (call.IsPass || call.Is(nt.Level + 2, Suit.Diamonds))
            {
                var suit = call.IsPass ? Suit.Clubs : Suit.Diamonds;
                call.HandShape[suit].Min = 6;
                nt.Signoff(call, HandRange.ResponderAll, $"6+ {suit}");
            }
        }


        public static void DescribeTransfer(InterpretedBid call, NoTrump nt, Suit transferSuit, int minFit)
        {
            if (call.RhoBid)    // Ignore doubles but punt on any RHO bid
            {
                // TODO: Maybe still do some thing here if competition level is low...
                CompetitiveAuction.HandleInterference(call);
            }
            // This is only possible if 1NT bidder has passed when opps have doubled.  If
            // we have a minimal hand then we need to complete the transfer ourselves...
            else if (call.Is(2, transferSuit))
            {
                //  TODO: Should this happen for ANY point range?  
                call.HandShape[transferSuit].Min = 5;
                nt.Signoff(call, HandRange.ResponderNoGame);
            }
            else if (call.Is(2, Suit.Unknown) && minFit == 2)
            {
                call.SetHandShape(transferSuit, 5);
                nt.Invitational(call, HandRange.ResponderGameInvitational, c => RebidAfterInvitation(c, nt, transferSuit),
					$"Invite to game; 5 {transferSuit}");
            }
            else if (call.Is(3, transferSuit))
            {
                call.SetHandShape(transferSuit, minFit > 2 ? 5 : 6, 13);
                nt.Invitational(call, HandRange.ResponderGameInvitational, c => RebidAfterInvitation(c, nt, transferSuit),
					$"Invite to game; 6+ {transferSuit} or known fit");
            }
            else if (call.Is(3, Suit.Unknown) && minFit == 2)
            {
                call.SetHandShape(transferSuit, 5);
                nt.Invitational(call, HandRange.ResponderGame, c => PickGameAfterTransfer(c, nt, transferSuit),
                    $"Game in NT or {transferSuit}; 5 {transferSuit}");
            }
            else if (call.Is(4, transferSuit))
            {
				call.SetHandShape(transferSuit, minFit > 2 ? 5 : 6, 13);
                nt.Signoff(call, HandRange.ResponderGame, $"Game in {transferSuit}; 6+ {transferSuit} or known fit");
				// TODO: Perhaps a new HandRange of AcceptWithMaxOpenerAndFit??
                if (minFit == 4)    // TODO: Kind of side-effect working here...  This means super-accepted
                {
                    call.Points.Min -= 4;
                }
            }
            // TODO: Slam bids here at 4NT...
        }
    

        public static void RebidAfterInvitation(InterpretedBid call, NoTrump nt, Suit transferSuit)
        {
            // TODO: need to deal with some interference...  For now ignore X and punt on anything else.
            if (call.RhoBid)
            {
                CompetitiveAuction.HandleInterference(call);
            }
            else if (call.Is(3, transferSuit))
            {
                call.SetHandShape(transferSuit, 3, 5);
                nt.Signoff(call, HandRange.OpenerMinimum,
                    $"Reject invitation to game, play at 3{transferSuit}; 3+ {transferSuit}");
            }
            else if (call.Is(3, Suit.Unknown))
            {
                call.SetHandShape(transferSuit, 2);
                nt.Signoff(call, HandRange.OpenerAcceptInvitation, $"Accept invitation to play in 3NT; 2 {transferSuit}");
            }
            else if (call.Is(3, BasicBidding.OtherMajor(transferSuit)))
            {
                var otherMajor = BasicBidding.OtherMajor(transferSuit);
                call.SetHandShape(transferSuit, 2);
                call.SetHandShape(otherMajor, 5);
                nt.Forcing(call, HandRange.OpenerAcceptInvitation, c => nt.ResponderGameChoice(c, otherMajor),
                    $"No fit in {transferSuit}.  Show 5 {otherMajor} and accept invitation to game");
            } 
            if (call.Is(4, transferSuit))
            {
				call.SetHandShape(transferSuit, 3, 5);
                nt.Signoff(call, HandRange.OpenerAcceptInvitation, $"Accept invitation to game in {transferSuit}; 3+ {transferSuit}");
            }
        }

   

        public static void PickGameAfterTransfer(InterpretedBid call, NoTrump nt, Suit transferSuit)
        {
            // TODO: need to deal with some interference...  For now ignore X and punt on anything else.
            if (call.RhoBid)
            {
                CompetitiveAuction.HandleInterference(call);
            }
            else if (call.IsPass)
            {
                call.SetHandShape(transferSuit, 2); ;
                nt.Signoff(call, HandRange.OpenerAll, $"2 {transferSuit};  Play in 3NT");
			}
            else if (call.Is(4, transferSuit))
            {        
                call.SetHandShape(transferSuit, 3, 5);
                nt.Signoff(call, HandRange.OpenerAll, $"Accept transfer; 3+ {transferSuit}");
			}
        }
    }
}