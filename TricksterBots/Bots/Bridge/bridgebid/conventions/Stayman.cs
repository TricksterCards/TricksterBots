using System;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class Stayman
    {
        //  Stayman can be used after a notrump opening or after a notrump rebid of a strong opening (2C) where responder is waiting (2D).
        //  Stayman is "off" if opponents interfere with anything other than a double, but a cuebid can be used with game-forcing strength as a substitute.
        public static bool CanUseStayman(InterpretedBid bid)
        {
            if (!CanUseAlternateStayman(bid))
                return false;

            var interference = bid.History[bid.Index - 1];
            if (interference.bid != BidBase.Pass && interference.bid != BridgeBid.Double)
                return false;

            return true;
        }

        public static bool Interpret(InterpretedBid bid)
        {
            if (CanUseStayman(bid)) return InterpretStayman(bid);

            //  Acol plays "system off" over a suit overcall of 1NT, so no cuebid-Stayman substitute
            if (bid.Options.bidding != BridgeBiddingScheme.Acol && CanUseAlternateStayman(bid))
                return InterpretCuebidStayman(bid.History[bid.Index - 1], bid);

            if (bid.Index >= 4 && bid.History[bid.Index - 2].BidConvention == BidConvention.Stayman)
            {
                InterpretOpenerRebid(bid);
                return true;
            }

            if (bid.Index >= 6 && bid.History[bid.Index - 4].BidConvention == BidConvention.Stayman) return InterpretResponderRebid(bid);

            if (bid.Index >= 8 && bid.History[bid.Index - 6].BidConvention == BidConvention.Stayman) return InterpretOpenerSecondRebid(bid);

            return false;
        }

        //  1N-2C-2H-2N/3N: responder denied hearts but promised a 4-card major, so opener shows 4 spades too
        private static bool InterpretOpenerSecondRebid(InterpretedBid bid)
        {
            var answer = bid.History[bid.Index - 4];
            var rebid = bid.History[bid.Index - 2];
            if (!bid.bidIsDeclare || !answer.bidIsDeclare || !rebid.bidIsDeclare || bid.History[bid.Index - 1].bid != BidBase.Pass ||
                answer.declareBid.suit != Suit.Hearts || rebid.declareBid.suit != Suit.Unknown || bid.declareBid.suit != Suit.Spades)
                return false;

            var opening = bid.History[bid.Index - 8];
            bid.HandShape[Suit.Spades].Min = 4;
            //  prefer the known 4-4 spade fit to notrump
            bid.Priority = 90;
            if (rebid.declareBid.level == 2 && bid.declareBid.level == 3)
            {
                //  minimum: decline the invitation, but play in the fit
                bid.BidPointType = BidPointType.Hcp;
                bid.Points.Max = (opening.Points.Min + opening.Points.Max) / 2 - 1;
                bid.Description = "4 Spades; minimum";
                return true;
            }

            if (bid.declareBid.level == 4 && (rebid.declareBid.level == 3 || rebid.declareBid.level == 2))
            {
                bid.BidPointType = BidPointType.Hcp;
                if (rebid.declareBid.level == 2)
                    bid.Points.Min = (opening.Points.Min + opening.Points.Max) / 2;
                bid.BidMessage = BidMessage.Signoff;
                bid.Description = "4 Spades; game";
                return true;
            }

            return false;
        }

        private static bool CanUseAlternateStayman(InterpretedBid bid)
        {
            if (bid.Index < 2)
                return false;

            var opening = bid.History[bid.Index - 2];
            if (opening.BidConvention != BidConvention.None)
                return false;

            if (!opening.bidIsDeclare || opening.declareBid.suit != Suit.Unknown)
                return false;

            //  (SAYC Booklet) Stayman is the only conventional response to partner's natural 1NT overcall (transfers are off)
            if (bid.BidPhase == BidPhase.Advance)
                return opening.BidPhase == BidPhase.Overcall && opening.declareBid.level == 1 && bid.Options.bidding != BridgeBiddingScheme.Acol;

            if (bid.BidPhase != BidPhase.Response && bid.BidPhase != BidPhase.ResponderRebid)
                return false;

            if (bid.BidPhase == BidPhase.ResponderRebid)
            {
                var originalOpening = bid.History[bid.Index - 6];
                if (originalOpening.BidConvention != BidConvention.StrongOpening)
                    return false;

                var originalResponse = bid.History[bid.Index - 4];
                if (originalResponse.BidConvention != BidConvention.Waiting)
                    return false;
            }

            return true;
        }

        private static bool InterpretCuebidStayman(InterpretedBid interference, InterpretedBid response)
        {
            if (!response.bidIsDeclare)
                return false;

            var cuebidSuit = interference.declareBid.suit;
            if (response.declareBid.suit != cuebidSuit)
                return false;

            if (response.declareBid.level > 3)
                return false;

            if (response.declareBid.level != interference.declareBid.level + 1)
                return false;

            response.BidConvention = BidConvention.Stayman;
            response.BidMessage = BidMessage.Forcing;
            response.BidPointType = BidPointType.Hcp;
            response.Points.Min = 10;

            if (BridgeBot.IsMajor(cuebidSuit))
            {
                var other = cuebidSuit == Suit.Hearts ? Suit.Spades : Suit.Hearts;
                response.HandShape[other].Min = 4;
                response.Description = $"4+ {other}";
            }
            else
            {
                response.Validate = hand =>
                {
                    //  we should have 4H or 4S
                    var counts = BasicBidding.CountsBySuit(hand);
                    return counts[Suit.Hearts] >= 4 || counts[Suit.Spades] >= 4;
                };
                response.Description = "asking for a major";
            }

            return true;
        }

        private static void InterpretOpenerRebid(InterpretedBid bid)
        {
            var interference = bid.History[bid.Index - 1];

            if (bid.bid == BridgeBid.Double && interference.bidIsDeclare && interference.declareBid.suit != Suit.Unknown)
            {
                //  1N-(P)-2C-(2H)-X: penalty, with length and strength in their suit
                var theirSuit = interference.declareBid.suit;
                bid.Priority = 90;
                bid.Description = $"Penalty; 4+ good {theirSuit}";
                bid.Validate = hand => BasicBidding.CountsBySuit(hand)[theirSuit] >= 4 &&
                                       hand.Count(c => c.suit == theirSuit && c.rank >= Rank.Queen) >= 2;
                return;
            }

            if (bid.bid == BridgeBid.Redouble && interference.bid == BridgeBid.Double)
            {
                //  1N-(P)-2C-(X)-XX: strong clubs, happy to play 2C redoubled
                bid.Priority = 90;
                bid.HandShape[Suit.Clubs].Min = 4;
                bid.Description = "Strong Clubs";
                bid.Validate = hand => hand.Count(c => c.suit == Suit.Clubs && c.rank >= Rank.Queen) >= 2;
                return;
            }

            if (!bid.bidIsDeclare)
                return;

            var stayman = bid.History[bid.Index - 2].declareBid;
            var db = bid.declareBid;

            //  the only accepted responses are in a suit at the same level
            if (db.suit == Suit.Unknown || db.level != stayman.level)
                return;

            switch (db.suit)
            {
                //  2C-2D
                //  3C-3D
                //  4C-4D
                case Suit.Diamonds:
                    bid.BidConvention = BidConvention.AnswerStayman;
                    bid.HandShape[Suit.Hearts].Max = 3;
                    bid.HandShape[Suit.Spades].Max = 3;
                    bid.Description = "No 4+ card major";
                    break;
                //  2C-2H
                //  2C-2S
                //  3C-3H
                //  3C-3S
                //  4C-4H
                //  4C-4S
                case Suit.Hearts:
                case Suit.Spades:
                    bid.HandShape[db.suit].Min = 4;
                    bid.Description = $"4+ {db.suit}";
                    if (db.suit == Suit.Spades)
                    {
                        bid.HandShape[Suit.Hearts].Max = 3;
                        bid.Description += "; denies 4 Hearts";
                    }

                    break;
            }
        }

        private static bool InterpretResponderRebid(InterpretedBid rebid)
        {
            var answer = rebid.History[rebid.Index - 2];

            if (!rebid.bidIsDeclare || !answer.bidIsDeclare)
                return false;

            var opening = rebid.History[rebid.Index - 6];
            var stayman = rebid.History[rebid.Index - 4];

            //  Stayman over 2NT (or higher) already promised game values, so honor that minimum when bidding game
            var gameMin = stayman.declareBid.level >= 3
                ? Math.Min(stayman.Points.Min, rebid.GamePoints - opening.Points.Min)
                : rebid.GamePoints - opening.Points.Min;

            if (rebid.declareBid.level <= 3 && BridgeBot.IsMajor(rebid.declareBid.suit) && rebid.declareBid.suit != answer.declareBid.suit)
            {
                var otherMajor = rebid.declareBid.suit == Suit.Hearts ? Suit.Spades : Suit.Hearts;
                if (BridgeBot.IsMajor(answer.declareBid.suit))
                {
                    //  after opener shows the other major: 4 cards at the 2-level, 5+ when jumping
                    rebid.HandShape[rebid.declareBid.suit].Min = rebid.declareBid.level == 2 ? 4 : 5;
                    InferOtherMajor(answer, rebid);
                    rebid.Description = $"{rebid.HandShape[rebid.declareBid.suit].Min}+ {rebid.declareBid.suit}; denies 4 {otherMajor}";
                }
                else
                {
                    //  show a 5-card major by bidding it (implies 4 of the other major)
                    rebid.HandShape[rebid.declareBid.suit].Min = 5;
                    rebid.HandShape[otherMajor].Min = 4;
                    rebid.Description = $"5+ {rebid.declareBid.suit} and 4+ {otherMajor}";
                }

                if (rebid.declareBid.level == 2)
                {
                    //  show invitational hand by rebidding at the 2-level
                    rebid.Points.Min = InterpretedBid.InvitationalPoints - opening.Points.Min;
                    rebid.Points.Max = rebid.GamePoints - 1 - opening.Points.Min;
                }
                else
                {
                    //  show a game forcing hand by jumping a level
                    rebid.BidMessage = BidMessage.Forcing;
                    rebid.Points.Min = gameMin;
                }

                return true;
            }

            if (rebid.declareBid.level == 3 && BridgeBot.IsMinor(rebid.declareBid.suit))
            {
                //  (SAYC Booklet): if responder rebids three of either minor, he shows slam interest and at least a five-card suit
                var shownMajor = BridgeBot.IsMajor(answer.declareBid.suit) ? answer.declareBid.suit : Suit.Unknown;
                rebid.Points.Min = 31 - opening.Points.Max;
                rebid.HandShape[rebid.declareBid.suit].Min = 5;
                rebid.Priority = 90;
                rebid.Description = $"5+ {rebid.declareBid.suit}; slam interest";
                //  with a fit for opener's major, that's the trump suit instead
                rebid.Validate = hand => shownMajor == Suit.Unknown || BasicBidding.CountsBySuit(hand)[shownMajor] < 4;
                return true;
            }

            //  invite game in 2NT when nothing else fits (does not need to be balanced in this case)
            if (rebid.declareBid.level == 2 && rebid.declareBid.suit == Suit.Unknown)
            {
                rebid.BidPointType = BidPointType.Hcp;
                rebid.Points.Min = InterpretedBid.InvitationalPoints - opening.Points.Min;
                rebid.Points.Max = rebid.GamePoints - 1 - opening.Points.Min;
                rebid.Description = "Inviting game";
                InferOtherMajor(answer, rebid);
                return true;
            }

            if (rebid.declareBid.level == 3 && rebid.declareBid.suit == Suit.Unknown)
            {
                rebid.BidPointType = BidPointType.Hcp;
                rebid.Points.Min = gameMin;
                rebid.BidMessage = BidMessage.Signoff;
                rebid.Description = "Sign-off at game";
                InferOtherMajor(answer, rebid);
                return true;
            }

            return false;
        }

        //  Stayman promised a 4-card major, so not raising the one opener showed means holding the other
        private static void InferOtherMajor(InterpretedBid answer, InterpretedBid rebid)
        {
            var shown = answer.declareBid.suit;
            if (!BridgeBot.IsMajor(shown) || rebid.declareBid.suit == shown)
                return;

            var other = shown == Suit.Hearts ? Suit.Spades : Suit.Hearts;
            rebid.HandShape[shown].Max = Math.Min(rebid.HandShape[shown].Max, 3);
            rebid.HandShape[other].Min = Math.Max(rebid.HandShape[other].Min, 4);
        }

        private static bool InterpretStayman(InterpretedBid response)
        {
            if (!response.bidIsDeclare)
                return false;

            if (response.declareBid.suit != Suit.Clubs)
                return false;

            if (response.declareBid.level != response.LowestAvailableLevel(response.declareBid.suit))
                return false;

            //  1N-2C
            //  2N-3C
            //  3N-4C
            //  2C-2D-2N-3C
            //  2C-2D-3N-4C
            response.BidConvention = BidConvention.Stayman;
            response.BidMessage = BidMessage.Forcing;
            response.BidPointType = BidPointType.Hcp;
            //  over a weak NT (Acol), Stayman promises at least invitational values
            response.Points.Min = response.declareBid.level <= 2 ? (response.Options.bidding == BridgeBiddingScheme.Acol ? 11 : 8) : 4;
            var ntBid = response.History[response.Index - 2];
            var ntMax = ntBid.Points.Max < 37 ? ntBid.Points.Max : 17;
            //  opposite a strong 2NT (e.g. 2C-2D-2N), even 3 HCP is enough for game
            if (response.declareBid.level == 3)
                response.Points.Min = Math.Max(3, 25 - ntMax);
            //  over a notrump overcall, Stayman also needs invitational values for the overcall's range
            if (response.BidPhase == BidPhase.Advance)
                response.Points.Min = 25 - response.History[response.Index - 2].Points.Max;
            response.Description = "asking for a major";
            response.Priority = 1; // always prefer Stayman over other bids when valid
            //  (SAYC Booklet) Stayman followed by 3 of a minor shows slam interest, so a long minor with slam values also starts with Stayman
            var minorSlamHcp = 31 - ntMax;
            response.Validate = hand =>
            {
                //  we should have 4H or 4S (any more and we'll use a transfer instead)
                var counts = BasicBidding.CountsBySuit(hand);
                if (counts[Suit.Hearts] == 4 || counts[Suit.Spades] == 4)
                    return true;

                return response.Options.bidding != BridgeBiddingScheme.Acol && counts[Suit.Hearts] < 5 && counts[Suit.Spades] < 5 &&
                       SuitRank.stdSuits.Any(s => BridgeBot.IsMinor(s) && counts[s] >= 5 && Overcall.SuitQuality(hand, s) >= 8) &&
                       BasicBidding.ComputeHighCardPoints(hand) >= minorSlamHcp;
            };

            return true;
        }
    }
}