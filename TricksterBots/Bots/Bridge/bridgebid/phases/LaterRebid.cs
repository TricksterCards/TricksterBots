using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    //  Opener/responder/overcaller 3rd+ calls and advancer 2nd+ calls (no BidPhase exists for these).
    internal class LaterRebid
    {
        private const int MajorFitPriority = 90;
        private const int NotrumpPriority = 95;
        private const int NewSuitPriority = 110;

        public static bool Applies(InterpretedBid bid)
        {
            if (bid.Index < 4)
                return false;

            for (var i = bid.Index - 2; i >= 0; i -= 2)
                if (bid.History[i].bid != BidBase.Pass)
                    return true;

            return false;
        }

        public static void Interpret(InterpretedBid bid)
        {
            var context = new Context(bid);

            if (bid.bid == BidBase.Pass)
                InterpretPass(context);
            else if (bid.bidIsDeclare)
                InterpretDeclare(context);
        }

        private static void InterpretPass(Context context)
        {
            var bid = context.Bid;

            if (context.PartnerSignedOff)
            {
                bid.Description = "Accept sign-off";
            }
            else if (context.IsInvitation)
            {
                //  a minor-suit invitation is usually aiming for 3NT
                var strain = context.PartnerLast.declareBid.suit;
                var gameStrain = BridgeBot.IsMinor(strain) ? Suit.Unknown : strain;
                bid.Points.Max = GamePoints(gameStrain) - 1 - context.PartnerInviteValue(gameStrain);
                SetPointCounter(context, gameStrain);
                bid.Description = "Decline invitation";
            }
            else if (context.PartnerForced && !context.RhoBid)
            {
                bid.Description = "Forcing";
                bid.Validate = hand => false;
            }
        }

        private static void InterpretDeclare(Context context)
        {
            var bid = context.Bid;
            var suit = bid.declareBid.suit;
            var level = bid.declareBid.level;

            if (suit != Suit.Unknown && context.OpponentSuits.Contains(suit) &&
                context.Me.HandShape[suit].Min == 0 && context.Partner.HandShape[suit].Min == 0)
            {
                //  TODO: cuebids of the opponents' suit
                bid.BidMessage = BidMessage.Forcing;
                bid.Description = "Cuebid";
                bid.Validate = hand => false;
                return;
            }

            if (suit == Suit.Unknown && level == 4)
            {
                bid.BidConvention = BidConvention.Blackwood;
                bid.BidMessage = BidMessage.Forcing;
                bid.Description = "asking for Aces";
                //  TODO: validate knowing count of Aces will help decision to bid slam
                bid.Validate = hand => false;
                return;
            }

            var isKnownStrain = SetStrainRequirements(context);
            if (isKnownStrain && level >= 6)
            {
                Slam(context);
                return;
            }

            if (context.PartnerSignedOff && !context.Competing)
            {
                Reject(bid);
                return;
            }

            var isCheapest = isKnownStrain && level == bid.LowestAvailableLevel(suit, true);

            if (context.IsInvitation)
            {
                if (isKnownStrain && level == bid.GameLevel)
                {
                    bid.Points.Min = bid.GamePoints - context.PartnerInviteValue(suit);
                    bid.BidMessage = BidMessage.Signoff;
                    bid.Description = "Accept invitation";
                }
                else if (isCheapest && level == 3 && BridgeBot.IsMajor(suit) && suit != context.PartnerStrain &&
                         !context.OpponentSuits.Contains(suit) && context.Partner.HandShape[suit].Min >= 4 &&
                         context.PartnerBidNaturally(suit))
                {
                    //  delayed 3-card support lets partner choose between 3NT and the major (partner's bid suit may be 5+)
                    bid.Points.Min = GamePoints(Suit.Unknown) - context.PartnerInviteValue(Suit.Unknown);
                    bid.HandShape[suit].Min = 3;
                    bid.HandShape[suit].Max = 3;
                    bid.BidMessage = BidMessage.Forcing;
                    bid.Description = $"3 {suit}; choice of games";
                }
                else
                {
                    Reject(bid);
                }

                return;
            }

            if (!isKnownStrain)
            {
                NewSuit(context);
                return;
            }

            if (context.PartnerForced && isCheapest)
            {
                if (suit != Suit.Unknown)
                    PreferLongestFit(context);
                bid.Description = level == bid.GameLevel ? "Game" : "Natural";
                return;
            }

            PlaceContract(context, isCheapest);
        }

        private static void PlaceContract(Context context, bool isCheapest)
        {
            var bid = context.Bid;
            var suit = bid.declareBid.suit;
            var level = bid.declareBid.level;
            var inviteMin = Math.Max(bid.GamePoints - context.PartnerMax, InterpretedBid.InvitationalPoints - context.PartnerMin);

            if (level > bid.GameLevel)
            {
                Reject(bid);
            }
            else if (level == bid.GameLevel && !context.PartnerSignedOff)
            {
                bid.Points.Min = bid.GamePoints - context.PartnerMin;
                bid.BidMessage = BidMessage.Signoff;
                bid.Description = "Sign-off at game";
            }
            else if (level == bid.GameLevel - 1 && !context.PartnerSignedOff)
            {
                bid.Points.Min = inviteMin;
                bid.Points.Max = bid.GamePoints - 1 - context.PartnerMin;
                bid.IsBalanced = suit == Suit.Unknown;
                bid.Description = "Inviting game";
            }
            else if (isCheapest && level < bid.GameLevel && (context.Competing || suit != context.PartnerStrain))
            {
                bid.Points.Max = inviteMin - 1;
                bid.BidMessage = BidMessage.Signoff;
                bid.Description = context.Competing ? "Competing" : "Preference";

                if (suit != Suit.Unknown)
                {
                    var partnerLength = context.Partner.HandShape[suit].Min;
                    if (context.Competing && partnerLength >= 3)
                        //  Law of Total Tricks: compete to the level of the combined trump length
                        bid.HandShape[suit].Min = Math.Max(bid.HandShape[suit].Min, level + 6 - partnerLength);
                    PreferLongestFit(context);
                }
            }
            else
            {
                Reject(bid);
            }
        }

        private static void NewSuit(Context context)
        {
            var bid = context.Bid;
            if (bid.declareBid.level >= bid.GameLevel)
            {
                Reject(bid);
                return;
            }

            bid.BidMessage = BidMessage.Forcing;
            bid.HandShape[bid.declareBid.suit].Min = 4;
            bid.Priority = NewSuitPriority;
            bid.Description = "New suit";
            if (!context.PartnerForced)
                bid.Points.Min = GamePoints(Suit.Unknown) - context.PartnerMin;
        }

        private static void Slam(Context context)
        {
            var bid = context.Bid;
            var isSmallSlam = bid.declareBid.level == 6;

            bid.PointCounter = null;
            bid.BidPointType = BidPointType.Hcp;
            bid.Points.Min = (isSmallSlam ? InterpretedBid.SmallSlamPoints : InterpretedBid.GrandSlamPoints) - context.PartnerMin;
            if (isSmallSlam)
                bid.Points.Max = InterpretedBid.GrandSlamPoints - 1 - context.PartnerMin;
            bid.BidMessage = BidMessage.Signoff;
            bid.Description = isSmallSlam ? "Small slam" : "Grand slam";
        }

        private static void Reject(InterpretedBid bid)
        {
            bid.Validate = hand => false;
        }

        //  returns false if the strain is new for our side
        private static bool SetStrainRequirements(Context context)
        {
            var bid = context.Bid;
            var suit = bid.declareBid.suit;

            if (suit == Suit.Unknown)
            {
                bid.BidPointType = BidPointType.Hcp;
                bid.Priority = NotrumpPriority;
                var opponentSuits = context.OpponentSuits;
                var unbidSuits = SuitRank.stdSuits.Where(s => !context.OurSuits.Contains(s) && !opponentSuits.Contains(s)).ToList();
                bid.Validate = hand =>
                {
                    //  no shortness in a suit nobody has bid unless it's a stopper (e.g. a singleton Ace)
                    var counts = BasicBidding.CountsBySuit(hand);
                    return opponentSuits.All(s => BasicBidding.HasStopper(hand, s)) &&
                           unbidSuits.All(s => counts[s] >= 2 || BasicBidding.HasStopper(hand, s));
                };
                return true;
            }

            var partnerLength = context.Partner.HandShape[suit].Min;
            if (partnerLength > 0)
            {
                bid.HandShape[suit].Min = Math.Max(1, 8 - partnerLength);
                SetPointCounter(context, suit);
                if (BridgeBot.IsMajor(suit))
                    bid.Priority = MajorFitPriority;
                return true;
            }

            if (context.Me.HandShape[suit].Min > 0)
            {
                //  without support from partner, rebidding our own suit needs extra length
                bid.HandShape[suit].Min = bid.declareBid.level < bid.GameLevel ? 6 : 7;
                return true;
            }

            return false;
        }

        private static void SetPointCounter(Context context, Suit trump)
        {
            var bid = context.Bid;
            if (trump == Suit.Unknown)
            {
                bid.BidPointType = BidPointType.Hcp;
                return;
            }

            //  shortness only gains value opposite real (3+) support, not a balanced partner's doubleton
            if (context.Partner.HandShape[trump].Min < 3)
                return;

            //  we're dummy if partner bid the suit first
            var first = bid.History.FindIndex(b => b.Index < bid.Index && (bid.Index - b.Index) % 2 == 0 && b.bidIsDeclare && b.declareBid.suit == trump);
            var asDummy = first != -1 && (bid.Index - first) % 4 != 0;
            bid.PointCounter = hand => BasicBidding.ComputeFitPoints(hand, trump, asDummy);
        }

        private static void PreferLongestFit(Context context)
        {
            var bid = context.Bid;
            var suit = bid.declareBid.suit;
            var partnerShape = context.Partner.HandShape;
            var otherSuits = SuitRank.stdSuits.Where(s => s != suit && !context.OpponentSuits.Contains(s)).ToList();
            bid.Validate = hand =>
            {
                var counts = BasicBidding.CountsBySuit(hand);
                var combined = counts[suit] + partnerShape[suit].Min;
                return otherSuits.All(s => counts[s] + partnerShape[s].Min <= combined);
            };
        }

        private static int GamePoints(Suit suit)
        {
            return suit == Suit.Unknown ? 25 : BridgeBot.IsMajor(suit) ? 26 : 29;
        }

        private static bool IsForcing(InterpretedBid bid)
        {
            return bid.BidMessage == BidMessage.Forcing || bid.BidMessage == BidMessage.GameForcing;
        }

        private class Context
        {
            public readonly InterpretedBid Bid;
            public readonly bool Competing;
            public readonly bool IsInvitation;
            public readonly InterpretedBid.PlayerSummary Me;
            public readonly List<Suit> OpponentSuits;
            public readonly List<Suit> OurSuits;
            public readonly InterpretedBid.PlayerSummary Partner;
            public readonly bool PartnerForced;
            public readonly InterpretedBid PartnerLast;
            public readonly int PartnerMax;
            public readonly int PartnerMin;
            public readonly bool PartnerSignedOff;
            public readonly bool RhoBid;

            public Context(InterpretedBid bid)
            {
                Bid = bid;
                PartnerLast = bid.History[bid.Index - 2];
                Me = new InterpretedBid.PlayerSummary(bid.History, bid.Index - 4);
                Partner = new InterpretedBid.PlayerSummary(bid.History, bid.Index - 2);
                PartnerMin = Partner.Points.Min;
                PartnerMax = Math.Max(Partner.Points.Max, PartnerMin);

                RhoBid = bid.History[bid.Index - 1].bidIsDeclare;
                Competing = RhoBid || bid.History[bid.Index - 3].bidIsDeclare;
                OpponentSuits = SuitsBidBySide(bid, 1);
                OurSuits = SuitsBidBySide(bid, 0);

                PartnerSignedOff = PartnerLast.bidIsDeclare && PartnerLast.BidMessage == BidMessage.Signoff;
                PartnerForced = IsForcing(PartnerLast);
                IsInvitation = !RhoBid && IsPartnerInviting(bid, PartnerLast);
            }

            public Suit PartnerStrain => PartnerLast.bidIsDeclare ? PartnerLast.declareBid.suit : Suit.Unknown;

            public bool PartnerBidNaturally(Suit suit)
            {
                return Bid.History.Any(b => (Bid.Index - b.Index) % 4 == 2 && b.bidIsDeclare && b.declareBid.suit == suit &&
                                            b.BidConvention == BidConvention.None);
            }

            //  accept an invitation when we're above the middle of partner's range (but stay conservative about 5m)
            public int PartnerInviteValue(Suit gameStrain)
            {
                return PartnerMax >= 37 || BridgeBot.IsMinor(gameStrain) ? PartnerMin : (PartnerMin + PartnerMax + 1) / 2;
            }

            private static List<Suit> SuitsBidBySide(InterpretedBid bid, int parity)
            {
                return bid.History.Take(bid.Index)
                    .Where(b => (bid.Index - b.Index) % 2 == parity && b.bidIsDeclare && b.declareBid.suit != Suit.Unknown)
                    .Select(b => b.declareBid.suit).Distinct().ToList();
            }

            private static bool IsPartnerInviting(InterpretedBid bid, InterpretedBid partnerLast)
            {
                if (!partnerLast.bidIsDeclare || partnerLast.BidConvention != BidConvention.None ||
                    partnerLast.BidMessage == BidMessage.Signoff || IsForcing(partnerLast))
                    return false;

                //  matches TryPlaceContract's invitational levels: 2NT or 3 of a suit
                var strain = partnerLast.declareBid.suit;
                if (partnerLast.declareBid.level != (strain == Suit.Unknown ? 2 : 3))
                    return false;

                if (strain != Suit.Unknown && !bid.History.Take(partnerLast.Index)
                        .Any(b => (partnerLast.Index - b.Index) % 2 == 0 && b.bidIsDeclare && b.declareBid.suit == strain))
                    return false;

                var isJump = partnerLast.declareBid.level > partnerLast.LowestAvailableLevel(strain, true);
                return isJump || bid.History[bid.Index - 3].bid == BidBase.Pass;
            }
        }
    }
}
