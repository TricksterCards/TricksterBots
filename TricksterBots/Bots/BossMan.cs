using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    /// <summary>
    ///     General helper class for figuring out sure and likely winners.
    /// </summary>
    /// <remarks>
    ///     Currently only used by Pinochle but could potentially be used by more games.
    /// </remarks>
    internal class BossMan
    {
        private readonly IBaseBot _bot;
        private readonly Card _highCardInTrick;
        private readonly bool _lastToPlay;
        private readonly int _numPlayers;
        private readonly Dictionary<Suit, bool> _opponentsVoidInSuit;
        private readonly Hand _outstandingCards;
        private readonly Dictionary<Suit, int> _outstandingCountBySuit;
        private readonly Hand _trick;
        private readonly bool _trickContainsTrump;
        private readonly Suit _trickSuit;

        public BossMan(IBaseBot bot, IEnumerable<Card> cardsPlayed, IEnumerable<Card> playersHand, IEnumerable<Card> otherKnownCards = null, IEnumerable<Card> trick = null,
            Dictionary<Suit, bool> opponentsVoidSuits = null)
        {
            _bot = bot;
            _numPlayers = bot.Options.players;

            var allKnownCards = new List<Card>(cardsPlayed).Concat(playersHand).Concat(otherKnownCards ?? Array.Empty<Card>()).Concat(trick ?? Array.Empty<Card>()).ToList();
            _outstandingCards = new Hand(DeckBuilder.BuildDeck(_bot.DeckType)).RemoveCards(allKnownCards);
            _outstandingCountBySuit = _outstandingCards.GroupBy(_bot.EffectiveSuit).ToDictionary(g => g.Key, g => g.Count());

            if (opponentsVoidSuits != null)
            {
                var countBySuit = DeckBuilder.BuildDeck(_bot.DeckType).GroupBy(_bot.EffectiveSuit).ToDictionary(g => g.Key, g => g.Count());
                var knownBySuit = countBySuit.Keys.ToDictionary(s => s, s => allKnownCards.Count(c => _bot.EffectiveSuit(c) == s));
                _opponentsVoidInSuit = opponentsVoidSuits.Where(kvp => countBySuit.ContainsKey(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value || knownBySuit[kvp.Key] == countBySuit[kvp.Key]);
            }

            if (trick != null)
            {
                _trick = new Hand(trick);
                _lastToPlay = _trick.Count == _numPlayers - 1;

                if (_trick.Count(_bot.IsOfValue) > 0)
                {
                    _trickSuit = _bot.EffectiveSuit(_trick.First(_bot.IsOfValue));

                    var trickHighCardIndex = _bot.TrickHighCardIndex(_trick);
                    _highCardInTrick = trickHighCardIndex != -1 ? _trick[trickHighCardIndex] : null;

                    if (_highCardInTrick != null)
                        _trickContainsTrump = _bot.IsTrump(_highCardInTrick);
                }
            }
        }

        private int MinOutstandingInSuit => (_numPlayers - 1) * 5 / 3 - (_trick?.Count ?? 0);

        public static Dictionary<Suit, List<Card>> GetWinnersBySuit(IBaseBot bot, Hand hand)
        {
            var deck = DeckBuilder.BuildDeck(bot.DeckType);
            return hand.GroupBy(bot.EffectiveSuit).Distinct()
                .ToDictionary(g => g.Key, g => CardsThatMightTakeTrick(bot, deck, hand, g.Key).Concat(BossInOffSuits(bot, deck, hand, g.Key)).ToList());
        }

        public bool IsLikelyWinner(Card card)
        {
            if (!PossibleWinner(card, out var isTrump, out var effectiveSuit, out var rankSort, out var isBossInSuit))
                return false;

            if (isTrump && OpponentsVoidIn(_bot.TrumpSuit)
                || isBossInSuit && !OpponentsVoidIn(effectiveSuit) && OutstandingInSuit(effectiveSuit) >= MinOutstandingInSuit
                || isBossInSuit && OpponentsVoidIn(effectiveSuit) && OpponentsVoidIn(_bot.TrumpSuit))
                return CanWinTrick(card, isTrump, rankSort);

            return false;
        }

        public bool IsSureWinner(Card card)
        {
            if (!PossibleWinner(card, out var isTrump, out var effectiveSuit, out var rankSort, out var isBossInSuit))
                return false;

            if (_lastToPlay)
                return CanWinTrick(card, isTrump, rankSort);

            if (isTrump && isBossInSuit || isBossInSuit && OpponentsVoidIn(_bot.TrumpSuit) || OpponentsVoidIn(_bot.TrumpSuit) && OpponentsVoidIn(effectiveSuit))
                return CanWinTrick(card, isTrump, rankSort);

            return false;
        }

        public bool OpponentsVoidIn(Suit suit)
        {
            return _opponentsVoidInSuit?[suit] ?? false;
        }

        private static IEnumerable<Card> BossInOffSuits(IBaseBot bot, List<Card> deck, Hand hand, Suit notSuit)
        {
            var sureWinners = new List<Card>();

            foreach (var suit in hand.Select(bot.EffectiveSuit).Distinct().Where(s => s != notSuit))
            {
                var sortedDeck = deck.Where(c => bot.EffectiveSuit(c) == suit).OrderByDescending(bot.RankSort).ToArray();
                var sortedHand = hand.Where(c => bot.EffectiveSuit(c) == suit).OrderByDescending(bot.RankSort).ToArray();

                for (int i = 0, j = 0; i < sortedDeck.Length && j < sortedHand.Length; ++i, ++j)
                {
                    if (sortedDeck[i].SameAs(sortedHand[j]))
                        sureWinners.Add(sortedDeck[i]);
                    else
                        break;
                }
            }

            return sureWinners;
        }

        private static IEnumerable<Card> CardsThatMightTakeTrick(IBaseBot bot, IEnumerable<Card> deck, Hand hand, Suit suit)
        {
            var sortedDeck = deck.Where(c => bot.EffectiveSuit(c) == suit).OrderByDescending(bot.RankSort).ToArray();
            var sortedHand = hand.Where(c => bot.EffectiveSuit(c) == suit).OrderByDescending(bot.RankSort).ToArray();

            var takers = new List<Card>();
            var gaps = 0;
            for (int i = 0, j = 0; i < sortedDeck.Length && j < sortedHand.Length - gaps; ++i)
            {
                if (sortedDeck[i].SameAs(sortedHand[j]))
                    takers.Add(sortedHand[j++]);
                else
                    gaps += 1;
            }

            return takers;
        }

        private bool CanWinTrick(Card card, bool isTrump, int rankSort)
        {
            if (_trick.Count(_bot.IsOfValue) == 0)
                return true;

            if (isTrump && !_trickContainsTrump)
                return true;

            return _bot.RankSort(_highCardInTrick) < rankSort || card == _highCardInTrick;
        }

        private bool IsBossInSuit(Suit effectiveSuit, int rankSort)
        {
            return _outstandingCards.Where(c => _bot.EffectiveSuit(c) == effectiveSuit).All(c => _bot.RankSort(c) <= rankSort);
        }

        private int OutstandingInSuit(Suit s)
        {
            return _outstandingCountBySuit.TryGetValue(s, out var n) ? n : 0;
        }

        private bool PossibleWinner(Card card, out bool isTrump, out Suit effectiveSuit, out int rankSort, out bool isBossInSuit)
        {
            if (_trick == null)
                throw new Exception("Call to IsSureWinner when trick is null");

            isTrump = _bot.IsTrump(card);
            effectiveSuit = _bot.EffectiveSuit(card);
            rankSort = _bot.RankSort(card);
            isBossInSuit = IsBossInSuit(effectiveSuit, rankSort);

            if (!_bot.IsOfValue(card))
                return false;

            if (_trickContainsTrump && !isTrump)
                return false;

            if (_trickSuit != Suit.Unknown)
            {
                if (effectiveSuit != _trickSuit && !isTrump)
                    return false;

                if (effectiveSuit == _trickSuit && rankSort < _bot.RankSort(_highCardInTrick))
                    return false;
            }

            return true;
        }
    }
}