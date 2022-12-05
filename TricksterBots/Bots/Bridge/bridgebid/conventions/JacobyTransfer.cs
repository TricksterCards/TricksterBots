using System.Net.Http.Headers;
using System.Security.Policy;
using Trickster.cloud;
using static Trickster.Bots.InterpretedBid;

namespace Trickster.Bots
{
    internal class JacobyTransfer
    {
        public static bool CanUseTransfers(InterpretedBid bid)
        {
            return Stayman.CanUseStayman(bid);
        }

        public static bool Interpret(InterpretedBid bid)
        {
            if (CanUseTransfers(bid)) return InterpretTransfers(bid);

            if (bid.bidIsDeclare && bid.Index >= 4 && bid.History[bid.Index - 2].BidConvention == BidConvention.JacobyTransfer)
            {
                AcceptTransfer(bid.History[bid.Index - 2], bid.History[bid.Index - 1], bid);
                return true;
            }

            if (bid.bidIsDeclare && bid.Index >= 6 && bid.History[bid.Index - 4].BidConvention == BidConvention.JacobyTransfer)
            {
                InterpretResponderRebid(bid.History[bid.Index - 4], bid.History[bid.Index - 2], bid);
                return true;
            }

            // Now check for opener's re-rebid to place contract TODO: Need to go to slam in some cases?
            if (bid.bidIsDeclare && bid.Index >= 8 && bid.History[bid.Index - 4].BidConvention == BidConvention.AcceptJacobyTransfer)
            {
                PlaceContract(bid.History[bid.Index - 4], bid.History[bid.Index -2], bid);
                return true;
            }

            return false;
        }

        public static bool InterpretTransfers(InterpretedBid response)
        {
            if (!response.bidIsDeclare)
                return false;

            if (response.declareBid.suit != Suit.Diamonds && response.declareBid.suit != Suit.Hearts)
                return false;

            if (response.declareBid.level != response.LowestAvailableLevel(response.declareBid.suit))
                return false;

            //  1N-2D
            //  1N-2H
            //  2N-3D
            //  2N-3H
            //  3N-4D
            //  3N-4H
            //  2C-2D-2N-3D
            //  2C-2D-2N-3H
            //  2C-2D-3N-4D
            //  2C-2D-3N-4H
            response.BidConvention = BidConvention.JacobyTransfer;
            response.BidMessage = BidMessage.Forcing;
            var transferSuit = response.declareBid.suit == Suit.Diamonds ? Suit.Hearts : Suit.Spades;
            response.HandShape[transferSuit].Min = 5;
            response.Description = $" to {response.declareBid.level}{Card.SuitSymbol(transferSuit)}; 5+ {transferSuit}";

            return true;
        }

        private static void AcceptTransfer(InterpretedBid transfer, InterpretedBid interference, InterpretedBid accept)
        {
            if (!accept.bidIsDeclare || accept.declareBid.level > transfer.declareBid.level + 1)
                return;

            if (transfer.declareBid.suit == Suit.Diamonds && accept.declareBid.suit != Suit.Hearts)
                return;

            if (transfer.declareBid.suit == Suit.Hearts && accept.declareBid.suit != Suit.Spades)
                return;

            //  1N-2D-2H
            //  1N-2H-2S
            //  ...
            accept.BidConvention = BidConvention.AcceptJacobyTransfer;
            accept.Description = string.Empty;

            if (interference.bid == BridgeBid.Double)
                //  if transfer is doubled, opener only completes the transfer with 3+ trumps
                accept.HandShape[accept.declareBid.suit].Min = 3;
            else
                //  otherwise we always accept the transfer if opponent hasn't bid (in which case this won't be an option anyway)
                accept.AlternateMatches = hand => true;

            if (accept.declareBid.level == transfer.declareBid.level + 1)
            {
                //  1N-2D-3H
                //  1N-2D-3S
                //  ...
                accept.Points.Min = 17;
                accept.BidPointType = BidPointType.Dummy;
                accept.HandShape[accept.declareBid.suit].Min = 4;
                accept.Description = $"super-accept; 4+ {accept.declareBid.suit}";
                accept.AlternateMatches = null;
            }
        }

