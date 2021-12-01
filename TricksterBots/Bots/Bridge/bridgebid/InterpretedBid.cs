using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Trickster.cloud;

namespace Trickster.Bots
{
    public delegate bool HandValidator(Hand hand);

    internal class InterpretedBid : BidExplanation
    {
        public const int GrandSlamPoints = 37;
        public const int InvitationalPoints = 23;
        public const int SmallSlamPoints = 33;

        private const string UnknownDescripton = "Unknown bid";

        private static readonly Regex rxSpaceBefore = new Regex(@"([a-z])([0-9A-Z])", RegexOptions.Compiled);

        public readonly List<int> Aces = new List<int>();

        public readonly int bid;

        public readonly bool bidIsDeclare;

        public readonly DeclareBid declareBid;

        public readonly List<InterpretedBid> History;

        public readonly int Index;

        public readonly List<int> Kings = new List<int>();

        public string AlternatePoints = string.Empty;

        public InterpretedBid()
        {
        }

        public InterpretedBid(int bid, List<InterpretedBid> history, int index)
        {
            Index = index;
            History = history;

            this.bid = bid;
            bidIsDeclare = DeclareBid.Is(bid);
            declareBid = bidIsDeclare ? new DeclareBid(bid) : null;
            BidPhase = PhaseOfBid();
            BidMessage = BidMessage.Invitational;
            BidConvention = BidConvention.None;
            IsGood = false;
            IsBalanced = false;
            IsPreemptive = false;
            Description = UnknownDescripton;

            Points = new Range(0, 37); // AKQJ, AKQ, AKQ, AKQ
            BidPointType = BidPointType.Distribution;
            HandShape = new Dictionary<Suit, Range>();
            foreach (var s in SuitRank.stdSuits)
                HandShape.Add(s, new Range(0, 13));

            //  assume (re)doubles are for penalty (and override later if not)
            if (bid == BridgeBid.Double)
                Description = "Penalty double";
            else if (bid == BridgeBid.Redouble)
                Description = "Penalty redouble";

            //  first answer question-asking conventions (e.g. Blackwood)
            if (!InterpretConventions())
                //  otherwise interpret the bid based on phase
                InterpretPhase();

            if (IsBalanced)
                //  set hand shape limits if we're balanced
                SetBalancedHandShape();

            //  re-adjust maximum lengths in a suit based on any minimums that were set
            SetHandShapeMaxes();

            Description = FinalDescriptionString;
        }

        public HandValidator AlternateMatches { get; set; }

        public int GameLevel => declareBid.suit == Suit.Unknown ? 3 : BridgeBot.IsMajor(declareBid.suit) ? 4 : 5;

        public int GamePoints => declareBid.suit == Suit.Unknown ? 25 : BridgeBot.IsMajor(declareBid.suit) ? 26 : 29;

        public Dictionary<Suit, Range> HandShape { get; }

        public DeclareBid LastDeclareBid
        {
            get
            {
                for (var i = Index - 1; i >= 0; --i)
                    if (History[i].bidIsDeclare)
                        return History[i].declareBid;

                return new DeclareBid(0, Suit.Unknown);
            }
        }

        public Range Points { get; }

        public List<Suit> SuitsBid
        {
            get { return History.Take(Index).Where(b => b.bidIsDeclare).Select(b => b.declareBid.suit).Distinct().ToList(); }
        }

        public HandValidator Validate { get; set; }

        private string FinalDescriptionString
        {
            get
            {
                string conventionString;
                if (BidConvention == BidConvention.None)
                {
                    conventionString = string.Empty;
                }
                else
                {
                    conventionString = PrettyBidConvention;
                    if (!string.IsNullOrEmpty(Description) && Description[0] != ' ')
                        conventionString += "; ";
                }

                //  TODO: also add N+ suit (e.g. 3+ Hearts) based on HandShape minimums
                var distribution = IsBalanced ? "Balanced; " : string.Empty;

                var goodString = IsGood ? "Good " : string.Empty;

                var preemptString = IsPreemptive ? "; preempt" : string.Empty;

                var bidMessageString = BidMessage == BidMessage.Invitational ? string.Empty : $" [{PrettyBidMessage}]";

                var maxPointsString = Points.Max >= 37 ? "+" : $"-{Points.Max}";
                var pointTypeString = BidPointType == BidPointType.Hcp ? "HCP" : BidPointType == BidPointType.Dummy ? "dummy points" : "points";
                var alternateString = string.IsNullOrEmpty(AlternatePoints) ? string.Empty : " or " + AlternatePoints;
                var pointsString = Points.Min <= 0 && Points.Max >= 37 ? string.Empty : $" ({Points.Min}{maxPointsString} {pointTypeString}{alternateString})";

                return conventionString + distribution + goodString + Description + preemptString + pointsString + bidMessageString;
            }
        }

