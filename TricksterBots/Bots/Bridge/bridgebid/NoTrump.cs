using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Trickster.Bots.InterpretedBid;
using Trickster.Bots;
using Trickster.cloud;
using System.Net.Http.Headers;
using System.Net;
using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;

namespace TricksterBots.Bots {

    public class NoTrump
    {
        // TODO: Where is most appropriate for this?  Some global "
        public bool UseSmolen = false;          // Implemented - NOT TESTED.
        public bool UseTransfers = true;        // Implemented
        public bool UseStayman = true;          // Implemented
        public bool UseGarbageStayman = false;  // TODO: IMPLEMENT AND TEST
        public bool Use4WayTansfers = false;    // TODO: IMPLEMENT AND TEST
        public bool UseLebenshol = false;       // TODO: IMPLEMENT AND TEST - LOTS OF WORK HERE...

        private int openerMin;
        private int openerMax;

        public static void Open(InterpretedBid opening)
        {

            var ntType = NtType.Open1NT;
            if (opening.Is(1, Suit.Unknown))
            {
                ntType = NtType.Open1NT;
            }
            else if (opening.Is(2, Suit.Unknown))
            {
                ntType = NtType.Open2NT;
            }
            else if (opening.Is(3, Suit.Unknown))
            {
                ntType = NtType.Open3NT;
            }
            else
            {
                // TODO: Assert something here?  Throw?  Seems like perhpas shouldnt happen...
                return;
            }
            var nt = new NoTrump(ntType);
            opening.IsBalanced = true;  // TODO: How often to use this
            nt.NonForcing(opening, HandRange.OpenerAll, nt.ConventionalResponses);
        }


        public static void Overcall(InterpretedBid overcall)
        {
            if (overcall.Is(1, Suit.Unknown))
            {
				// TODO: See comment below about suit quality:
				// overcall.SuitQuality[cueSuit / oppsBidSuit] = SuitQuality.StoppedOnce;
				// This may not be a requirement if "cueSuit" is a club...
				var nt = new NoTrump(NtType.Overcall1NT);
                overcall.IsBalanced = true;     // TODO: Should this happen in SetPoints() implementation???
                nt.Invitational(overcall, HandRange.OpenerAll, nt.ConventionalResponses);
            }
            // TODO: More cases here - 2NT after weak open, etc.  But for now, just 1NT overcall...
        }

        public void ConventionalResponses(InterpretedBid response)
        {
            if (response.RhoBid)
            {
                CompetitiveAuction.HandleInterference(response);
            }
            else if (response.Is(3, Suit.Unknown))
            {
                Signoff(response, HandRange.ResponderGame);
            }
            else
            {
                InterpretSlamBids(response);
            }
            // TODO: Should we NOT call conventional stuff when bid made.  Should InitiateXXX assume no interference.
            // then just put natural bids here.....???

            if (response.Level == Level + 1) {
                if (UseStayman && response.Suit == Suit.Clubs)
                {
                    Stayman.InitiateStayman(response, this);
                }
                else if ((UseTransfers && (response.Suit == Suit.Diamonds || response.Suit == Suit.Hearts || response.Suit == Suit.Spades)) ||
                        (Use4WayTansfers && response.Suit == Suit.Unknown))
                {
                    JacobyTransfer.InitiateTransfer(response, this);
                }
                else if (response.Level == 2)
                {
                    if (response.Suit == Suit.Unknown)
                    {
                        Invitational(response, HandRange.ResponderGameInvitational, OpenerRebidAfterGameInvitation);
                    }
                    else
                    {
						response.SetHandShape(response.Suit, 5, 13);
                        Signoff(response, HandRange.ResponderNoGame);
                    }
                }
            }
            // SAYC Standard bidding for 3-level bid by responder.
            // TODO: This seeems like non-natural bids.  Perhpas put this in some different place?  SAYC conventions
            // Natural "Audry Grant" bids at this level are game force and suit-showing 5-cards
            if (Level == 1 && response.Level == 3)
            {
                if (BasicBidding.IsMinor(response.Suit))
                {
					response.SetHandShape(response.Suit, 6, 13);
                    // TODO: OpenerRebidAfterGameInvitation not really right.. Need new function
                    Invitational(response, HandRange.ResponderGameInvitational, OpenerRebidAfterGameInvitation,
                        $"Game invitaional with 6+{response.Suit}");
                }
                else if (BasicBidding.IsMajor(response.Suit))
                {
                    // TODO: 4NT would be blackwood
                    // Opener should probably bid blackwood with slam interest
                    response.SetHandShape(response.Suit, 6, 13);
                    Forcing(response, HandRange.ResponderSlamInvitationalOrBetter, c => OpenerEvaluateSlam(c, response.Suit),
						$"Slam interest with 6+{response.Suit}");
                }
            }
        }