        private static void InterpretResponderRebid(InterpretedBid transfer, InterpretedBid accept, InterpretedBid rebid)
        {
            if (accept.declareBid == null || transfer.declareBid == null)
                return; // TODO: Handle X, etc...

            // Validate that transfer happened
            var transferSuit = transfer.declareBid.suit == Suit.Diamonds ? Suit.Hearts : Suit.Spades;
            if (accept.declareBid.suit != transferSuit)
                return;

            if (rebid.bid == BidBase.Pass)
            {
                rebid.Points.Max = 7;
                rebid.BidPointType = BidPointType.Hcp;
                return;
            }

            if (rebid.declareBid == null)
                return;     // TODO: Handle these cases X, etc.

            if (transferSuit == rebid.declareBid.suit)
            {
                if (rebid.declareBid.level == 3)
                {
                    rebid.Points.Min = 8;
                    rebid.Points.Max = 9;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.HandShape[transferSuit].Min = 6;
                    rebid.Description = $"Inviting game; 6+ {transferSuit}";
                    return;
                }
                // TODO: Slam stuff not implemented
                if (rebid.declareBid.level == 4)
                {
                    // If opener has super-accepted the transfer then game can be bid with weaker hand
                    // bec
                    if (accept.declareBid.level == 3)
                    {
                        rebid.Points.Min = 6;
                        rebid.BidPointType = BidPointType.Hcp;
                        rebid.HandShape[transferSuit].Min = 5;
                        rebid.Description = $"Sign-off at game; 5+ {transferSuit}";
                        // TODO: Weaker game with more trump cards - alternate matches
                    }
                    else
                    {
                        rebid.Points.Min = 10;
                        rebid.BidPointType = BidPointType.Hcp;
                        rebid.HandShape[transferSuit].Min = 6;
                        rebid.Description = $"Sign-off at game; 6+ {transferSuit}";
                    }
                }
                return;
            }

            if (rebid.declareBid.suit == Suit.Unknown)
            {
                if (rebid.declareBid.level == 2)
                {
                    rebid.Points.Min = 8;
                    rebid.Points.Max = 9;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.HandShape[transferSuit].Max = 5;
                    rebid.HandShape[transferSuit].Min = 5;
                    rebid.Description = $"Invite in game; 5 {transferSuit}";
                    return;
                }
                if (rebid.declareBid.level == 3)
                {
                    rebid.Points.Min = 10;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.HandShape[transferSuit].Max = 5;
                    rebid.HandShape[transferSuit].Min = 5;
                    rebid.Description = $"Sign-off at game; 5 {transferSuit}";
                    return;
                }
            }


        }

        private static void PlaceContract(InterpretedBid accept, InterpretedBid responderRebid, InterpretedBid rebid)
        {
            if (accept.declareBid == null || responderRebid.declareBid == null)
                return; // TODO: Handle X, etc...

            if (rebid.declareBid == null)
                return;     // TODO: Handle these cases X, etc.

            // Validate that transfer happened -- TODO: Check earlier bids were successful transfers?
            var transferSuit = accept.declareBid.suit;

            if (rebid.bid == BidBase.Pass)
            {
                rebid.Points.Max = 15;
                rebid.HandShape[transferSuit].Min = 2;
                rebid.HandShape[transferSuit].Max = 2;
                rebid.BidPointType = BidPointType.Hcp;
                return;
            }
            
            if (rebid.declareBid.suit == transferSuit)
            {
                if (rebid.declareBid.level == 3)
                {
                    rebid.Points.Min = 15;
                    rebid.Points.Max = 15;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.HandShape[transferSuit].Min = 3;
                    rebid.Description = $"Sign-off at partscore; 3+ {transferSuit}";
                    return;
                }
                if (rebid.declareBid.level == 4)
                {
                    rebid.Points.Min = (responderRebid.declareBid.level == 3 && responderRebid.declareBid.suit == Suit.Unknown) ? 15 : 16;
                    rebid.Points.Max = 17;
                    rebid.BidPointType = BidPointType.Hcp;
                    rebid.HandShape[transferSuit].Min = 3;
                    rebid.Description = $"Sign-off at game; 3+ {transferSuit}";
                    return;
                }
            }

            if (rebid.declareBid.suit == Suit.Unknown && rebid.declareBid.level == 3)
            {
                rebid.Points.Min = 16;
                rebid.Points.Max = 17;
                rebid.BidPointType = BidPointType.Hcp;
                rebid.HandShape[transferSuit].Max = 2;
                rebid.HandShape[transferSuit].Min = 2;
                rebid.Description = $"Sign-off in game; 2 {transferSuit}";
                return;
            }

        }
    }
    public abstract class BidInterpreter
    {
        public abstract void Interpret(BridgeBidHistory history, InterpretedBid bid);
        public virtual void InterpretRhoX(BridgeBidHistory history, InterpretedBid bid)
        {
            this.Interpret(history, bid);
        }
        public virtual void InterpertRhoXX(BridgeBidHistory history, InterpretedBid bid)
        {
            this.Interpret(history, bid);
        }
        public virtual void InterpretRhoBid(BridgeBidHistory history, InterpretedBid rhoBid, InterpretedBid bid)
        {
            // TODO: CompetitiveBidInterpreter().Interpret(history, bid)
        }
    }

