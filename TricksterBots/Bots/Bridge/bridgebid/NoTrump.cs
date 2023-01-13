using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Trickster.Bots.InterpretedBid;
using Trickster.Bots;
using Trickster.cloud;
using System.Net.Http.Headers;


namespace TricksterBots.Bots {

    public class NoTrump
    {
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
            opening.SetPoints(nt.OpenerPoints);
            opening.IsBalanced = true;
            opening.Description = string.Empty;
            opening.PartnersCall = nt.ConventionalResponses;
        }
        

        public static void Overcall(InterpretedBid overcall)
        {
            if (overcall.Is(1, Suit.Unknown))
            {
                var nt = new NoTrump(NtType.Overcall1NT);
				// TODO: Opponent's suit must be stopped...
				overcall.SetPoints(nt.OpenerPoints);
				overcall.IsBalanced = true;
				// TODO: See comment below about suit quality:
				// overcall.SuitQuality[cueSuit / oppsBidSuit] = SuitQuality.StoppedOnce;
				// This may not be a requirement if "cueSuit" is a club...
				overcall.Description = string.Empty;
                overcall.PartnersCall = nt.ConventionalResponses;
			}
            // TODO: More cases here - 2NT after weak open, etc.  But for now, just 1NT overcall...
        }

        public void ConventionalResponses(InterpretedBid response)
        {

            // Game is always a signoff.  3NT never has any other meaning than signoff in game
            if (response.Is(3, Suit.Unknown))
            { 
                response.BidMessage = BidMessage.Signoff;
                response.SetPoints(ResponderGamePoints);
                response.IsBalanced = true;
                response.Description = string.Empty;
                return;
            }


            if (response.Level == BidLevel + 1) {
                if (response.declareBid.suit == Suit.Clubs)
                {
                    Stayman.InitiateStayman(response, this);
                }
                else
                {
                    JacobyTransfer.InitiateTransfer(response, this, false);
                }
            }
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
            this.OpenerPoints = new Range(15, 17);  // Set up for 1NT
            switch (ntType)
            {
                case NtType.Balancing1NT:
                    this.OpenerPoints.Min = 12;
                    this.OpenerPoints.Max = 14;
                    break;

                case NtType.Overcall1NT:
                case NtType.Overcall2NTOverWeak:
                    this.OpenerPoints.Min = 15;
                    this.OpenerPoints.Max = 18;
                    break;

                case NtType.Open2NT:
                case NtType.Overcall2NT:    // TODO: Is this right?
                    this.OpenerPoints.Min = 20;
                    this.OpenerPoints.Max = 21;
                    break;

                case NtType.Open2C:
                    this.OpenerPoints.Min = 22;
                    this.OpenerPoints.Max = 37;
                    break;

                case NtType.Open3NT:
                    this.OpenerPoints.Min = 25;
                    this.OpenerPoints.Max = 28;     // TODO: What is the max?  This is stupid...
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

        public Range ResponderNoGamePoints
        {
            get
            {
                return new Range(0, System.Math.Max(0, ResponderInvitationalPoints.Min - 1));
            }
        }
        public Range ResponderInvitationalPoints
        {
            get
            {
                int min = 23 - OpenerPoints.Min;
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
        // TODO: Slam ranges...
        public int BidLevel
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
        public Range OpenerPoints { get; }
    }
}