        public void InterpretSlamBids(InterpretedBid call)
        {
			if (call.Is(4, Suit.Unknown))
			{
				Invitational(call, HandRange.ResponderSlamInvitational, c => SlamInvitation(c, 6), "Slam Interest");
			}
			else if (call.Is(5, Suit.Unknown))
			{
				Forcing(call, HandRange.ResponderGrandSlamInvitational, c => SlamInvitation(c, 7), "Pick a slam at 6 or 7NT");
			}
			else if (call.Is(6, Suit.Unknown))
			{
				Signoff(call, HandRange.ResponderSlam);
			}
			else if (call.Is(7, Suit.Unknown))
			{
				Signoff(call, HandRange.ResponderGrandSlam);
			}
		}

        // TODO: SAYC SPECIFIC HERE
        private void OpenerEvaluateSlam(InterpretedBid rebid, Suit trumpSuit)
        {
            // TODO: Interference...
            if (rebid.Is(4, trumpSuit) || rebid.Is(6, trumpSuit))
            {
                bool accepted = (rebid.Level == 6);
                // TODO: Now is where you really want to evaluate with a known suit.  ADD THIS ABILITY TO UPGRADE
                NonForcing(rebid, accepted ? HandRange.OpenerAcceptInvitation : HandRange.OpenerMinimum,
                    c => ResponderPlaceSlam(c, trumpSuit, accepted));
            }
        }

        // TODO: SAYC SPECIFIC LOGIC HERE 
        private void ResponderPlaceSlam(InterpretedBid rebid, Suit trumpSuit, bool accepted)
        {
            // If the caller ended in 4 then if the responder range is > Invitational then place in small slam
            if (rebid.Is(6, trumpSuit))
            {
                // TODO: Assert(accepted) == false
                Signoff(rebid, HandRange.ResponderSlamOrBetter);
            }
            else if (rebid.Is(7, trumpSuit))
            {
                // If opener accepted slam invitation then if wer are in grand slam range bid 7
                Signoff(rebid, accepted ? HandRange.ResponderGrandSlamInvitationalOrBetter : HandRange.ResponderGrandSlam);
            }
        }

        private void SlamInvitation(InterpretedBid rebid, int acceptLevel)
        {
            if (rebid.Is(acceptLevel - 1, Suit.Unknown))    // Happens in pick-a-slam
            {
                Signoff(rebid, HandRange.OpenerMinimum);
            }
            else if (rebid.Is(acceptLevel, Suit.Unknown))
            {
                Signoff(rebid, HandRange.OpenerAcceptInvitation);
            }
        }

        private void OpenerRebidAfterGameInvitation(InterpretedBid rebid)
        {
            // TODO: Handle some more interference here...
            if (rebid.RhoBid)
            {
                CompetitiveAuction.HandleInterference(rebid);
            }
            else
            {
                foreach (Suit major in BasicBidding.MajorSuits)
                {
                    if (rebid.Is(3, major))
                    {
                        rebid.SetHandShape(major, 5);
                        Forcing(rebid, HandRange.OpenerAcceptInvitation, c => ResponderGameChoice(c, major),
                            $"Show 5 {major} and accept invitation to game");
                    }
                }
                if (rebid.Is(3, Suit.Unknown))
                {
                    rebid.SetHandShape(Suit.Hearts, 2, 4);
                    rebid.SetHandShape(Suit.Spades, 2, 4);
                    Signoff(rebid, HandRange.OpenerAcceptInvitation, $"Accept invitation to play in 3NT; No 5 card major");
                }
            }
        }



