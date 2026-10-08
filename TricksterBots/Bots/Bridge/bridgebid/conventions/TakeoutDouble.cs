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
            }

            return false;
        }

        private static void Advance(InterpretedBid advance)
        {
            if (!advance.bidIsDeclare)
                return;

            var opening = advance.History.First(b => b.bid != BidBase.Pass);
            var lowestAvailableLevel = advance.LowestAvailableLevel(advance.declareBid.suit);

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
                        advance.Points.Min = 6;
                        advance.Points.Max = 10;
                    }
                    else if (advance.declareBid.level == lowestAvailableLevel + 1)
                    {
                        //  11-12 points: bid notrump, jumping a level
                        advance.Points.Min = 11;
                        advance.Points.Max = 12;
                    }
                    else if (advance.declareBid.level == 3)
                    {
                        //  13+ points: bid game in notrump
                        advance.Points.Min = 13;
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

                if (advance.declareBid.suit == opening.declareBid.suit && advance.declareBid.suit != Suit.Unknown)
                {
                    //  cuebid
                    advance.BidConvention = BidConvention.Cuebid;
                    advance.BidMessage = BidMessage.Forcing;
                    advance.Points.Min = 10;
                    advance.Description = "asking for more information";
                    advance.Validate = hand => false;
                }
                else if (!bidSuits.Contains(advance.declareBid.suit))
                {
                    if (advance.declareBid.level == lowestAvailableLevel)
                    {
                        //  0-8 points: bid at the cheapest level (but don't cap points if we're at the game level)
                        if (lowestAvailableLevel < gameLevel - 1)
                            advance.Points.Max = 8;

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
                        advance.Points.Min = 9;
                        advance.Points.Max = 11;
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
                        advance.Points.Min = 12;
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

            //  handle takeout doubles worth 13+ dummy points with 0-2 cards in the opponents' suit(s)
            overcall.Points.Min = 13;
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
            bool prefersNaturalOvercall(Hand hand)
            {
                var counts = BasicBidding.CountsBySuit(hand);
                if (unbidMajors.Count == 0 || unbidMajors.Any(s => counts[s] >= 4))
                    return false;

                return unbidSuits.Any(s => counts[s] >= 5 && new InterpretedBid(
                    new DeclareBid(overcall.LowestAvailableLevel(s, true), s), overcall.History, overcall.Index, overcall.Options).Match(hand));
            }

            //  if we can overcall 1NT (balanced; 15-18 HCP; stopper in the opponents' suits) we'll defer to that instead
            bool prefersNotrumpOvercall(Hand hand)
            {
                var hcp = BasicBidding.ComputeHighCardPoints(hand);
                return 15 <= hcp && hcp <= 18 && overcall.LowestAvailableLevel(Suit.Unknown, true) == 1 && BasicBidding.IsBalanced(hand) &&
                       HasStoppers(hand, bidSuits);
            }

            overcall.Validate = hand => !prefersSuitOvercall(hand) && !prefersNotrumpOvercall(hand) && !prefersNaturalOvercall(hand);
            overcall.AlternateMatches = hand =>
            {
                if (prefersSuitOvercall(hand) || prefersNotrumpOvercall(hand))
                    return false;

                //  otherwise a takeout double can also show 18+ points (too strong for a simple overcall)
                var points = BasicBidding.ComputeHighCardPoints(hand) + BasicBidding.ComputeDistributionPoints(hand);
                return 18 <= points;
            };

            return true;
        }

        private static void Rebid(InterpretedBid advance, InterpretedBid rebid)
        {
            if (!rebid.bidIsDeclare)
                return;

            var opening = rebid.History.First(b => b.bid != BidBase.Pass);
            var suit = rebid.declareBid.suit;

            if (suit == opening.declareBid.suit && suit != Suit.Unknown)
            {
                //  cuebid: 19+ points without a better descriptive bid (other bids describing shape are preferred when sorting)
                rebid.BidConvention = BidConvention.Cuebid;
                rebid.BidMessage = BidMessage.Forcing;
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

        private static bool IsMinimumSuitAdvance(InterpretedBid advance)
        {
            return advance.declareBid.suit != Suit.Unknown && advance.BidConvention != BidConvention.Cuebid && !advance.IsPreemptive && advance.Points.Max <= 8;
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
            if (level == lowestAvailableLevel && level <= 2)
            {
                var min = isAcol || level == 2 ? 19 : 18;
                return (min, min + 2, string.Empty);
            }

            if (level == lowestAvailableLevel + 1 && level == 2)
            {
                var min = isAcol ? 22 : 21;
                return (min, min + 1, "inviting game; ");
            }

            if (level == 3 && lowestAvailableLevel < 3)
                return (lowestAvailableLevel == 2 ? 22 : isAcol ? 24 : 23, 37, string.Empty);

            return null;
        }

        private static bool HasStoppers(Hand hand, IEnumerable<Suit> suits)
        {
            return suits.All(s => BasicBidding.HasStopper(hand, s));
        }

        private static void RaiseAdvance(InterpretedBid advance, InterpretedBid rebid)
        {
            if (!IsMinimumSuitAdvance(advance))
                return;

            //  raise with 4+ support: single raise 16-18 (through the 3-level), jump raise 19-21, game (beyond a jump) 22+
            var suit = rebid.declareBid.suit;
            var level = rebid.declareBid.level;
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
            //  TODO: handle other changes to response meanings due to the takeout double overcall
            if (response.bid != BridgeBid.Redouble)
                return false;

            //  a redouble shows 10+ points over a takeout double
            response.Points.Min = 10;
            response.BidPointType = BidPointType.Hcp;
            response.Description = "Good hand";

            return true;
        }
    }
}