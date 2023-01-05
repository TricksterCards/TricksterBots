using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Trickster.Bots.InterpretedBid;
using Trickster.Bots;
using Trickster.cloud;

namespace TricksterBots.Bots {

    public class NTFundamentals
    {
        public static void Open(InterpretedBid opening, NtType ntType)
        {
            var ntInfo = new NTFundamentals(ntType);
            opening.SetHighCardPoints(ntInfo.OpenerPoints);
            opening.IsBalanced = true;
            opening.Description = string.Empty;
            opening.PartnersCall = c => ConventionalResponses(c, ntInfo);
        }

        public static void ConventionalResponses(InterpretedBid response, NTFundamentals ntInfo)
        {
            // Game is always a signoff.  3NT never has any other meaning than signoff in game
            if (response.Is(3, Suit.Unknown))
            { 
                response.BidMessage = BidMessage.Signoff;
                response.SetHighCardPoints(ntInfo.ResponderGamePoints);
                response.IsBalanced = true;
                response.Description = string.Empty;
                return;
            }

            // TODO: Do stayman needs to be full state-machine with PartnersCall 
            if (!Stayman.Interpret(response))
            {
                if (response.declareBid != null && response.declareBid.level == ntInfo.BidLevel + 1)
                {
                    // We send ALL bids at the next level through, including 2NT.  
                    // TODO: Make sure this only happens for 2NT.  Other levels don't make sense.  Minor transfers over 2NT and 3NT?  Seem odd..  Probably wrong...
                    JacobyTransfer.InitiateTransfer(response, ntInfo, false);   // TODO: In the furture we want 4-way transfers.
                    return;
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
        public NTFundamentals(NtType ntType)
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