        private string PrettyBidConvention
        {
            get { return rxSpaceBefore.Replace(BidConvention.ToString(), m => m.Groups[1].Value + " " + m.Groups[2].Value.ToLowerInvariant()); }
        }

        private string PrettyBidMessage
        {
            get { return rxSpaceBefore.Replace(BidMessage.ToString(), m => m.Groups[1].Value + " " + m.Groups[2].Value.ToLowerInvariant()); }
        }

        public static List<InterpretedBid> InterpretHistory(BridgeBidHistory bidHistory)
        {
            var history = new List<InterpretedBid>();

            for (var i = 0; i < bidHistory.Count; i++) history.Add(new InterpretedBid(bidHistory[i], history, i));

            return history;
        }

        public int LowestAvailableLevel(Suit suit, bool includeInterference = false)
        {
            //  note: by default this deliberately ignores any interference from the other team
            var step = includeInterference ? 1 : 2;
            var start = Index - step;
            for (var i = start; i >= 0; i -= step)
            {
                if (History[i].bidIsDeclare)
                {
                    var db = History[i].declareBid;
                    return BridgeBot.suitRank[db.suit] < BridgeBot.suitRank[suit] ? db.level : db.level + 1;
                }

                if (History[i].bid == BridgeBid.Double)
                    //  even when ignoring interference, we should look at opponents' bids if we hit a double
                    step = 1;
            }

            return 1;
        }

        public bool Match(Hand hand)
        {
            if (AlternateMatches != null && AlternateMatches(hand))
                return true;

            if (IsBalanced && !BasicBidding.IsBalanced(hand))
                return false;

            if (IsGood && !BasicBidding.IsGoodSuit(hand, declareBid.suit))
                return false;

            var points = BasicBidding.ComputeHighCardPoints(hand);
            switch (BidPointType)
            {
                case BidPointType.Distribution:
                    points += BasicBidding.ComputeDistributionPoints(hand);
                    break;
                case BidPointType.Dummy:
                    points += BasicBidding.ComputeDummyPoints(hand);
                    break;
                case BidPointType.Hcp:
                    break;
                default:
                    throw new Exception("Unknown point type");
            }

            if (Points.Min > points || points > Points.Max)
                return false;

            var counts = BasicBidding.CountsBySuit(hand);
            if (HandShape.Any(hs => Math.Min(hs.Value.Min, hs.Value.MinMatch) > counts[hs.Key] || counts[hs.Key] > hs.Value.Max))
                return false;

            if (Aces.Count != 0 && !Aces.Contains(hand.Count(c => c.rank == Rank.Ace)))
                return false;

            if (Kings.Count != 0 && !Kings.Contains(hand.Count(c => c.rank == Rank.King)))
                return false;

            //  don't match bids with "unset" hand-related values (except "Pass")
            if (bid != BidBase.Pass && !IsBalanced && Points.Min == 0 && Points.Max == 37 &&
                HandShape.All(hs => hs.Value.Min == 0 && hs.Value.Max == 13) && Aces.Count == 0 && Kings.Count == 0)
                return false;

            //  don't allow an unknown pass as a response to a forcing bid without interference
            if (bid == BidBase.Pass && Description == UnknownDescripton && History.Count >= 2 && History[Index - 1].bid == BidBase.Pass &&
                History[Index - 2].BidMessage == BidMessage.Forcing)
                return false;

            //  run any custom-validation required for this bid
            return Validate == null || Validate(hand);
        }