    public abstract class NtConventionInterpreter: BidInterpreter
    {

        public readonly NtType ntType;
        
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
        public NtConventionInterpreter(NtType ntType)
        {
            this.ntType = ntType;
            this.NtRange = new Range(15, 17);  // Set up for 1NT
            switch (ntType)
            {
                case NtType.Balancing1NT:
                    this.NtRange.Min = 12;
                    this.NtRange.Max = 14;
                    break;

                case NtType.Overcall1NT:
                case NtType.Overcall2NTOverWeak:
                    this.NtRange.Min = 15;
                    this.NtRange.Max = 18;
                    break;

                case NtType.Open2NT:
                case NtType.Overcall2NT:    // TODO: Is this right?
                    this.NtRange.Min = 20;
                    this.NtRange.Max = 21;
                    break;

                case NtType.Open2C:
                    this.NtRange.Min = 22;
                    this.NtRange.Max = 37;
                    break;

                case NtType.Open3NT:
                    this.NtRange.Min = 25;
                    this.NtRange.Max = 28;     // TODO: What is the max?  This is stupid...
                    break;
            }

        }

        public Range AcceptInviteRange
        {
            get
            {
                return new Range(this.NtRange.Min + 1, this.NtRange.Max);
            }
        }
        public Range InvitationalRange
        {
            get
            {
                int min = 23 - NtRange.Min;
                if (min > 0) { return new Range(min, min+1) }
                return new Range(0, 0);
            }
        }
        public Range GameRange
        {
            get
            {
                int min = System.Math.Max(0, 25 - NtRange.Min);
                int max = 32 - NtRange.Min; // TODO: Is this right?
                return new Range(min, max);
            }
        }
        // TODO: Slam ranges...
        public int ConventionBidLevel
        {
            get
            {
                switch (ntType)
                {
                    case NtType.Open1NT:
                    case NtType.Overcall1NT:
                    case NtType.Balancing1NT:
                        return 2;
                    case NtType.Open2NT:
                    case NtType.Overcall2NT:
                    case NtType.Open2C:
                        return 3;
                    case NtType.Open3NT:
                        return 4;
                    default:
                        return 0;   // TODO: THROW!
                }
            }
        }
        public Range NtRange { get; }
    }
    public class TransferInterpreter: NtConventionInterpreter
    {
        public TransferInterpreter(NtType ntType) : base(ntType)
        {
        }