        // Choose to pay in suit if the responder hand has at least 3 of the suit, and is non 4333 unless the 4-card suit is the bid suit
        private bool PlayInSuit(Hand hand, Suit suit)
        {
            var counts = BasicBidding.CountsBySuit(hand);
            return (counts[suit] > 2 && (!BasicBidding.Is4333(counts) || counts[suit] == 4));
        }

        public void ResponderGameChoice(InterpretedBid rebid, Suit openersMajor)
        {
            // TODO: NEED TO BID IF POSSIBLE... Otherewise handle intereference...
            if (rebid.RhoBid)
            {
                CompetitiveAuction.HandleInterference(rebid);
            }
            else if (rebid.Is(3, Suit.Unknown))
            {
                rebid.SetHandShape(openersMajor, 2);    // TODO: Is this needed with Validate?  Investigate.
                rebid.Validate = hand => { return !PlayInSuit(hand, openersMajor); };
                Signoff(rebid, HandRange.ResponderGameInvitational,
                    $"No fit in {openersMajor} or 4333 shape with 3 in {openersMajor};  Play game at 3NT");
            }
            else if (rebid.Is(4, openersMajor))
            {
                rebid.SetHandShape(openersMajor, 3, 10);
                rebid.Validate = hand => PlayInSuit(hand, openersMajor);
				Signoff(rebid, HandRange.ResponderGameInvitational, $"3+ {openersMajor}; Play game in {openersMajor}");
			}
		}


        public enum HandRange { 
            OpenerAll,
            OpenerMinimum,
            OpenerAcceptInvitation, 
            OpenerMaximum,
			ResponderAll,
			ResponderNoGame,
			ResponderGameInvitational,
			ResponderGameInvitationalOrBetter,
			ResponderGame,
			ResponderGameOrBetter,
			ResponderSlamInvitational,
			ResponderSlamInvitationalOrBetter,
			ResponderSlam,
            ResponderSlamOrBetter,
			ResponderGrandSlamInvitational,
            ResponderGrandSlamInvitationalOrBetter,
			ResponderGrandSlam
		};



        public void SetPoints(HandRange handRange, InterpretedBid call)
        {
            switch (handRange)
            {
                case HandRange.OpenerAll:
                    call.SetPoints(openerMin, openerMax);
                    break;
                case HandRange.OpenerMinimum:
                    call.SetPoints(openerMin, openerMin);
                    break;
                case HandRange.OpenerAcceptInvitation:
                    call.SetPoints(openerMin + 1, openerMax);
                    break;
                case HandRange.OpenerMaximum:
                    call.SetPoints(openerMax, openerMax);
                    break;

                case HandRange.ResponderAll:
                    SetResponderPoints(call, 0);
                    break;
                case HandRange.ResponderNoGame:
					SetResponderPoints(call, 0, 22);
					break;
				case HandRange.ResponderGameInvitational:
					SetResponderPoints(call, 23, 24);
					break;
				case HandRange.ResponderGameInvitationalOrBetter:
					SetResponderPoints(call, 23);
					break;
				case HandRange.ResponderGame:
					SetResponderPoints(call, 25, 30);
					break;
				case HandRange.ResponderGameOrBetter:
					SetResponderPoints(call, 25);
					break;
				case HandRange.ResponderSlamInvitational:
					SetResponderPoints(call, 31, 32);
					break;
				case HandRange.ResponderSlamInvitationalOrBetter:
					SetResponderPoints(call, 31);
					break;
				case HandRange.ResponderSlam:
					SetResponderPoints(call, 33, 35);
					break;
                case HandRange.ResponderSlamOrBetter:
                    SetResponderPoints(call, 33);
                    break;
				case HandRange.ResponderGrandSlamInvitational:
					SetResponderPoints(call, 36, 36);
					break;
                case HandRange.ResponderGrandSlamInvitationalOrBetter:
                    SetResponderPoints(call, 36);
                    break;
				case HandRange.ResponderGrandSlam:
					SetResponderPoints(call, 37);
					break;
			}
		}

		private void SetResponderPoints(InterpretedBid call, int min, int max = 40)
		{
			call.SetPoints(System.Math.Max(0, min - openerMin), System.Math.Max(0, max - openerMin));
		}



		public void Signoff(InterpretedBid call, HandRange handRange, string description = null)
        {
            DescribeBid(call, handRange, BidMessage.Signoff, CompetitiveAuction.PassOrCompete, description);
        }

