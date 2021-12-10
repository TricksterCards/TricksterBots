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
    }
}