        public void NoFourCardMajors()
        {
            foreach (var s in SuitRank.stdSuits.Where(BridgeBot.IsMajor))
                HandShape[s].Max = Math.Min(HandShape[s].Max, 3);
        }

        public void SetHandShapeMaxesOfOtherSuits(Suit suit, int max)
        {
            SetHandShapeMaxes();

            foreach (var s in SuitRank.stdSuits.Where(s => s != suit))
                HandShape[s].Max = Math.Min(HandShape[s].Max, max);
        }

        private bool InterpretConventions()
        {
            if (ArtificialInquiry.Interpret(this))
                return true;

            if (Blackwood.Interpret(this))
                return true;

            if (Cappelletti.Interpret(this))
                return true;

            if (ControlBid.Interpret(this))
                return true;

            if (FourthSuitForcing.Interpret(this))
                return true;

            if (Gerber.Interpret(this))
                return true;

            if (Jacoby2NT.Interpret(this))
                return true;

            if (JacobyTransfer.Interpret(this))
                return true;

            if (NegativeDouble.Interpret(this))
                return true;

            if (Relay.Interpret(this))
                return true;

            if (Stayman.Interpret(this))
                return true;

            if (StrongOpening.Interpret(this))
                return true;

            if (TakeoutDouble.Interpret(this))
                return true;

            return false;
        }

        private void InterpretPhase()
        {
            switch (BidPhase)
            {
                case BidPhase.Opening:
                    Opening.Interpret(this);
                    break;
                case BidPhase.Overcall:
                    Overcall.Interpret(this);
                    break;
                case BidPhase.Response:
                    Response.Interpret(this);
                    break;
                case BidPhase.Advance:
                    Advance.Interpret(this);
                    break;
                case BidPhase.OpenerRebid:
                    OpenerRebid.Interpret(this);
                    break;
                case BidPhase.OvercallRebid:
                    //  TODO: OvercallRebid.Interpret(this);
                    break;
                case BidPhase.ResponderRebid:
                    ResponderRebid.Interpret(this);
                    break;
                case BidPhase.AdvanceRebid:
                    //  TODO: AdvanceRebid.Interpret(this);
                    break;
            }
        }

        private BidPhase PhaseOfBid()
        {
            var rho = Index > 0 ? History[Index - 1] : null;

            //  set phase to opening if everyone who has bid before us has passed
            if (rho == null || rho.BidPhase == BidPhase.Opening && rho.bid == BidBase.Pass)
                return BidPhase.Opening;

            //  someone has not passed, if it wasn't our team, we're overcalling
            if (rho.BidPhase == BidPhase.Opening)
                return BidPhase.Overcall;

            var partner = History[Index - 2];

            //  our team has bid before, I'm responding if my partner opened
            if (partner.BidPhase == BidPhase.Opening && partner.bid != BidBase.Pass)
                return BidPhase.Response;

            //  our team has bid before, I'm responding if my partner overcalled
            if (partner.BidPhase == BidPhase.Overcall && partner.bid != BidBase.Pass)
                return BidPhase.Advance;

            //  if my rho is responding, but partner didn't overcall, then we're overcalling
            if (rho.BidPhase == BidPhase.Response)
                return BidPhase.Overcall;

            var previous = History[Index - 4];

            //  my previous bid was a non-pass opening, so I'm rebidding the opener
            if (previous.BidPhase == BidPhase.Opening && previous.bid != BidBase.Pass)
                return BidPhase.OpenerRebid;

            //  my previous bid was a non-pass overcall, so I'm rebidding the overcall
            if (previous.BidPhase == BidPhase.Overcall && previous.bid != BidBase.Pass)
                return BidPhase.OvercallRebid;

            //  my previous bid was a response, so I'm rebidding the response
            if (previous.BidPhase == BidPhase.Response)
                return BidPhase.ResponderRebid;

            //  my previous bid was an advance, so I'm rebidding the advance
            if (previous.BidPhase == BidPhase.Advance)
                return BidPhase.AdvanceRebid;

            return BidPhase.Unknown;
        }

