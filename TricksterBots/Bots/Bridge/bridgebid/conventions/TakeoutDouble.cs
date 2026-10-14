using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    internal class TakeoutDouble
    {
        public static bool Interpret(InterpretedBid bid)
        {
            if (bid.BidPhase == BidPhase.Overcall && bid.bid == BridgeBid.Double)
            {
                //  we overcalled with a double - check if it is a takeout double
                InterpretedBid opening;
                InterpretedBid response = null;
                if (bid.History[bid.Index - 1].BidPhase == BidPhase.Opening)
                {
                    opening = bid.History[bid.Index - 1];
                }
                else
                {
                    opening = bid.History[bid.Index - 3];
                    response = bid.History[bid.Index - 1];
                }

                return Overcall(opening, response, bid);
            }

            if (bid.BidPhase == BidPhase.Response && bid.History[bid.Index - 1].BidConvention == BidConvention.TakeoutDouble)
                return Response(bid.History[bid.Index - 2], bid);

            if (bid.Index >= 2 && bid.History[bid.Index - 2].BidConvention == BidConvention.TakeoutDouble)
            {
                Advance(bid);
                return true;
            }

            if (bid.Index >= 4 && bid.History[bid.Index - 4].BidConvention == BidConvention.TakeoutDouble)
            {
                var advance = bid.History[bid.Index - 2];
                if (advance.bidIsDeclare)
                {
                    Rebid(advance, bid);
                    return true;
                }

                //  partner passed after the opponents bid again, so we're free to show extra values
                if (advance.bid == BidBase.Pass && (bid.History[bid.Index - 1].bidIsDeclare || bid.History[bid.Index - 3].bidIsDeclare))
                {
                    RebidAfterPass(bid);
                    return true;
                }
            }

            return false;
        }

        private static void RebidAfterPass(InterpretedBid rebid)
        {
            var opponentSuits = OpponentSuits(rebid);

            if (rebid.bid == BridgeBid.Double)
            {
                var last = rebid.History[rebid.Index - 1].bidIsDeclare ? rebid.History[rebid.Index - 1] : rebid.History[rebid.Index - 3];
                if (!last.bidIsDeclare || last.declareBid.suit == Suit.Unknown)
                    return;

                //  doubling again shows extra values and still asks partner to bid (partner may pass for penalty)
                rebid.BidConvention = BidConvention.TakeoutDouble;
                rebid.BidPointType = BidPointType.Hcp;
                rebid.Points.Min = 17;
                rebid.HandShape[last.declareBid.suit].Max = 2;
                rebid.Description = "Extra values";
                return;
            }

            if (!rebid.bidIsDeclare)
                return;

            var suit = rebid.declareBid.suit;
            if (suit == Suit.Unknown)
            {
                var range = NotrumpRange(rebid, rebid.declareBid.level);
                if (!range.HasValue || rebid.declareBid.level != rebid.LowestAvailableLevel(Suit.Unknown, true))
                    return;

                rebid.Points.Min = range.Value.min;
                rebid.Points.Max = range.Value.max;
                rebid.BidPointType = BidPointType.Hcp;
                rebid.IsBalanced = true;
                rebid.Description = $"stoppers in {string.Join(" and ", opponentSuits)}";
                rebid.Validate = hand => HasStoppers(hand, opponentSuits);
                return;
            }

            if (opponentSuits.Contains(suit) || rebid.declareBid.level > 3 || rebid.declareBid.level != rebid.LowestAvailableLevel(suit, true))
                return;

            //  a new suit after doubling shows a hand too strong for a simple overcall
            rebid.Points.Min = IsBalancingDouble(rebid) ? 15 : 18;
            rebid.HandShape[suit].Min = 5;
            rebid.Description = $"5+ {suit}; too strong for a simple overcall";
        }

        //  a double in the pass-out seat may be made with less, so later bids show about 3 fewer points
        private static bool IsBalancingDouble(InterpretedBid rebid)
        {
            var doubleIndex = rebid.Index - 4;
            return doubleIndex >= 3 && rebid.History[doubleIndex - 1].bid == BidBase.Pass && rebid.History[doubleIndex - 2].bid == BidBase.Pass &&
                   rebid.History[doubleIndex - 3].bidIsDeclare;
        }

        private static void Advance(InterpretedBid advance)
        {
            var takeoutDouble = advance.History[advance.Index - 2];
            var doubled = advance.History.Take(advance.Index).LastOrDefault(b => b.bidIsDeclare);

            if (advance.bid == BidBase.Pass)
            {
                //  converting a low-level takeout double to penalties needs length and strength in their suit
                if (advance.History[advance.Index - 1].bid == BidBase.Pass && doubled != null && doubled.declareBid.suit != Suit.Unknown &&
                    doubled.declareBid.level <= 2 && (advance.Index - doubled.Index) % 2 == 1)
                {
                    var theirSuit = doubled.declareBid.suit;
                    advance.Priority = 90;
                    advance.Description = $"Penalty pass; 5+ {theirSuit} with honors";
                    advance.Validate = hand =>
                    {
                        var length = BasicBidding.CountsBySuit(hand)[theirSuit];
                        var honors = hand.Count(c => c.suit == theirSuit && c.rank >= Rank.Jack);
                        return length >= 5 && length + honors >= 7;
                    };
                }

                return;
            }

            if (!advance.bidIsDeclare)
                return;

            //  the opponents' first suit (usually their opening, but our side may have opened and reopened with a double)
            var opening = FirstOpponentBid(advance) ?? advance.History.First(b => b.bid != BidBase.Pass);
            var lowestAvailableLevel = advance.LowestAvailableLevel(advance.declareBid.suit);
            //  partner may have doubled with less in the balancing seat, so we need a couple more points for each action
            var adjust = AdvanceAdjustment(takeoutDouble);
            var partnerSuits = advance.History.Take(advance.Index)
                .Where(b => (advance.Index - b.Index) % 4 == 2 && b.bidIsDeclare && b.declareBid.suit != Suit.Unknown && b.BidConvention == BidConvention.None)
                .Select(b => b.declareBid.suit).ToList();

            if (partnerSuits.Contains(advance.declareBid.suit))
            {
                //  partner opened and reopened with a double: return to partner's suit with a weak hand
                if (advance.declareBid.level == advance.LowestAvailableLevel(advance.declareBid.suit, true) && advance.declareBid.level <= 3)
                {
                    advance.Points.Max = 9;
                    advance.HandShape[advance.declareBid.suit].Min = 3;
                    advance.Description = $"Preference; 3+ {advance.declareBid.suit}";
                }

                return;
            }

            //  in notrump (with strength in the opponents' suit and no better option)
            if (advance.declareBid.suit == Suit.Unknown)
            {
                if (advance.declareBid.level < 4)
                {
                    advance.IsBalanced = true;
                    advance.Description = $"stopper in {opening.declareBid.suit}";
                    advance.Validate = hand => BasicBidding.HasStopper(hand, opening.declareBid.suit);

                    if (advance.declareBid.level == lowestAvailableLevel)
                    {
                        //  6-10 points: bid notrump at the cheapest level
                        advance.Points.Min = 6 + adjust;
                        advance.Points.Max = 10 + adjust;
                    }
                    else if (advance.declareBid.level == lowestAvailableLevel + 1)
                    {
                        //  11-12 points: bid notrump, jumping a level
                        advance.Points.Min = 11 + adjust;
                        advance.Points.Max = 12 + adjust;
                    }
                    else if (advance.declareBid.level == 3)
                    {
                        //  13+ points: bid game in notrump
                        advance.Points.Min = 13 + adjust;
                    }
                }
                else if (advance.declareBid.level == 4)
                {
                    advance.BidConvention = BidConvention.Blackwood;
                    advance.Points.Min = 20;
                    advance.Validate = hand => false;
                }
            }
            //  in a suit
            else
            {
                var gameLevel = BridgeBot.IsMajor(advance.declareBid.suit) ? 4 : 5;
                var bidSuits = advance.History.Where(b => b.bidIsDeclare).Select(b => b.declareBid.suit).Distinct().ToList();
                //  length in the opponents' suits doesn't help us
                advance.PointCounter = hand => BasicBidding.ComputeHighCardPoints(hand) + SuitRank.stdSuits
                    .Where(s => !bidSuits.Contains(s)).Select(s => BasicBidding.CountsBySuit(hand)[s]).Where(n => n >= 5).Sum(n => n - 4);

                var theirSuits = advance.History.Take(advance.Index)
                    .Where(b => (advance.Index - b.Index) % 2 == 1 && b.bidIsDeclare && b.declareBid.suit != Suit.Unknown)
                    .Select(b => b.declareBid.suit).Distinct().ToList();
                var theirLastSuit = advance.History.Take(advance.Index)
                    .LastOrDefault(b => (advance.Index - b.Index) % 2 == 1 && b.bidIsDeclare && b.declareBid.suit != Suit.Unknown)?.declareBid.suit;

                if (theirSuits.Contains(advance.declareBid.suit))
                {
                    //  cuebid (in the suit they bid last): game values without a clear strain (no 5-card unbid suit, but 4+ cards in two unbid suits)
                    var unbidSuits = SuitRank.stdSuits.Where(s => !bidSuits.Contains(s)).ToList();
                    advance.BidConvention = BidConvention.Cuebid;
                    advance.BidMessage = BidMessage.Forcing;
                    advance.Points.Min = 12 + adjust;
                    advance.Priority = 90;
                    advance.Description = "asking for more information";
                    advance.Validate = hand =>
                    {
                        var counts = BasicBidding.CountsBySuit(hand);
                        return advance.declareBid.suit == theirLastSuit && advance.declareBid.level == lowestAvailableLevel &&
                               unbidSuits.All(s => counts[s] < 5) && unbidSuits.Count(s => counts[s] >= 4) >= 2;
                    };
                }
                else if (!bidSuits.Contains(advance.declareBid.suit))
                {
                    if (advance.declareBid.level == lowestAvailableLevel)
                    {
                        //  0-8 points: bid at the cheapest level (but don't cap points if we're at the game level)
                        if (lowestAvailableLevel < gameLevel - 1)
                            advance.Points.Max = 8 + adjust;

                        //  over a doubled game, pass is a reasonable (penalty) option, so only pull with a long suit and some values
                        if (lowestAvailableLevel >= 5)
                        {
                            advance.Points.Min = 8;
                            advance.HandShape[advance.declareBid.suit].Min = 5;
                            advance.Description = $"5+ {advance.declareBid.suit}";
                            return;
                        }

                        //  usually 4+ cards, but when forced to bid (no interference) bid the best 3-card suit without a 4-card unbid suit
                        var isForced = advance.History[advance.Index - 1].bid == BidBase.Pass;
                        advance.HandShape[advance.declareBid.suit].Min = 4;
                        if (isForced)
                            advance.HandShape[advance.declareBid.suit].MinMatch = 3;
                        advance.Description = isForced ? $"3+ {advance.declareBid.suit} (usually 4+)" : $"4+ {advance.declareBid.suit}";
                        advance.Validate = hand =>
                        {
                            var counts = BasicBidding.CountsBySuit(hand);
                            var unbidCounts = counts.Where(kvp => !bidSuits.Contains(kvp.Key)).ToList();
                            var maxCount = unbidCounts.Max(kvp => kvp.Value);
                            if (counts[advance.declareBid.suit] != maxCount)
                                return false;

                            if (maxCount >= 4)
                                return true;

                            //  with only 3-card unbid suits, prefer a major, then the cheapest suit
                            var preferredSuit = unbidCounts.Where(kvp => kvp.Value == maxCount).Select(kvp => kvp.Key)
                                .OrderBy(s => BridgeBot.IsMajor(s) ? 0 : 1)
                                .ThenBy(s => BridgeBot.suitRank[s])
                                .First();
                            return advance.declareBid.suit == preferredSuit;
                        };
                    }
                    else if (advance.declareBid.level == lowestAvailableLevel + 1 && advance.declareBid.level <= 3)
                    {
                        //  9-11 points: make an invitational bid by jumping a level
                        advance.Points.Min = 9 + adjust;
                        advance.Points.Max = 11 + adjust;
                        advance.HandShape[advance.declareBid.suit].Min = 4;
                        advance.Description = $"4+ {advance.declareBid.suit}; inviting game";
                    }
                    else if (advance.declareBid.level == gameLevel - 1)
                    {
                        //  4-8 points: make a preemptive bid below game with 6+ cards
                        advance.Points.Min = 4;
                        advance.Points.Max = 8;
                        advance.HandShape[advance.declareBid.suit].Min = 6;
                        advance.IsPreemptive = true;
                        advance.Description = $"6+ {advance.declareBid.suit}";
                    }
                    else if (advance.declareBid.level == gameLevel)
                    {
                        //  12+ points: get the partnership to game
                        var minCards = BridgeBot.IsMajor(advance.declareBid.suit) ? 4 : 5;
                        advance.Points.Min = 12 + adjust;
                        advance.HandShape[advance.declareBid.suit].Min = minCards;
                        advance.Description = $"{minCards}+ {advance.declareBid.suit}";
                    }
                }
            }
        }

        private static bool Overcall(InterpretedBid opening, InterpretedBid response, InterpretedBid overcall)
        {
            //  a double is never for takeout over an NT opening
            if (opening.declareBid.suit == Suit.Unknown)
                return false;

            //  a double is for takeout over 4D or lower
            var level = response?.declareBid?.level ?? opening.declareBid.level;
            var suit = response?.declareBid?.suit ?? opening.declareBid.suit;
            if (BridgeBot.IsMinor(suit) ? level > 4 : level >= 4)
                return false;

            var bidSuits = SuitRank.stdSuits.Where(s =>
                    opening.bidIsDeclare && opening.declareBid.suit == s || response != null && response.bidIsDeclare && response.declareBid.suit == s)
                .OrderBy(s => BridgeBot.suitRank[s]).ToList();
            var unbidSuits = SuitRank.stdSuits.Where(s => !bidSuits.Contains(s)).OrderBy(s => BridgeBot.suitRank[s]).ToList();
            var unbidMajors = unbidSuits.Where(BridgeBot.IsMajor).ToList();

            //  handle takeout doubles worth 13+ dummy points (10+ when balancing) with 0-2 cards in the opponents' suit(s)
            var isBalancing = Bots.Overcall.IsBalancing(overcall);
            var maxOvercallHcp = Bots.Overcall.MaxSuitOvercallHcp(overcall);
            overcall.Points.Min = isBalancing ? 13 - Bots.Overcall.BalancingAdjustment : 13;
            overcall.BidPointType = BidPointType.Dummy;
            overcall.BidConvention = BidConvention.TakeoutDouble;
            overcall.BidMessage = BidMessage.Forcing;
            foreach (var s in bidSuits) overcall.HandShape[s].Max = 2;

            if (unbidSuits.Count == 3)
            {
                //  over a single suit, show 3+ cards in each unbid suit (usually 4+ in the unbid majors, see Validate below)
                foreach (var s in unbidSuits) overcall.HandShape[s].Min = 3;
                overcall.Description = $"3+ in each unbid suit";
            }
            else
            {
                //  over two suits, show 4+ cards in both unbid suits
                foreach (var s in unbidSuits) overcall.HandShape[s].Min = 4;
                overcall.Description = $"4+ {string.Join(" and ", unbidSuits)}";
            }

            //  still double with 4+ cards in another unbid major, to look for a major fit
            bool prefersSuitOvercall(Hand hand)
            {
                var counts = BasicBidding.CountsBySuit(hand);
                return unbidSuits.Any(s => overcall.LowestAvailableLevel(s, true) <= 2
                    && Bots.Overcall.IsStrongSuitOvercall(overcall, hand, s)
                    && !unbidSuits.Any(o => o != s && BridgeBot.IsMajor(o) && counts[o] >= 4)
                );
            }

            //  with only 3 cards in the unbid major(s), prefer a natural overcall in a 5+ card suit when one fits
            //  (when balancing, a double is preferred to a minor-suit overcall)
            bool prefersNaturalOvercall(Hand hand)
            {
                var counts = BasicBidding.CountsBySuit(hand);
                if (unbidMajors.Count == 0 || unbidMajors.Any(s => counts[s] >= 4))
                    return false;

                return unbidSuits.Any(s => counts[s] >= 5 && (!isBalancing || BridgeBot.IsMajor(s)) && new InterpretedBid(
                    new DeclareBid(overcall.LowestAvailableLevel(s, true), s), overcall.History, overcall.Index, overcall.Options).Match(hand));
            }

            //  if we can overcall 1NT (balanced; 15-18 HCP, or 11-14 when balancing; stopper in the opponents' suits) we'll defer to that instead
            bool prefersNotrumpOvercall(Hand hand)
            {
                var hcp = BasicBidding.ComputeHighCardPoints(hand);
                var min = isBalancing ? 11 : 15;
                return min <= hcp && hcp <= min + 3 && overcall.LowestAvailableLevel(Suit.Unknown, true) == 1 && BasicBidding.IsBalanced(hand) &&
                       HasStoppers(hand, bidSuits);
            }

            overcall.Validate = hand => !prefersSuitOvercall(hand) && !prefersNotrumpOvercall(hand) && !prefersNaturalOvercall(hand);
            overcall.AlternateMatches = hand =>
            {
                if (prefersSuitOvercall(hand) || prefersNotrumpOvercall(hand))
                    return false;

                //  otherwise a takeout double can also show a hand too strong for a simple overcall
                //  (but when balancing, a 15-17 hand short in an unbid major overcalls instead)
                return BasicBidding.ComputeHighCardPoints(hand) > maxOvercallHcp &&
                       !(isBalancing && BasicBidding.ComputeHighCardPoints(hand) <= Bots.Overcall.MaxOvercallHcp && Bots.Overcall.IsShortInUnbidMajor(overcall, hand));
            };

            return true;
        }

        private static void Rebid(InterpretedBid advance, InterpretedBid rebid)
        {
            if (!rebid.bidIsDeclare)
                return;

            var opening = FirstOpponentBid(rebid) ?? rebid.History.First(b => b.bid != BidBase.Pass);
            var suit = rebid.declareBid.suit;

            //  rebidding a suit we bid before the double (e.g. opener's reopening double) needs extra length
            var ourEarlierSuits = rebid.History.Take(rebid.Index - 4)
                .Where(b => (rebid.Index - b.Index) % 4 == 0 && b.bidIsDeclare && b.declareBid.suit != Suit.Unknown).Select(b => b.declareBid.suit).ToList();
            if (ourEarlierSuits.Contains(suit))
            {
                if (rebid.declareBid.level == rebid.LowestAvailableLevel(suit, true))
                {
                    rebid.HandShape[suit].Min = 6;
                    rebid.Description = $"6+ {suit}";
                }

                return;
            }

            if (advance.BidConvention == BidConvention.Cuebid)
            {
                //  answer advancer's cuebid by showing a 4-card major at the cheapest level
                if (BridgeBot.IsMajor(suit) && !OpponentSuits(rebid).Contains(suit) && rebid.declareBid.level == rebid.LowestAvailableLevel(suit, true))
                {
                    rebid.HandShape[suit].Min = 4;
                    rebid.Description = $"4+ {suit}";
                }

                return;
            }

            if (suit == opening.declareBid.suit && suit != Suit.Unknown)
            {
                //  cuebid: 19+ points without a better descriptive bid (other bids describing shape are preferred when sorting)
                rebid.BidConvention = BidConvention.Cuebid;
                rebid.BidMessage = BidMessage.Forcing;
                rebid.BidPointType = BidPointType.Hcp;
                rebid.Points.Min = 19;
                rebid.Description = "asking for more information";
                if (!IsMinimumSuitAdvance(advance) || rebid.declareBid.level > 3 || rebid.declareBid.level != rebid.LowestAvailableLevel(suit, true))
                    rebid.Validate = hand => false;
            }
            else if (suit == advance.declareBid.suit && suit != Suit.Unknown)
            {
                RaiseAdvance(advance, rebid);
            }
            else if (suit == Suit.Unknown)
            {
                NotrumpRebid(advance, rebid);
            }
            else if (!OpponentSuits(rebid).Contains(suit))
            {
                NewSuitRebid(advance, rebid);
            }
        }

        private static InterpretedBid FirstOpponentBid(InterpretedBid bid)
        {
            return bid.History.Take(bid.Index).FirstOrDefault(b => (bid.Index - b.Index) % 2 == 1 && b.bidIsDeclare);
        }

        private static int AdvanceAdjustment(InterpretedBid takeoutDouble)
        {
            return Bots.Overcall.IsBalancing(takeoutDouble) ? 2 : 0;
        }

        private static bool IsMinimumSuitAdvance(InterpretedBid advance)
        {
            return advance.declareBid.suit != Suit.Unknown && advance.BidConvention != BidConvention.Cuebid && !advance.IsPreemptive &&
                   advance.Points.Max <= 8 + AdvanceAdjustment(advance.History[advance.Index - 2]);
        }

        private static bool IsInvitationalSuitAdvance(InterpretedBid advance)
        {
            var adjust = AdvanceAdjustment(advance.History[advance.Index - 2]);
            return advance.declareBid.suit != Suit.Unknown && advance.BidConvention == BidConvention.None && advance.Points.Min == 9 + adjust &&
                   advance.Points.Max == 11 + adjust;
        }

        private static List<Suit> OpponentSuits(InterpretedBid rebid)
        {
            return rebid.History
                .Where(b => (rebid.Index - b.Index) % 2 == 1 && b.bidIsDeclare && b.declareBid.suit != Suit.Unknown)
                .Select(b => b.declareBid.suit).Distinct().ToList();
        }

        //  returns the HCP range (and description prefix) for a notrump rebid at this level, or null if not defined
        private static (int min, int max, string prefix)? NotrumpRange(InterpretedBid rebid, int level)
        {
            //  a direct 1NT overcall shows 15-18 HCP, so doubling then bidding notrump shows more
            //  Acol: cheapest NT 19-21, jump to 2NT 22-23, 3NT 24+ (or 22+ when 2NT was the cheapest)
            //  SAYC: cheapest 1NT 18-20 or 2NT 19-21, jump to 2NT 21-22, 3NT 23+ (or 22+ when 2NT was the cheapest)
            var isAcol = rebid.Options.bidding == BridgeBiddingScheme.Acol;
            var lowestAvailableLevel = rebid.LowestAvailableLevel(Suit.Unknown, true);
            var adjust = IsBalancingDouble(rebid) ? 3 : 0;
            if (level == lowestAvailableLevel && level <= 2)
            {
                var min = (isAcol || level == 2 ? 19 : 18) - adjust;
                return (min, min + 2, string.Empty);
            }

            if (level == lowestAvailableLevel + 1 && level == 2)
            {
                var min = (isAcol ? 22 : 21) - adjust;
                return (min, min + 1, "inviting game; ");
            }

            if (level == 3 && lowestAvailableLevel < 3)
                return ((lowestAvailableLevel == 2 ? 22 : isAcol ? 24 : 23) - adjust, 37, string.Empty);

            return null;
        }

        private static bool HasStoppers(Hand hand, IEnumerable<Suit> suits)
        {
            return suits.All(s => BasicBidding.HasStopper(hand, s));
        }

        private static void RaiseAdvance(InterpretedBid advance, InterpretedBid rebid)
        {
            var suit = rebid.declareBid.suit;
            var level = rebid.declareBid.level;

            if (IsInvitationalSuitAdvance(advance))
            {
                //  accept advancer's invitational jump with more than a minimum double
                if (level != rebid.GameLevel)
                    return;

                rebid.BidPointType = BidPointType.Dummy;
                rebid.Points.Min = 15;
                rebid.HandShape[suit].Min = 4;
                rebid.BidMessage = BidMessage.Signoff;
                rebid.Description = "Accept invitation";
                return;
            }

            if (!IsMinimumSuitAdvance(advance))
                return;

            //  raise with 4+ support: single raise 16-18 (through the 3-level), jump raise 19-21, game (beyond a jump) 22+
            var lowestAvailableLevel = rebid.LowestAvailableLevel(suit, true);
            if (level == lowestAvailableLevel && level <= 3)
            {
                rebid.Points.Min = 16;
                rebid.Points.Max = 18;
                rebid.Description = $"4+ {suit}; inviting game";
            }
            else if (level == lowestAvailableLevel + 1 && level <= rebid.GameLevel)
            {
                rebid.Points.Min = 19;
                rebid.Points.Max = 21;
                rebid.Description = level >= rebid.GameLevel ? $"4+ {suit}" : $"4+ {suit}; strongly inviting game";
            }
            else if (level == rebid.GameLevel && level > lowestAvailableLevel + 1)
            {
                rebid.Points.Min = 22;
                rebid.BidMessage = BidMessage.Signoff;
                rebid.Description = $"4+ {suit}";
            }
            else
            {
                return;
            }

            rebid.BidPointType = BidPointType.Hcp;
            rebid.HandShape[suit].Min = 4;

            //  prefer notrump over raising a minor when balanced with stoppers and strong enough
            var notrump = NotrumpRange(rebid, rebid.LowestAvailableLevel(Suit.Unknown, true));
            if (BridgeBot.IsMinor(suit) && notrump.HasValue)
            {
                var opponentSuits = OpponentSuits(rebid);
                rebid.Validate = hand => !(BasicBidding.IsBalanced(hand) && HasStoppers(hand, opponentSuits) &&
                                           BasicBidding.ComputeHighCardPoints(hand) >= notrump.Value.min);
            }
        }

        private static void NewSuitRebid(InterpretedBid advance, InterpretedBid rebid)
        {
            var suit = rebid.declareBid.suit;
            var level = rebid.declareBid.level;
            var lowestAvailableLevel = rebid.LowestAvailableLevel(suit);
            if (level == lowestAvailableLevel)
            {
                //  new suit at lowest available level shows 18-21 points and 5+ cards
                rebid.Points.Min = 18;
                rebid.Points.Max = 21;
                rebid.HandShape[suit].Min = 5;
                rebid.Description = $"5+ {suit}";
            }
            else if (level == lowestAvailableLevel + 1 && level <= 3)
            {
                //  jump in a new suit shows 22+ points and a good 6+ card suit
                rebid.Points.Min = 22;
                rebid.HandShape[suit].Min = 6;
                rebid.Description = $"Jump shift; 6+ {suit}";
            }
            else
            {
                return;
            }

            //  with 4+ card support for advancer's major, prefer raising it
            if (BridgeBot.IsMajor(advance.declareBid.suit))
                rebid.Validate = hand => BasicBidding.CountsBySuit(hand)[advance.declareBid.suit] < 4;
        }

        private static void NotrumpRebid(InterpretedBid advance, InterpretedBid rebid)
        {
            //  only define these over a minimum (0-8 point) suit advance
            if (!IsMinimumSuitAdvance(advance))
                return;

            var range = NotrumpRange(rebid, rebid.declareBid.level);
            if (!range.HasValue)
                return;

            rebid.Points.Min = range.Value.min;
            rebid.Points.Max = range.Value.max;
            if (rebid.declareBid.level == 3)
                rebid.BidMessage = BidMessage.Signoff;

            var opponentSuits = OpponentSuits(rebid);

            rebid.BidPointType = BidPointType.Hcp;
            rebid.IsBalanced = true;
            rebid.Description = $"{range.Value.prefix}stoppers in {string.Join(" and ", opponentSuits)}";
            if (BridgeBot.IsMajor(advance.declareBid.suit))
                //  with 4+ card support for advancer's major, prefer raising it
                rebid.HandShape[advance.declareBid.suit].Max = 3;
            rebid.Validate = hand => HasStoppers(hand, opponentSuits);
        }

        private static bool Response(InterpretedBid opening, InterpretedBid response)
        {
            if (response.bid == BridgeBid.Redouble)
            {
                //  a redouble shows 10+ points over a takeout double
                response.Points.Min = 10;
                response.BidPointType = BidPointType.Hcp;
                response.Description = "Good hand";
                return true;
            }

            if (!response.bidIsDeclare || !opening.bidIsDeclare || opening.declareBid.level != 1 || opening.declareBid.suit == Suit.Unknown)
                return false;

            var ourSuit = opening.declareBid.suit;
            var suit = response.declareBid.suit;
            var level = response.declareBid.level;
            var lowestAvailableLevel = response.LowestAvailableLevel(suit, true);

            if (suit == Suit.Unknown && level == 2 && BridgeBot.IsMajor(ourSuit))
            {
                //  1M-(X)-2N: Jordan 2NT shows a limit raise or better
                response.BidMessage = BidMessage.Forcing;
                response.BidPointType = BidPointType.Dummy;
                response.Points.Min = 10;
                response.HandShape[ourSuit].Min = 3;
                response.Priority = 90;
                response.Description = $"Jordan 2NT; limit raise or better in {ourSuit}";
                return true;
            }

            if (suit == ourSuit && BridgeBot.IsMajor(suit) && level == 3)
            {
                //  1M-(X)-3M: preemptive
                response.BidPointType = BidPointType.Dummy;
                response.Points.Max = 9;
                response.HandShape[suit].Min = 4;
                response.IsPreemptive = true;
                response.Description = $"4+ {suit}";
                return true;
            }

            if (suit == ourSuit && BridgeBot.IsMajor(suit) && level == 4)
            {
                //  1M-(X)-4M: preemptive with 5+ trumps
                response.BidPointType = BidPointType.Hcp;
                response.Points.Max = 9;
                response.HandShape[suit].Min = 5;
                response.IsPreemptive = true;
                response.Priority = 80;
                response.Description = $"5+ {suit}";
                return true;
            }

            var isAcol = response.Options.bidding == BridgeBiddingScheme.Acol;

            if (isAcol && suit != ourSuit && suit != Suit.Unknown && level == 2 && level == lowestAvailableLevel)
            {
                //  Acol 1X-(X)-2Y: natural and non-forcing; stronger hands redouble first
                response.BidPointType = BidPointType.Hcp;
                response.Points.Min = 6;
                response.Points.Max = 9;
                response.HandShape[suit].Min = 5;
                response.Description = $"5+ {suit}; non-forcing";
                if (BridgeBot.IsMajor(ourSuit))
                    response.Validate = hand => BasicBidding.CountsBySuit(hand)[ourSuit] < 8 - opening.HandShape[ourSuit].Min;
                return true;
            }

            if (suit != ourSuit && suit != Suit.Unknown && level == lowestAvailableLevel + 1 && level <= 3)
            {
                //  1X-(X)-jump shift: weak with a long suit (Acol: weaker than a non-forcing 2-level new suit)
                response.BidPointType = BidPointType.Hcp;
                response.Points.Max = isAcol ? 5 : 9;
                response.HandShape[suit].Min = 6;
                response.IsGood = true;
                response.IsPreemptive = true;
                response.Description = $"6+ {suit}";
                return true;
            }

            return false;
        }

        //  1M-(X)-2N: Jordan 2NT
        public static bool IsJordan(InterpretedBid bid)
        {
            if (!bid.bidIsDeclare || bid.declareBid.suit != Suit.Unknown || bid.declareBid.level != 2 || bid.BidPhase != BidPhase.Response ||
                bid.History[bid.Index - 1].BidConvention != BidConvention.TakeoutDouble)
                return false;

            var opening = bid.History[bid.Index - 2];
            return opening.bidIsDeclare && opening.declareBid.level == 1 && BridgeBot.IsMajor(opening.declareBid.suit);
        }
    }
}