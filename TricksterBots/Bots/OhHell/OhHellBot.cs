using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public class OhHellBot : BaseBot<OhHellOptions>
    {
        public OhHellBot(OhHellOptions options, Suit trumpSuit) : base(options, trumpSuit)
        {
        }

        public override DeckType DeckType => DeckType.Std52Card;

        public override BidBase SuggestBid(SuggestBidState<OhHellOptions> state)
        {
            var hand = state.hand;

            var est = 0.0;

            var nCards = hand.Count;
            var cardsOfTrump = hand.Where(IsTrump).OrderByDescending(RankSort).ToList();
            var offSuitCards = hand.Where(c => !IsTrump(c)).ToList();

            //  process voids, singletons, and doubletons
            double nTrump = cardsOfTrump.Count;
            foreach (var suitCount in SuitRank.stdSuits.Select(s => new { suit = s, count = offSuitCards.Count(c => EffectiveSuit(c) == s) }))
            {
                var maxLengthInSuit = 2 + (options.players == 3 ? 1 : 0);
                if (suitCount.suit != trump && suitCount.count < maxLengthInSuit)
                {
                    //  count 2 for each void, 1 for each singleton
                    var use = maxLengthInSuit - suitCount.count;
                    var trumpIn = Math.Min(nTrump, use);
                    nTrump -= trumpIn;
                    est += trumpIn;
                }
            }

            //  keep the highest cards in trump not used to trump in (this assumes we get away with trumping-in low trump)
            cardsOfTrump = cardsOfTrump.Take((int)Math.Round(nTrump)).ToList();

            foreach (var card in cardsOfTrump)
            {
                var nCardsAbove = cardsOfTrump.Count(c => RankSort(c) > RankSort(card));
                var nCardsBelow = cardsOfTrump.Count(c => RankSort(c) < RankSort(card));
                var nRanksAbove = HighRankInSuit(card) - RankSort(card);
                var nGapsAbove = nRanksAbove - nCardsAbove;

                if (nCardsBelow >= nGapsAbove)
                    est += 1;
                else if (nCardsBelow == nGapsAbove - 1)
                    est += 0.75;
                else if (nCardsBelow == nGapsAbove - 2)
                    est += 0.25;
            }

            var offSuitAces = offSuitCards.Where(c => c.rank == Rank.Ace).ToList();
            foreach (var nCardsOfAceSuit in offSuitAces.Select(ace => offSuitCards.Count(c => EffectiveSuit(c) == ace.suit)))
            {
                var maxLengthInSuit = nCards / 2 + (options.players == 3 ? 1 : 0);
                if (nCardsOfAceSuit <= maxLengthInSuit)
                    est += 1;
                else
                    est += 0.5;
            }

            //  look at off-suit kings where we're not already counting the ace
            var offSuitKings = offSuitCards.Where(c => c.rank == Rank.King && offSuitAces.All(ace => ace.suit != c.suit));
            foreach (var nCardsOfKingSuit in offSuitKings.Select(king => offSuitCards.Count(c => EffectiveSuit(c) == king.suit)))
            {
                if (nCardsOfKingSuit > 1)
                {
                    var maxLengthInSuit = nCards / 2 + (options.players == 3 ? 1 : 0);
                    if (nCardsOfKingSuit <= maxLengthInSuit - 1)
                        est += 1;
                    else
                        est += 0.25;
                }
            }

            var target = (int)Math.Floor(est);
            var legalBids = state.legalBids.Where(b => b.value != BidBase.NoBid).Select(b => new OhHellBid(b)).ToList();
            return legalBids.FirstOrDefault(b => b.Tricks == target) ?? legalBids.FirstOrDefault(b => b.Tricks == target - 1) ?? legalBids.First();
        }

        public override List<Card> SuggestDiscard(SuggestDiscardState<OhHellOptions> state)
        {
            throw new NotImplementedException();
        }

        public override Card SuggestNextCard(SuggestCardState<OhHellOptions> state)
        {
            var (players, trick, legalCards, cardsPlayed, player, isPartnerTakingTrick, cardTakingTrick) = (new PlayersCollectionBase(this, state.players), state.trick, state.legalCards, state.cardsPlayed,
                state.player, state.isPartnerTakingTrick, state.cardTakingTrick);


            if (new OhHellBid(player.Bid).Tricks == player.HandScore)
                return TryDumpEm(trick, legalCards, players.Count);

            return TryTakeEm(player, trick, legalCards, cardsPlayed, players, isPartnerTakingTrick, cardTakingTrick, false);
        }

        public override List<Card> SuggestPass(SuggestPassState<OhHellOptions> state)
        {
            throw new NotImplementedException();
        }

        private Card LowestCardFromWeakestSuit(IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed)
        {
            var nonTrumpCards = legalCards.Where(c => !IsTrump(c)).ToList();

            //  if we only have trump, dump the lowest trump
            if (nonTrumpCards.Count == 0)
                return legalCards.OrderBy(RankSort).First();

            var suitCounts = nonTrumpCards.GroupBy(EffectiveSuit).Select(g => new { suit = g.Key, count = g.Count() }).ToList();

            //  try to ditch a singleton that's not "boss" and whose suit has the most outstanding cards
            var bestSingletonSuitCount = suitCounts.Where(sc => sc.count == 1).Where(sc => !IsCardHigh(nonTrumpCards.Single(c => EffectiveSuit(c) == sc.suit), cardsPlayed))
                .OrderBy(sc => cardsPlayed.Count(c => EffectiveSuit(c) == sc.suit)).FirstOrDefault();

            if (bestSingletonSuitCount != null)
                return nonTrumpCards.Single(c => EffectiveSuit(c) == bestSingletonSuitCount.suit);

            //  now we look at doubletons in the order of the number of remaining cards in the suit
            var doubletonSuitCounts = suitCounts.Where(sc => sc.count == 2).OrderBy(sc => cardsPlayed.Count(c => EffectiveSuit(c) == sc.suit)).ToList();

            foreach (var sc in doubletonSuitCounts)
            {
                var cards = nonTrumpCards.Where(c => EffectiveSuit(c) == sc.suit).OrderBy(RankSort).ToList();

                //  TODO: Use RankSort
                if (IsCardHigh(cards[1], cardsPlayed) && cards[0].rank < cards[1].rank - 1)
                {
                    //  okay to ditch from this doubleton where the high card is "boss" and the card below it isn't adjacent
                    return cards[0];
                }

                //  TODO: fix assumption about the king. Also, use RankSort
                if (cards.All(c => c.rank != Rank.King))
                {
                    //  okay to ditch from this doubleton that doesn't contain a king we might be able to make "boss"
                    return cards[0];
                }
            }

            //  return the lowest card from the longest non-trump suit
            return nonTrumpCards.OrderByDescending(c => nonTrumpCards.Count(c1 => EffectiveSuit(c1) == c.suit)).ThenBy(RankSort).First();
        }

        private Card TryDumpEm(IReadOnlyList<Card> trick, IReadOnlyList<Card> legalCards, int nPlayers, bool takeWithHigh = false)
        {
            Card suggestion;

            var firstCardInTrick = trick.FirstOrDefault(IsOfValue);

            if (firstCardInTrick == null)
            {
                //  lead the absolute lowest card we can
                suggestion = legalCards.OrderBy(RankSort).First();
            }
            else
            {
                var trickSuit = EffectiveSuit(firstCardInTrick);

                if (EffectiveSuit(legalCards[0]) == trickSuit)
                {
                    //  we can follow suit: dump the highest card that won't take the trick
                    if (trick.Any(IsTrump) && !IsTrump(firstCardInTrick))
                    {
                        //  if the lead suit was not trump and there is trump in the trick, just dump our highest card
                        suggestion = legalCards.OrderByDescending(RankSort).First();
                    }
                    else
                    {
                        //  otherwise dump the highest card below the card that is currently taking the trick 
                        var trickTakerRank = trick.Where(c => EffectiveSuit(c) == trickSuit).Max(RankSort);
                        suggestion = legalCards.Where(c => RankSort(c) < trickTakerRank).OrderByDescending(RankSort).FirstOrDefault();

                        //  else if we can't get below the highest card and we're playing last, just play our highest card
                        if (suggestion == null && (takeWithHigh || trick.Count == nPlayers - 1))
                            suggestion = legalCards.OrderByDescending(RankSort).First();
                    }
                }
                else if (trick.Any(IsTrump))
                {
                    //  we can't follow suit but the trick contains trump: dump the highest trump that won't take the trick or the highest non-trump we have
                    var maxTrumpInTrick = trick.Where(IsTrump).Max(RankSort);
                    suggestion = legalCards.Where(c => IsTrump(c) && RankSort(c) < maxTrumpInTrick).OrderByDescending(RankSort).FirstOrDefault()
                                 ?? legalCards.Where(c => !IsTrump(c)).OrderByDescending(RankSort).FirstOrDefault();
                }
                else
                {
                    //  we can't follow suit and the trick does not contain trump: dump the highest non-trump we have or the highest trump if that's all we have left
                    suggestion = legalCards.Where(c => !IsTrump(c)).OrderByDescending(RankSort).FirstOrDefault() ?? legalCards.OrderByDescending(RankSort).FirstOrDefault();
                }
            }

            //  return either the suggestion, the lowest non-trump we can, or the lowest trump we can
            return suggestion ?? legalCards.Where(c => !IsTrump(c)).OrderBy(RankSort).FirstOrDefault() ?? legalCards.OrderBy(RankSort).First();
        }

        private Card TrySignalGoodSuit(PlayerBase player, IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed, bool isDefending)
        {
            //  don't signal when defending or playing an individual game
            if (isDefending || !IsPartnership)
                return null;

            //  if we've already sloughed when showing a void suit, then we've already signaled and won't do so again
            if (player.VoidSuits.Any() && (trump == Suit.Unknown || player.GoodSuit != trump))
                return null;

            //  no need to signal if all we have left is trump
            if (legalCards.All(IsTrump))
                return null;

            //  otherwise try to signal a suit where we hold boss and at least one other card, or where we hold a singleton and at least one trump
            var hasTrump = legalCards.Any(IsTrump);
            var suitRange = legalCards.Where(c => !IsTrump(c)).OrderBy(RankSort).GroupBy(EffectiveSuit).Select(g => new { suit = g.Key, low = g.First(), high = g.Last() }).ToList();
            var signalableSuit = suitRange.FirstOrDefault(sr => sr.high != sr.low && IsCardHigh(sr.high, cardsPlayed))
                                 ?? suitRange.FirstOrDefault(sr => sr.low == sr.high && hasTrump);

            return signalableSuit?.low;
        }

        //  we're trying to take a trick
        private Card TryTakeEm(PlayerBase player, IReadOnlyList<Card> trick, IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed, PlayersCollectionBase players, bool isPartnerTakingTrick,
            Card cardTakingTrick, bool isDefending)
        {
            Card suggestion = null;

            var trickCount = trick.Count(IsOfValue);
            var firstCardInTrick = trick.FirstOrDefault(IsOfValue);

            if (firstCardInTrick == null)
            {
                //  we're leading

                var isLhoVoidInSuit = new Dictionary<Suit, bool>();
                var isRhoVoidInSuit = new Dictionary<Suit, bool>();
                var isPartnerVoidInSuit = new Dictionary<Suit, bool>();
                var goodSuitForPartner = players.PartnersOf(player).FirstOrDefault(p => p.GoodSuit != Suit.Unknown)?.GoodSuit ?? Suit.Unknown;

                //  don't count trumping in as sloughing a "good suit"
                if (goodSuitForPartner == trump)
                    goodSuitForPartner = Suit.Unknown;

                //  filter out valueless cards if we have some of value
                if (legalCards.Any(IsOfValue) && !legalCards.All(IsOfValue))
                    legalCards = legalCards.Where(IsOfValue).ToList();

                //  filter out trump cards if we're defending and have non-trump (don't want to help the bidding team)
                if (isDefending && trump != Suit.Unknown && legalCards.Any(IsTrump) && legalCards.Any(c => !IsTrump(c)))
                    legalCards = legalCards.Where(c => !IsTrump(c)).ToList();

                foreach (var suit in SuitRank.stdSuits)
                {
                    var card = new Card(suit, Rank.Ace);
                    isLhoVoidInSuit[suit] = players.LhoIsVoidInSuit(player, card, cardsPlayed);
                    isRhoVoidInSuit[suit] = players.RhoIsVoidInSuit(player, card, cardsPlayed);
                    isPartnerVoidInSuit[suit] = players.PartnerIsVoidInSuit(player, card, cardsPlayed);
                }

                if (trump != Suit.Unknown && !isRhoVoidInSuit[trump])
                {
                    //  RHO may still have trump: try to avoid suits where RHO is known to be void
                    var avoidSuits = SuitRank.stdSuits.Where(s => isRhoVoidInSuit[s]).ToList();
                    var preferredLegalCards = legalCards.Where(c => !avoidSuits.Contains(EffectiveSuit(c))).ToList();
                    if (preferredLegalCards.Count > 0)
                        legalCards = preferredLegalCards;
                }

                if (trump != Suit.Unknown && !isLhoVoidInSuit[trump])
                {
                    //  LHO may still have trump: try to avoid suits where LHO is known to be void unless partner is also void and may have trump
                    var avoidSuits = SuitRank.stdSuits.Where(s => isLhoVoidInSuit[s] && (isPartnerVoidInSuit[trump] || !isPartnerVoidInSuit[s])).ToList();
                    var preferredLegalCards = legalCards.Where(c => !avoidSuits.Contains(EffectiveSuit(c))).ToList();
                    if (preferredLegalCards.Count > 0)
                        legalCards = preferredLegalCards;
                }

                if (trump != Suit.Unknown)
                {
                    if (isLhoVoidInSuit[trump] && isRhoVoidInSuit[trump])
                    {
                        //  RHO and LHO are both out of trump, prefer leading an off-suit
                        var preferredLegalCards = legalCards.Where(c => !IsTrump(c)).ToList();
                        if (preferredLegalCards.Count > 0)
                            legalCards = preferredLegalCards;
                    }
                    else if (!isDefending)
                    {
                        //  opponents may still have trump, prefer leading it if we hold at least two and are not defending
                        var preferredLegalCards = legalCards.Where(IsTrump).ToList();
                        if (preferredLegalCards.Count > 1)
                            legalCards = preferredLegalCards;
                    }
                }

                var cards = legalCards;
                var bossCards = legalCards.Where(c => IsCardHigh(c, cardsPlayed)).OrderByDescending(c => cards.Count(c1 => EffectiveSuit(c1) == EffectiveSuit(c))).ToList();
                if (bossCards.Count > 0)
                {
                    //  consider leading our "boss" cards favoring boss in our longest suit
                    suggestion = bossCards.First();
                }
                else if (legalCards.Any(c => EffectiveSuit(c) == goodSuitForPartner))
                {
                    //  partner has signaled a "good suit", lead low in it
                    suggestion = legalCards.OrderBy(RankSort).First(c => EffectiveSuit(c) == goodSuitForPartner);
                }
                else if (trump != Suit.Unknown && !isPartnerVoidInSuit[trump])
                {
                    //  partner may still have trump: try to lead a suit where partner is known to be void
                    var preferSuits = SuitRank.stdSuits.Where(s => isPartnerVoidInSuit[s]).ToList();
                    var preferredLegalCards = legalCards.Where(c => preferSuits.Contains(EffectiveSuit(c))).ToList();
                    if (preferredLegalCards.Count > 0)
                        legalCards = preferredLegalCards;
                }
            }
            else
            {
                var nActivePlayers = players.Count(p => p.IsActivelyPlaying);
                var lastToPlay = trick.Count == nActivePlayers - 1; // use real trick count in this situation
                var nextToLastToPlay = trick.Count == nActivePlayers - 2;

                var trickSuit = EffectiveSuit(firstCardInTrick);

                if (EffectiveSuit(legalCards[0]) == trickSuit)
                {
                    //  we can follow suit

                    if (IsTrump(firstCardInTrick) || trick.All(c => !IsTrump(c)))
                    {
                        //  either it's a trump-led trick or there's no trump in the trick

                        if (isPartnerTakingTrick && (lastToPlay || IsCardHigh(cardTakingTrick, cardsPlayed.Concat(new Hand(player.Hand)))))
                        {
                            //  our partner is taking the trick and we're either the last to play or they are taking it with "boss" - don't try to take it
                        }
                        else if (lastToPlay)
                        {
                            //  we're the last to play: just play good enough to take the trick
                            var highestInTrick = trick.Where(c => EffectiveSuit(c) == trickSuit).Max(RankSort);
                            suggestion = legalCards.Where(c => RankSort(c) > highestInTrick).OrderBy(RankSort).FirstOrDefault();
                        }
                        else if (IsPartnership && nextToLastToPlay)
                        {
                            //  we're 2nd to last to play: play as high as we can if we can take the trick and our card is effectively better than partner's
                            //  this keeps the last player from winning with a low card (or letting their partner win with one)
                            var highCard = legalCards.OrderBy(RankSort).Last();
                            if (isPartnerTakingTrick ? RankSort(highCard) - RankSort(cardTakingTrick) > 1 : RankSort(highCard) > RankSort(cardTakingTrick))
                                suggestion = highCard;
                        }
                        else
                        {
                            //  we're not the last to play nor the 2nd to last: if our best card is "boss," play it
                            var highCard = legalCards.OrderBy(RankSort).Last();

                            if (IsCardHigh(highCard, cardsPlayed))
                                suggestion = highCard;
                        }
                    }
                }
                else if (legalCards.Any(IsTrump))
                {
                    //  we can't follow suit but we have trump

                    if (IsPartnership && trickCount == 1 && players.LhoIsVoidInSuit(player, firstCardInTrick, cardsPlayed))
                    {
                        //  second to play and left hand opponent is void in the led suit - don't trump-in; leave it to our partner
                    }
                    else if (isPartnerTakingTrick && (lastToPlay || IsCardHigh(cardTakingTrick, cardsPlayed.Concat(new Hand(player.Hand)))))
                    {
                        //  our partner is taking the trick and we're either the last to play or they are taking it with "boss" - don't trump in
                        suggestion = TrySignalGoodSuit(player, legalCards, cardsPlayed, isDefending);
                    }
                    else if (trick.All(c => !IsTrump(c)))
                    {
                        //  no trump currently in the trick: play our lowest trump
                        suggestion = legalCards.Where(IsTrump).OrderBy(RankSort).First();
                    }
                    else
                    {
                        //  trump currently in the trick: play high enough to beat what's in the trick already (if we can)
                        var highestTrumpInTrick = trick.Where(IsTrump).Max(RankSort);
                        suggestion = legalCards.Where(c => IsTrump(c) && RankSort(c) > highestTrumpInTrick).OrderBy(RankSort).FirstOrDefault();
                    }
                }
                else
                {
                    //  we can't follow suit and don't have trump - try signaling a good suit
                    suggestion = TrySignalGoodSuit(player, legalCards, cardsPlayed, isDefending);
                }
            }

            //  if we generated a suggestion, return it
            if (suggestion != null)
                return suggestion;

            //  else, if we only have trump, return the lowest
            if (legalCards.All(IsTrump))
                return legalCards.OrderBy(RankSort).First();

            //  else, dump the lowest card from the weakest suit
            return LowestCardFromWeakestSuit(legalCards, cardsPlayed);
        }
    }
}