        public void Invitational(InterpretedBid call, HandRange handRange, CallInterpreter partnersCall, string description = null)
        {
            DescribeBid(call, handRange, BidMessage.Invitational, partnersCall, description);
        }

		public void Forcing(InterpretedBid call, HandRange handRange, CallInterpreter partnersCall, string description = null)
		{
			DescribeBid(call, handRange, BidMessage.Forcing, partnersCall, description);
		}

        // TODO:  Need a new BidMessage.NonForcing to describe this as different from Invitational.  For now use Invitational
		public void NonForcing(InterpretedBid call, HandRange handRange, CallInterpreter partnersCall, string description = null)
		{
			DescribeBid(call, handRange, BidMessage.Invitational, partnersCall, description);
		}

		private void DescribeBid(InterpretedBid call, HandRange handRange, BidMessage bidMessage, CallInterpreter partnersCall, string description)
        {
            SetPoints(handRange, call);
            call.BidMessage = bidMessage;
            call.PartnersCall = partnersCall;
            call.Description = description == null ? string.Empty : description;
        }


		public NtType ntType;
        public enum NtType
        {
            Open1NT,
            Open2NT,
            Open3NT,
            Open2C,
            Overcall1NT,
            Overcall2NTOverWeak,
            Overcall2NT,
            Balancing1NT
        }
        public NoTrump(NtType ntType)
        {
            this.ntType = ntType;
            this.openerMin = 15;
            this.openerMax = 17;
            switch (ntType)
            {
                case NtType.Balancing1NT:
                    this.openerMin = 12;
                    this.openerMax = 14;
                    break;

                case NtType.Overcall1NT:
                    this.UseTransfers = false;      // TODO: This is the case for SAYC.  
                    this.openerMin = 15;
                    this.openerMax = 18;
                    break; 

                case NtType.Overcall2NTOverWeak:
                    this.openerMin = 15;
                    this.openerMax = 18;
                    break;

                case NtType.Open2NT:
                case NtType.Overcall2NT:    // TODO: Is this right?
                    this.openerMin = 20;
                    this.openerMax = 21;
                    break;

                case NtType.Open2C:
                    this.openerMin = 22;
                    this.openerMax = 37;
                    break;

                case NtType.Open3NT:
                    this.openerMin = 25;
                    this.openerMax = 28;     // TODO: What is the max?  This is stupid...
                    break;
            }
        }

        public Range OpenerAcceptInvitePoints
        {
            get
            {
                return new Range(this.OpenerPoints.Min + 1, this.OpenerPoints.Max);
            }
        }

        public Range OpenerRejectInvitePoints
        {
            get
            {
                return new Range(this.OpenerPoints.Min, this.OpenerPoints.Min); // Only reject at the lowest level.
            }
        }

 
        public Range ResponderInvitationalPoints
        {
            get
            {
                int min = 23 - openerMin;
                if (min > 0) { return new Range(min, min + 1); }
                return new Range(0, 0);
            }
        }


        public Range ResponderGamePoints    
        {
            get
            {
                int min = System.Math.Max(0, 25 - OpenerPoints.Min);
                int max = 32 - OpenerPoints.Min; // TODO: Is this right?
                return new Range(min, max);
            }
        }
        public Range ResponderGameOrBetterPoints
        {
            get
            {
                return new Range(System.Math.Max(0, 25 - OpenerPoints.Min), 40);;
            }
        }
        public Range ResponderInvitationalOrBetterPoints
        {
            get
            {
                return new Range(ResponderInvitationalPoints.Min, 40);  
            }
        }





		public int Level
        {
            get
            {
                switch (ntType)
                {
                    case NtType.Open1NT:
                    case NtType.Overcall1NT:
                    case NtType.Balancing1NT:
                        return 1;
                    case NtType.Open2NT:
                    case NtType.Overcall2NT:
                    case NtType.Open2C:
                        return 2;
                    case NtType.Open3NT:
                        return 3;
                    default:
                        return 0;   // TODO: THROW!
                }
            }
        }
        public Range OpenerPoints {
            get
            {
                return new Range(openerMin, openerMax);
            }
        }
    }
}