        private void SetBalancedHandShape()
        {
            foreach (var s in SuitRank.stdSuits)
            {
                HandShape[s].Min = Math.Max(HandShape[s].Min, 2);
                HandShape[s].Max = Math.Min(HandShape[s].Max, 5);
            }
        }

        private void SetHandShapeMaxes()
        {
            var totalMin = HandShape.Values.Sum(r => r.Min);

            foreach (var s in SuitRank.stdSuits)
                HandShape[s].Max = Math.Min(HandShape[s].Max, 13 - totalMin + HandShape[s].Min);
        }

        internal class PlayerSummary : Summary
        {
            public PlayerSummary(IReadOnlyList<InterpretedBid> history, int lastIndex)
            {
                var firstIndex = lastIndex % 4;
                for (var i = firstIndex; i >= 0 && i <= lastIndex; i += 4)
                {
                    var bid = history[i];
                    Points.Min = Math.Max(Points.Min, bid.Points.Min);
                    Points.Max = Math.Min(Points.Max, bid.Points.Max);

                    foreach (var s in SuitRank.stdSuits)
                    {
                        HandShape[s].Min = Math.Max(HandShape[s].Min, bid.HandShape[s].Min);
                        HandShape[s].Max = Math.Min(HandShape[s].Max, bid.HandShape[s].Max);
                    }
                }
            }

            public bool IsBalanced
            {
                get
                {
                    foreach (var s in SuitRank.stdSuits)
                        if (HandShape[s].Min < 2)
                            return false;
                    return true;
                }
            }

            public void AdjustByOtherPlayers(IReadOnlyList<PlayerSummary> otherPlayers)
            {
                if (otherPlayers.Count != 3)
                    throw new ArgumentException("ALL other players must be provided");

                AdjustBy(Combine(otherPlayers));
            }
        }

        internal class Range
        {
            public int Max;
            public int Min;
            public int MinMatch;

            public Range(int min, int max)
            {
                Min = min;
                Max = max;
                MinMatch = 13; // Lower of Min vs MinMatch will be used for matching
            }
        }

        internal class Summary
        {
            public Summary()
            {
                Points = new Range(0, 40);

                HandShape = new Dictionary<Suit, Range>();
                foreach (var s in SuitRank.stdSuits)
                    HandShape.Add(s, new Range(0, 13));
            }

            public Dictionary<Suit, Range> HandShape { get; }
            public Range Points { get; }

            public static Summary Combine(IEnumerable<Summary> summaries)
            {
                var summary = new Summary();

                foreach (var p in summaries)
                {
                    summary.Points.Min += p.Points.Min;
                    summary.Points.Max += p.Points.Max;

                    foreach (var s in SuitRank.stdSuits)
                    {
                        summary.HandShape[s].Min += p.HandShape[s].Min;
                        summary.HandShape[s].Max += p.HandShape[s].Max;
                    }
                }

                return summary;
            }

            protected void AdjustBy(Summary others)
            {
                Points.Min = Math.Max(Points.Min, 40 - others.Points.Max);
                Points.Max = Math.Min(Points.Max, 40 - others.Points.Min);

                foreach (var s in SuitRank.stdSuits)
                {
                    HandShape[s].Min = Math.Max(HandShape[s].Min, 13 - others.HandShape[s].Max);
                    HandShape[s].Max = Math.Min(HandShape[s].Max, 13 - others.HandShape[s].Min);
                }
            }
        }

        internal class TeamSummary : Summary
        {
            public PlayerSummary p1;
            public PlayerSummary p2;

            public TeamSummary(IReadOnlyList<InterpretedBid> history, int lastIndex)
            {
                p1 = new PlayerSummary(history, lastIndex);
                p2 = new PlayerSummary(history, lastIndex - 2);

                Points.Min = p1.Points.Min + p2.Points.Min;
                Points.Max = p1.Points.Max + p2.Points.Max;

                foreach (var s in SuitRank.stdSuits)
                {
                    HandShape[s].Min = p1.HandShape[s].Min + p2.HandShape[s].Min;
                    HandShape[s].Max = p1.HandShape[s].Max + p2.HandShape[s].Max;
                }
            }

            public void AdjustByOpponents(TeamSummary opponents)
            {
                AdjustBy(opponents);
            }
        }
    }

}