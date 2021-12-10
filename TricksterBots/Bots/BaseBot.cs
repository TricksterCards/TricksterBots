using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public interface IBaseBot
    {
        DeckType DeckType { get; }
        bool IsPartnership { get; }
        bool IsTwoTeams { get; }
        GameOptions Options { get; }
        Suit TrumpSuit { get; }
        bool CanSeeHand(PlayersCollectionBase players, PlayerBase player, PlayerBase target);
        Suit EffectiveSuit(Card c);
        bool IsOfValue(Card c);
        bool IsTrump(Card card);
        int RankSort(Card c);
        int TrickHighCardIndex(IReadOnlyList<Card> trick);
        int SuitSort(Card c);
        int SuitOrder(Suit s);
    }

    public abstract class BaseBot<T> : IBaseBot where T : GameOptions
    {
        private Dictionary<Suit, int> _highRankBySuit;

        protected BaseBot(T options, Suit trumpSuit)
        {
            this.options = options;
            trump = trumpSuit;
        }

        protected Dictionary<Suit, int> highRankBySuit =>
            _highRankBySuit ?? (_highRankBySuit = DeckBuilder.BuildDeck(DeckType).GroupBy(c => EffectiveSuit(c, trump))
                .ToDictionary(g => g.Key, g => g.Max(c => RankSort(c, trump))));

        protected T options { get; }

        protected Suit trump { get; }

        public virtual bool CanSeeHand(PlayersCollectionBase players, PlayerBase player, PlayerBase target)
        {
            return player.Seat == target.Seat;
        }

        public abstract DeckType DeckType { get; }

        public Suit EffectiveSuit(Card c)
        {
            return EffectiveSuit(c, trump);
        }

        public virtual bool IsOfValue(Card c)
        {
            return true;
        }

        /// <summary>
        ///     Indicates whether this game is played as a partner. Could be a 2- or 3- player partnership.
        /// </summary>
        public bool IsPartnership => options.isPartnership;

        public bool IsTrump(Card card)
        {
            return IsOfValue(card) && EffectiveSuit(card) == trump;
        }

        /// <summary>
        ///     Indicates if a 6-player game is played as 2 teams in 3v3 configuration.
        /// </summary>
        public bool IsTwoTeams => options.isTwoTeams;

        public GameOptions Options => options;

        public int RankSort(Card c)
        {
            return RankSort(c, trump);
        }

        public int TrickHighCardIndex(IReadOnlyList<Card> trick)
        {
            if (trick.Count(IsOfValue) == 0)
                return -1;

            var takeSuit = trick.Any(IsTrump) ? trump : EffectiveSuit(trick.First(IsOfValue));
            var takeRank = RankSort(trick.Where(c => EffectiveSuit(c) == takeSuit).OrderBy(RankSort).Last());
            var takeCard = trick.First(c => EffectiveSuit(c) == takeSuit && RankSort(c) == takeRank);

            //  find the index of the takeCard
            for (var i = 0; i < trick.Count; ++i)
            {
                if (trick[i] == takeCard)
                    return i;
            }

            return -1;
        }

        public Suit TrumpSuit => trump;

        public abstract BidBase SuggestBid(SuggestBidState<T> state);

        public abstract List<Card> SuggestDiscard(SuggestDiscardState<T> state);

        public abstract Card SuggestNextCard(SuggestCardState<T> state);

        public abstract List<Card> SuggestPass(SuggestPassState<T> state);

        public virtual int SuitSort(Card c)
        {
            var suitOrder = SuitOrder(EffectiveSuit(c));
            return suitOrder > SuitOrder(trump) ? suitOrder - 5 : suitOrder;
        }

        protected virtual Suit EffectiveSuit(Card c, Suit trumpSuit)
        {
            return c.suit == Suit.Joker && trumpSuit != Suit.Unknown ? trumpSuit : c.suit;
        }

        protected int HighRankInSuit(Card card)
        {
            return HighRankInSuit(EffectiveSuit(card));
        }

        protected int HighRankInSuit(Suit suit)
        {
            return highRankBySuit.TryGetValue(suit, out var r) ? r : (int)Rank.Ace;
        }

        protected bool IsCardHigh(Card highestCard, IEnumerable<Card> cardsPlayed)
        {
            if (!IsOfValue(highestCard))
                return false;

            var highRank = HighRankInSuit(highestCard);
            return RankSort(highestCard) == highRank ||
                   cardsPlayed.Count(c => EffectiveSuit(c) == EffectiveSuit(highestCard) && RankSort(c) > RankSort(highestCard)) ==
                   highRank - RankSort(highestCard);
        }

        protected virtual int RankSort(Card c, Suit trumpSuit)
        {
            return (int)c.rank;
        }

        public virtual int SuitOrder(Suit s)
        {
            return (int)s;
        }

        //  NOTE: If you're going to edit this in a game-specific way, copy the method to your bot and edit it there
        protected Card TryTakeEm(PlayerBase player, IReadOnlyList<Card> trick, IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed, PlayersCollectionBase players, bool isPartnerTakingTrick,
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

        //  NOTE: If you're going to edit this in a game-specific way, copy the method to your bot and edit it there
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

        //  NOTE: If you're going to edit this in a game-specific way, copy the method to your bot and edit it there
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
    }
}