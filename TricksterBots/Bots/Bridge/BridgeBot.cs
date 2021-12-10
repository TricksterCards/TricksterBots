using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public class BridgeBot : BaseBot<BridgeOptions>
    {
        internal static readonly Dictionary<Suit, int> suitRank = new Dictionary<Suit, int>
        {
            { Suit.Unknown, 5 },
            { Suit.Clubs, 1 },
            { Suit.Diamonds, 2 },
            { Suit.Hearts, 3 },
            { Suit.Spades, 4 }
        };

        private static readonly Dictionary<Suit, int> suitOrder = new Dictionary<Suit, int>
        {
            { Suit.Unknown, 0 },
            { Suit.Diamonds, 1 },
            { Suit.Clubs, 2 },
            { Suit.Hearts, 3 },
            { Suit.Spades, 4 }
        };

        public BridgeBot(BridgeOptions options, Suit trumpSuit) : base(options, trumpSuit)
        {
        }

        public override DeckType DeckType => DeckType.Std52Card;

        public static bool IsMajor(Suit s)
        {
            return s == Suit.Spades || s == Suit.Hearts;
        }

        public static bool IsMinor(Suit s)
        {
            return s == Suit.Clubs || s == Suit.Diamonds;
        }

        public override bool CanSeeHand(PlayersCollectionBase players, PlayerBase player, PlayerBase target)
        {
            return player.Seat == target.Seat || target.Bid == BidBase.Dummy && target.Hand.Length < 26 || player.Bid == BidBase.Dummy && target.Seat == players.PartnerOf(player).Seat;
        }

        public override BidBase SuggestBid(SuggestBidState<BridgeOptions> state)
        {
            var (players, dealerSeat, hand) = (new PlayersCollectionBase(this, state.players), state.dealerSeat, state.hand);
            var history = new BridgeBidHistory(players, dealerSeat);
            var interpretedHistory = InterpretedBid.InterpretHistory(history);
            var legalBids = AllPossibleBids().Where(history.IsBidLegal).ToList();

            foreach (var legalBid in legalBids)
                legalBid.why = new InterpretedBid(legalBid.value, interpretedHistory, interpretedHistory.Count);

            //  get and sort all possible suggestions
            var suggestions = legalBids.Where(b => b.why.Match(hand));
            if (legalBids[0].why.BidPhase == BidPhase.Opening)
                //  when opening, prefer suggestions with higher minimum points first, then higher minimum cards
            {
                suggestions = suggestions
                    .OrderByDescending(s => s.why.Points.Min)
                    .ThenByDescending(s => s.why.HandShape.Max(hs => hs.Value.Min));
            }
            else
                //  in other phases, prefer finding the best fit first (prioritizing majors), then higher minimum points
            {
                suggestions = suggestions
                    .OrderBy(s => s.why.HandShape.Any(hs => IsMajor(hs.Key) && hs.Value.Min > 3) ? 0 : 1)
                    .ThenByDescending(s => s.why.HandShape.Max(hs => hs.Value.Min))
                    .ThenByDescending(s => s.why.Points.Min);
            }

            //  then favor the most "descriptive" one
            var bid = suggestions.FirstOrDefault() ?? FindBestFit(legalBids, interpretedHistory) ?? legalBids.First(b => b.value == BidBase.Pass);

            return bid;
        }

        public override List<Card> SuggestDiscard(SuggestDiscardState<BridgeOptions> state)
        {
            throw new NotImplementedException();
        }

        public override Card SuggestNextCard(SuggestCardState<BridgeOptions> state)
        {
            var (players, trick, legalCards, cardsPlayed, player, isPartnerTakingTrick, cardTakingTrick) = (new PlayersCollectionBase(this, state.players), state.trick, state.legalCards, state.cardsPlayed,
                state.player, state.isPartnerTakingTrick, state.cardTakingTrick);

            return TryTakeEm(player, players.PartnerOf(player), trick, legalCards, cardsPlayed, players, isPartnerTakingTrick, cardTakingTrick);
        }

        public override List<Card> SuggestPass(SuggestPassState<BridgeOptions> state)
        {
            throw new NotImplementedException();
        }

        public override int SuitOrder(Suit s)
        {
            return suitOrder[s];
        }

        private static List<BidWhy> AllPossibleBids()
        {
            var bids = new List<BidBase>();
            var sortedSuits = suitRank.OrderBy(sr => sr.Value).Select(sr => sr.Key).ToList();

            for (var i = 1; i <= 7; ++i) bids.AddRange(sortedSuits.Select(suit => new BidBase((int)new DeclareBid(i, suit))));

            bids.Add(new BidBase(BridgeBid.Double));
            bids.Add(new BidBase(BridgeBid.Redouble));
            bids.Add(new BidBase(BidBase.Pass));

            return bids.Select(bb => new BidWhy(bb)).ToList();
        }

        private static BidWhy FindBestFit(IEnumerable<BidWhy> legalBids, IReadOnlyList<InterpretedBid> history)
        {
            //  if we're not responding to a forcing bid or there was interference, we can just pass
            var nHistory = history.Count;
            if (nHistory < 2 || history[nHistory - 1].bid != BidBase.Pass || history[nHistory - 2].BidMessage != BidMessage.Forcing)
                return legalBids.First(b => b.value == BidBase.Pass);

            //  TODO: check if any bids that didn't fit were "close enough"

            //  otherwise we should bid the best fit between our hand and partner's at the lowest available level
            var summary = new InterpretedBid.TeamSummary(history, history.Count - 2);
            var suit = summary.HandShape.Where(hs => hs.Value.Min >= 8).Select(hs => hs.Key).FirstOrDefault();

            //  if we don't have a fit, go back to partner's best suit at the lowest available level
            if (suit == Suit.Unknown)
                suit = summary.p1.HandShape.Where(hs => hs.Value.Min > 0).OrderByDescending(hs => hs.Value.Min).Select(hs => hs.Key).FirstOrDefault();

            //  if we don't know partner's suit, bid our best suit at the lowest available level
            if (suit == Suit.Unknown)
                suit = summary.p2.HandShape.Where(hs => hs.Value.Min > 0).OrderByDescending(hs => hs.Value.Min).Select(hs => hs.Key).FirstOrDefault();

            return legalBids.FirstOrDefault(b => b.why.bidIsDeclare && b.why.declareBid.suit == suit);
        }

        private Card LowestCardFromWeakestSuit(IReadOnlyList<Card> legalCards, IReadOnlyList<Card> teamCards)
        {
            var nonTrumpCards = legalCards.Where(c => !IsTrump(c)).ToList();

            //  if we only have trump, dump the lowest trump
            if (nonTrumpCards.Count == 0)
                return legalCards.OrderBy(RankSort).First();

            var suitCounts = nonTrumpCards.GroupBy(EffectiveSuit).Select(g => new { suit = g.Key, count = g.Count() }).ToList();

            //  try to ditch a singleton that's not "boss" and whose suit has the most outstanding cards
            var bestSingletonSuitCount = suitCounts.Where(sc => sc.count == 1)
                .Where(sc => !IsCardHigh(nonTrumpCards.Single(c => EffectiveSuit(c) == sc.suit), teamCards))
                .OrderBy(sc => teamCards.Count(c => EffectiveSuit(c) == sc.suit)).FirstOrDefault();

            if (bestSingletonSuitCount != null)
                return nonTrumpCards.Single(c => EffectiveSuit(c) == bestSingletonSuitCount.suit);

            //  now we look at doubletons in the order of the number of remaining cards in the suit
            var doubletonSuitCounts = suitCounts.Where(sc => sc.count == 2).OrderBy(sc => teamCards.Count(c => EffectiveSuit(c) == sc.suit)).ToArray();

            foreach (var sc in doubletonSuitCounts)
            {
                var cards = nonTrumpCards.Where(c => EffectiveSuit(c) == sc.suit).OrderBy(RankSort).ToArray();

                if (IsCardHigh(cards[1], teamCards) && cards[0].rank < cards[1].rank - 1)
                    //  okay to ditch from this doubleton where the high card is "boss" and the card below it isn't adjacent
                    return cards[0];

                if (cards.All(c => c.rank != Rank.King))
                    //  okay to ditch from this doubleton that doesn't contain a king we might be able to make "boss"
                    //  TODO: don't ditch other cards that could be made boss (e.g. Queen when Ace/King has been played)
                    return cards[0];
            }

            //  return the lowest card from the longest non-trump suit
            return nonTrumpCards.OrderByDescending(c => nonTrumpCards.Count(c1 => EffectiveSuit(c1) == c.suit)).ThenBy(RankSort).First();
        }

        //  we're trying to take a trick
        private Card TryTakeEm(PlayerBase player, PlayerBase partner, IReadOnlyList<Card> trick, IReadOnlyList<Card> legalCards,
            IReadOnlyList<Card> cardsPlayed, PlayersCollectionBase players, bool isPartnerTakingTrick, Card cardTakingTrick)
        {
            var lho = players.Lho(player);
            var rho = players.Rho(player);

            var isLhoVoidInSuit = new Dictionary<Suit, bool> { { Suit.Unknown, true } };
            var isRhoVoidInSuit = new Dictionary<Suit, bool> { { Suit.Unknown, true } };
            var isPartnerVoidInSuit = new Dictionary<Suit, bool> { { Suit.Unknown, true } };

            var trickSuit = trick.Count > 0 ? EffectiveSuit(trick[0]) : Suit.Unknown;

            foreach (var suit in SuitRank.stdSuits)
            {
                var card = new Card(suit, Rank.Ace);
                isLhoVoidInSuit[suit] = players.LhoIsVoidInSuit(player, card, cardsPlayed);
                isRhoVoidInSuit[suit] = players.RhoIsVoidInSuit(player, card, cardsPlayed);
                isPartnerVoidInSuit[suit] = players.PartnerIsVoidInSuit(player, card, cardsPlayed);
            }

            var teamCards = cardsPlayed.Concat(new Hand(player.Hand)).ToList();
            if (CanSeeHand(players, player, partner)) teamCards = teamCards.Concat(new Hand(partner.Hand)).ToList();

            var knownCards = teamCards;
            if (CanSeeHand(players, player, rho)) knownCards = knownCards.Concat(new Hand(rho.Hand)).ToList();

            if (trick.Count == 0)
            {
                //  we're leading

                if (trump != Suit.Unknown && !isRhoVoidInSuit[trump])
                {
                    //  RHO may still have trump: avoid suits where RHO is known to be void
                    var avoidSuits = SuitRank.stdSuits.Where(s => isRhoVoidInSuit[s]).ToList();
                    var preferredLegalCards = legalCards.Where(c => !avoidSuits.Contains(EffectiveSuit(c))).ToList();
                    if (preferredLegalCards.Count > 0)
                        legalCards = preferredLegalCards;
                }

                if (trump != Suit.Unknown && !isLhoVoidInSuit[trump])
                {
                    //  LHO may still have trump: avoid suits where LHO is known to be void unless partner is also void and may have trump
                    var avoidSuits = SuitRank.stdSuits.Where(s => isLhoVoidInSuit[s] && (isPartnerVoidInSuit[trump] || !isPartnerVoidInSuit[s])).ToList();
                    var preferredLegalCards = legalCards.Where(c => !avoidSuits.Contains(EffectiveSuit(c))).ToList();
                    if (preferredLegalCards.Count > 0)
                        legalCards = preferredLegalCards;
                }

                var cards = legalCards;

                if (!isLhoVoidInSuit[trump] || !isRhoVoidInSuit[trump])
                {
                    //  if opponents are not void in trump
                    var teamTrump = new Hand(player.Hand).Count(IsTrump);
                    if (CanSeeHand(players, player, partner)) teamTrump += new Hand(partner.Hand).Count(IsTrump);

                    //  and we hold over half the remaining trump
                    var playedTrump = cardsPlayed.Count(c => EffectiveSuit(c) == trump);
                    if (teamTrump * 2 > 13 - playedTrump)
                    {
                        //  prefer leading trump
                        var preferredLegalCards = legalCards.Where(IsTrump).ToList();
                        if (preferredLegalCards.Count > 0)
                            legalCards = preferredLegalCards;
                    }
                    else
                    {
                        //  otherwise avoid leading trump
                        var preferredLegalCards = legalCards.Where(c => !IsTrump(c)).ToList();
                        if (preferredLegalCards.Count > 0)
                            legalCards = preferredLegalCards;
                    }
                }
                else
                {
                    //  but avoid leading trump once opponents are void
                    var preferredLegalCards = legalCards.Where(c => !IsTrump(c)).ToList();
                    if (preferredLegalCards.Count > 0)
                        legalCards = preferredLegalCards;
                }

                if (CanSeeHand(players, player, partner))
                {
                    //  we can see our partner's hand

                    //  check if partner has any boss cards
                    var partnerBossCards = new Hand(partner.Hand).Where(c => IsCardHigh(c, cardsPlayed.Concat(new Hand(player.Hand))))
                        .OrderByDescending(c => cards.Count(c1 => EffectiveSuit(c1) == EffectiveSuit(c))).ToList();
                    if (partnerBossCards.Count > 0)
                    {
                        //  our partner has boss; lead low in their suit if we can
                        //  TODO: prefer suits where opponents with trump are not known to be void
                        var leads = legalCards.Where(c1 => partnerBossCards.Any(c2 => EffectiveSuit(c1) == EffectiveSuit(c2))).OrderBy(RankSort).ToList();
                        if (leads.Count > 0)
                            return leads.First();
                    }
                }

                //  consider leading our "boss" cards favoring boss in our longest suit
                var bossCards = legalCards.Where(c => IsCardHigh(c, cardsPlayed))
                    .OrderByDescending(c => cards.Count(c1 => EffectiveSuit(c1) == EffectiveSuit(c))).ToList();
                if (bossCards.Count > 0)
                    return bossCards.First();

                //  avoid suits where RHO has boss (unless partner is void and has trump)
                if (CanSeeHand(players, player, rho))
                {
                    var rhoBossCards = new Hand(rho.Hand).Where(c => IsCardHigh(c, cardsPlayed))
                        .OrderByDescending(c => cards.Count(c1 => EffectiveSuit(c1) == EffectiveSuit(c))).ToList();
                    if (rhoBossCards.Count > 0)
                    {
                        var avoidSuits = SuitRank.stdSuits.Where(s =>
                            (!isPartnerVoidInSuit[s] || isPartnerVoidInSuit[trump]) && rhoBossCards.Any(c => EffectiveSuit(c) == s));
                        var preferredLegalCards = legalCards.Where(c => !avoidSuits.Contains(EffectiveSuit(c))).ToList();
                        if (preferredLegalCards.Count > 0)
                            legalCards = preferredLegalCards;
                    }
                }

                //  avoid suits where LHO has boss (unless partner is void, has trump, and RHO is not void or has no trump)
                if (CanSeeHand(players, player, lho))
                {
                    var lhoBossCards = new Hand(lho.Hand).Where(c => IsCardHigh(c, cardsPlayed))
                        .OrderByDescending(c => cards.Count(c1 => EffectiveSuit(c1) == EffectiveSuit(c))).ToList();
                    if (lhoBossCards.Count > 0)
                    {
                        var avoidSuits = SuitRank.stdSuits.Where(s =>
                            (!isPartnerVoidInSuit[s] || isPartnerVoidInSuit[trump] || isRhoVoidInSuit[s] && !isRhoVoidInSuit[trump]) &&
                            lhoBossCards.Any(c => EffectiveSuit(c) == s));
                        var preferredLegalCards = legalCards.Where(c => !avoidSuits.Contains(EffectiveSuit(c))).ToList();
                        if (preferredLegalCards.Count > 0)
                            legalCards = preferredLegalCards;
                    }
                }

                if (trump != Suit.Unknown && !isPartnerVoidInSuit[trump])
                {
                    //  partner may have trump: prefer suits where partner is known to be void
                    var preferSuits = SuitRank.stdSuits.Where(s => isPartnerVoidInSuit[s]).ToList();
                    var preferredLegalCards = legalCards.Where(c => preferSuits.Contains(EffectiveSuit(c))).ToList();
                    if (preferredLegalCards.Count > 0)
                        legalCards = preferredLegalCards;
                }
            }
            else if (EffectiveSuit(legalCards[0]) == trickSuit)
            {
                //  we can follow suit

                //  if the trick is not trump, but someone has already trumped in - play low
                if (!IsTrump(trick[0]) && trick.Any(IsTrump))
                    return LowestCardFromWeakestSuit(legalCards, teamCards);

                //  if partner hasn't played, but has "boss" - play low
                if (IsPartnership && trick.Count == 1 && CanSeeHand(players, player, partner))
                {
                    var partnersHighCard = new Hand(partner.Hand).Where(c => EffectiveSuit(c) == trickSuit).OrderBy(RankSort).LastOrDefault();
                    if (partnersHighCard != null && IsCardHigh(partnersHighCard, cardsPlayed.Concat(legalCards)))
                        return LowestCardFromWeakestSuit(legalCards, teamCards);
                }

                //  if we can't beat the best card in the trick - play low
                var highestInTrick = trick.Where(c => EffectiveSuit(c) == trickSuit).Max(RankSort);
                var minimumWinner = legalCards.Where(c => RankSort(c) > highestInTrick).OrderBy(RankSort).FirstOrDefault();
                if (minimumWinner == null)
                    return LowestCardFromWeakestSuit(legalCards, teamCards);

                if (isPartnerTakingTrick)
                {
                    //  partner is taking trick

                    //  if we're last to play - play low
                    if (trick.Count == players.Count - 1)
                        return LowestCardFromWeakestSuit(legalCards, teamCards);

                    //  if LHO is void - play low
                    if (isLhoVoidInSuit[trickSuit])
                        return LowestCardFromWeakestSuit(legalCards, teamCards);

                    //  if we can see LHO and LHO can't beat partner's card - play low
                    if (CanSeeHand(players, player, lho) && new Hand(lho.Hand).All(c => EffectiveSuit(c) == trickSuit && RankSort(c) < RankSort(cardTakingTrick)))
                        return LowestCardFromWeakestSuit(legalCards, teamCards);

                    //  if we can deduce that LHO can't beat partner's card - play low
                    if (IsCardHigh(cardTakingTrick, knownCards))
                        return LowestCardFromWeakestSuit(legalCards, teamCards);
                }
                else
                {
                    //  partner is not taking trick

                    //  if we're last to play - play just high enough to win
                    if (trick.Count == players.Count - 1)
                        return minimumWinner;

                    //  if LHO is void - play just high enough to win
                    if (isLhoVoidInSuit[trickSuit])
                        return minimumWinner;
                }

                //  partner needs help

                //  if we can beat LHO's best card (or best in trick if lho is void) - play just high enough to win
                if (CanSeeHand(players, player, lho))
                {
                    var lhoHighRank = new Hand(lho.Hand).Where(c => EffectiveSuit(c) == trickSuit).Max(RankSort);
                    var targetRank = isLhoVoidInSuit[trickSuit] ? highestInTrick : Math.Max(lhoHighRank, highestInTrick);
                    minimumWinner = legalCards.Where(c => RankSort(c) > targetRank).OrderBy(RankSort).FirstOrDefault();
                    if (minimumWinner != null)
                        return minimumWinner;
                }

                //  if we have any "boss" cards (accounting for partner/RHO if visible) - play our lowest one
                var lowestBossCard = legalCards.Where(c => IsCardHigh(c, knownCards)).OrderBy(RankSort).FirstOrDefault();
                if (lowestBossCard != null)
                    return lowestBossCard;

                //  TODO: check if we can force LHO to play a high card by playing a medium-rank card
            }
            else if (legalCards.Any(IsTrump))
            {
                //  we can't follow suit but we have trump

                if (isPartnerTakingTrick)
                {
                    //  partner is taking trick

                    //  if we're last to play - play low
                    if (trick.Count == players.Count - 1)
                        return LowestCardFromWeakestSuit(legalCards, teamCards);

                    //  if partner's card is trump - play low
                    if (IsTrump(cardTakingTrick))
                        return LowestCardFromWeakestSuit(legalCards, teamCards);

                    //  if LHO is void - play low
                    if (isLhoVoidInSuit[trickSuit])
                        return LowestCardFromWeakestSuit(legalCards, teamCards);

                    //  if we can see LHO and LHO can't beat partner's card - play low
                    if (CanSeeHand(players, player, lho) &&
                        new Hand(lho.Hand).Any(c => EffectiveSuit(c) == trickSuit && RankSort(c) > RankSort(cardTakingTrick)))
                        return LowestCardFromWeakestSuit(legalCards, teamCards);

                    //  if we can deduce that LHO can't beat partner's card - play low
                    if (IsCardHigh(cardTakingTrick, knownCards))
                        return LowestCardFromWeakestSuit(legalCards, teamCards);
                }

                //  if partner hasn't played, is void, and may have trump - play low
                if (IsPartnership && trick.Count == 1 && isPartnerVoidInSuit[trickSuit] && !isPartnerVoidInSuit[trump])
                    return LowestCardFromWeakestSuit(legalCards, teamCards);

                //  if the trick doesn't contain any trump - play our lowest trump
                if (trick.All(c => !IsTrump(c)))
                    return legalCards.Where(IsTrump).OrderBy(RankSort).First();

                //  if we can beat the best trump in the trick - play just high enough to win
                var highestTrumpInTrick = trick.Where(IsTrump).Max(RankSort);
                var minimumTrump = legalCards.Where(c => IsTrump(c) && RankSort(c) > highestTrumpInTrick).OrderBy(RankSort).FirstOrDefault();
                if (minimumTrump != null)
                    return minimumTrump;
            }

            //  else, dump the lowest card from the weakest suit
            return LowestCardFromWeakestSuit(legalCards, teamCards);
        }
    }
}