        public override void Interpret(BridgeBidHistory history, InterpretedBid bid)
        {
            if (bid.declareBid == null)
                return;

            if (bid.declareBid.level == ConventionBidLevel)
            { 
                if (bid.declareBid.suit == Suit.Diamonds || bid.declareBid.suit == Suit.Hearts))
                {
                    var transferSuit = bid.declareBid.suit == Suit.Diamonds ? Suit.Hearts : Suit.Spades;
                    bid.HandShape[transferSuit].Min = 5;
                    bid.Description = $"Transfer to {Card.SuitSymbol(transferSuit)}; 5+ {transferSuit}";
                    // bid.NextState = AcceptMajorTransferInterpreter(ntType, transferSuit);
                }
                if (bid.declareBid.suit == Suit.Spades)
                {
                    // TODO: Minor transfers here...
                }
            }
        }
    }

    public class AcceptMajorTransferInterpreter : NtConventionInterpreter
    {
        private Suit transferSuit;
        public AcceptMajorTransferInterpreter(NtType ntType, Suit transferSuit) : base(ntType)
        {
            this.transferSuit = transferSuit;
        }
        public override void InterpretRhoX(BridgeBidHistory history, InterpretedBid bid)
        {
            if (bid.declareBid == null)
            {   // TODO: I assume null == PASS!  Is that right?
                bid.HandShape[transferSuit].Min = 2;
                bid.HandShape[transferSuit].Max = 2;
                bid.Description = $"pass transfer to {transferSuit} indicating no fit after opponent X";
                // bid.NextState = DescribeTransferInterpreter(ntType, transferSuit, false);
            }
            else if (bid.declareBid.level == ConventionBidLevel && bid.declareBid.suit == transferSuit)
            {
                bid.HandShape[transferSuit].Min = 3;
                bid.Description = $"Accept transfer to {transferSuit} after opponent X; 3+ {transferSuit}";
                // bid.NextState = DescribeTransferInterpreter(ntType, transferSuit, true);
            }
        }

        public override void Interpret(BridgeBidHistory history, InterpretedBid bid)
        {
            if (bid.declareBid != null && bid.declareBid.level == ConventionBidLevel && bid.declareBid.suit == transferSuit)
            {
                bid.Points = this.NtRange;      // Is this right?  Just do this to force the bid to happen?
                bid.Description = $"Accept transfer to {transferSuit}";
                // bid.NextState = DescribeTransferInterpreter(ntType, transferSuit, false);
            }
        }
    }

    public class DescribeTransferInterpreter : NtConventionInterpreter
    {
        private Suit transferSuit;
        private bool knownFit;

        public DescribeTransferInterpreter(NtType ntType, Suit transferSuit, bool knownFit) : base(ntType)
        {
            this.transferSuit = transferSuit;
            this.knownFit = knownFit;
        }

        public override void Interpret(BridgeBidHistory history, InterpretedBid bid)
        {
            if (bid.declareBid == null)
                return;

            // This is only possible if 1NT bidder has passed when opps have doubled.  If
            // we have a minimal hand then we need to complete the transfer ourselves...
            if (bid.declareBid.level == 2 && bid.declareBid.suit == transferSuit)
            {
                bid.Points.Min = 0;
                bid.Points.Max = InvitationalRange.Min - 1;
                bid.HandShape[transferSuit].Min = 5;
                // bid.nextState = null; (or something that passes all the time)...
            }

            // TODO: Maybe if suit is stopped and 5-cards we would want to bid this even if knownFit...
            if (bid.declareBid.level == 2 && bid.declareBid.suit == Suit.Unknown && !knownFit)
            {
                bid.Points = InvitationalRange;
                bid.HandShape[transferSuit].Min = 5;
                bid.HandShape[transferSuit].Max = 5;
                bid.Description = $"Invite to game; 5 {transferSuit}";
                // bid.NextState = FinishTransferInviteInterpreter(ntType, transferSuit);
            }
            else if (bid.declareBid.level == 3 && bid.declareBid.suit == transferSuit)
            {
                bid.Points = InvitationalRange;
                bid.HandShape[transferSuit].Min = knownFit ? 5 : 6;
                // NEED TO SET MAX?  TODO
               // bid.NextState = FinishTransferInviteInterpreter(ntType, transferSuit);
            }
            else if (bid.declareBid.level == 3 && bid.declareBid.suit == Suit.Unknown && !knownFit)
            {
                bid.Points = GameRange;
                bid.HandShape[transferSuit].Min = 5;
                bid.HandShape[transferSuit].Max = 5;
                bid.Description = $"Game in NT or {transferSuit}; 5 {transferSuit}";
                // bid.NextState = PickGameTransferInterpreter(ntType, transferSuit);
            }
            else if (bid.declareBid.level == 4 && bid.declareBid.suit == transferSuit)
            {
                bid.Points = GameRange;
                bid.HandShape[transferSuit].Min = knownFit ? 5 : 6;
                // TODO: Need Max too?
                bid.Description = $"Game in {transferSuit}; 6+ {transferSuit} or known fit";
                // bid.NextState = null
            }
            // TODO: Slam bids here at 4NT...
        }
    }

    public class FinishTransferInviteInterpreter : NtConventionInterpreter
    {
        private Suit transferSuit;

        public FinishTransferInviteInterpreter(NtType ntType, Suit transferSuit) : base(ntType)
        {
            this.transferSuit = transferSuit;
        }



        public override void Interpret(BridgeBidHistory history, InterpretedBid bid)
        {
            if (bid.Is(3, Suit.Unknown))
            {
                bid.Points = AcceptInviteRange;
                bid.HandShape[transferSuit].Min = 2;
                bid.HandShape[transferSuit].Max = 2;
                bid.Description = $"Accept invitation to play in 3NT; 2 {transferSuit}";
                // bid.nextstate = null 
            }
            var otherMajor = transferSuit == Suit.Hearts ? Suit.Spades : Suit.Hearts;
            if (bid.Is(4, otherMajor))
            {
                bid.Points = AcceptInviteRange;
                bid.HandShape[transferSuit].Min = 2;
                bid.HandShape[transferSuit].Max = 2;
                bid.HandShape[otherMajor].Min = 5;
                bid.HandShape[otherMajor].Max = 5;
                bid.Description = $"No fit in {transferSuit}.  Show 5 {otherMajor} and accept invitation to game";
                // bid.nextState = TrySecondMajorTransferInterpreter(ntType, otherMajor);
            }
            if (bid.declareBid != null && bid.declareBid.level == 4 && bid.declareBid.suit == transferSuit)
            {
                bid.Points = AcceptInviteRange;
                bid.HandShape[transferSuit].Min = 3;
                bid.HandShape[transferSuit].Max = 5;
                bid.Description = $"Accept invitation to game in {transferSuit}; 3+ {transferSuit}";
                // bid.NextState = null
            }

        }

    }



    public class PickGameTransferInterpreter : NtConventionInterpreter
    {
        private Suit transferSuit;

        public PickGameTransferInterpreter(NtType ntType, Suit transferSuit) : base(ntType)
        {
            this.transferSuit = transferSuit;
        }

        public override void Interpret(BridgeBidHistory history, InterpretedBid bid)
        {
            if (bid.Is(4, transferSuit))
            {        
                bid.Points = NtRange;
                bid.HandShape[transferSuit].Min = 3;
                bid.HandShape[transferSuit].Max = 5;
                bid.Description = $"Accept transfer; 3+ {transferSuit}";
                // bid.NextState = null
            }
        }
    }

    public class TrySecondMajorTransferInterpreter : NtConventionInterpreter
    {
        private Suit otherMajor;

        public TrySecondMajorTransferInterpreter(NtType ntType, Suit otherMajor) : base(ntType)
        {
            this.otherMajor = otherMajor;
        }

        public override void Interpret(BridgeBidHistory history, InterpretedBid bid)
        {
            if (bid.Is(3, Suit.Unknown))
            {
                bid.Points = InvitationalRange;
                bid.HandShape[otherMajor].Max = 2;
                bid.HandShape[otherMajor].Min = 2;
                bid.Description = $"No fit in {otherMajor};  Play game at 3NT";
                // bid.nextState = null;
            }
            if (bid.Is(4, otherMajor))
            {
                bid.Points = InvitationalRange;
                bid.HandShape[otherMajor].Min = 3;
                bid.HandShape[otherMajor].Max = 10;  // TODO: Need Max?
                bid.Description = $"Play game in {otherMajor}";
                // bid.NextState = null
            }
        }
    }


}