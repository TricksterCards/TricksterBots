using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public class WhistBot : BaseBot<WhistOptions>
    {
        public WhistBot(WhistOptions options, Suit trumpSuit) : base(options, trumpSuit)
        {
        }

        private int UnplayedCardCountAbove(Card c, IReadOnlyList<Card> cardsPlayed)
        {
            var suit = EffectiveSuit(c);
            var rank = RankSort(c);
            var rankGapToDeckTop = HighRankInSuit(c) - rank;
            var playedAbove = cardsPlayed.Count(p => EffectiveSuit(p) == suit && RankSort(p) > rank);
            return rankGapToDeckTop - playedAbove;
        }

        private bool TopCanBeCovered(Card top, IReadOnlyList<Card> cardsPlayed) =>
            UnplayedCardCountAbove(top, cardsPlayed) <= 1;

        private bool HasOnlyOneCardAbove(Card c, IReadOnlyList<Card> cardsPlayed) =>
            !IsCardHigh(c, cardsPlayed) && UnplayedCardCountAbove(c, cardsPlayed) == 1;

        private static int TricksTaken(PlayerBase player)
        {
            return player.HandScore;
        }

        private bool CanOrMustCashBossCards(PlayerBase player, PlayersCollectionBase players, IReadOnlyList<Card> bossCards, bool isDefending)
        {
            var declarer = players.FirstOrDefault(p => new WhistBid(p.Bid).IsDeclareBid);
            if (declarer == null)
                return false;

            var contract = new WhistBid(declarer.Bid);
            var partner = players.PartnerOf(player);
            var tricksTaken = TricksTaken(player);
            if (partner != null)
                tricksTaken += TricksTaken(partner);

            var tricksRemaining = player.Hand.Length / 2;

            if (isDefending)
                return tricksTaken + bossCards.Count > 13 - contract.Tricks
                    || tricksTaken + tricksRemaining == 13 - contract.Tricks;
            else
                return tricksTaken + bossCards.Count >= contract.Tricks
                    || tricksTaken + tricksRemaining == contract.Tricks;
        }

        private Suit PartnerIntroducedSuit(PlayerBase player, PlayersCollectionBase players, IReadOnlyList<Card> cardsPlayed,
            string cardsPlayedInOrder)
        {
            var partner = players.PartnersOf(player).FirstOrDefault();
            if (partner == null)
                return Suit.Unknown;

            var suit = partner.GoodSuit;
            if (trump != Suit.Unknown && suit != Suit.Unknown && suit != trump)
                return suit; // use GoodSuit in trump contracts

            if (trump != Suit.Unknown)
                return Suit.Unknown; // only read signals in NT contracts

            var playedOrder = cardsPlayedInOrder;
            if (string.IsNullOrEmpty(playedOrder))
                return Suit.Unknown; // no cards played yet, so partner hasn't had a chance to signal

            var tricks = GetCardsPlayedByTrick(playedOrder, players.Count);
            var lastPartnerLeadCard = tricks.LastOrDefault(trick => trick[0].seat == partner.Seat)?.First().card;
            if (lastPartnerLeadCard == null)
                return Suit.Unknown; // partner hasn't led

            var lastPlayerLeadCard = tricks.LastOrDefault(trick => trick[0].seat == player.Seat)?.First().card;
            if (lastPlayerLeadCard != null && EffectiveSuit(lastPlayerLeadCard) == EffectiveSuit(lastPartnerLeadCard))
                return Suit.Unknown; // partner is likely returning our lead, not introducing a new suit

            return EffectiveSuit(lastPartnerLeadCard);
        }

        private List<Card> BossCards(PlayerBase player, IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed, PlayersCollectionBase players)
        {
            var bossCards = legalCards.Where(c => IsCardHigh(c, cardsPlayed.Concat(legalCards))).ToList();
            var bossSuits = bossCards.Select(EffectiveSuit).Distinct().ToList();

            // In any suit where we have boss cards,
            // check if we can exhaust the remaining cards in that suit
            // (making any others cards in that suit boss too)
            foreach (var suit in bossSuits)
            {
                var nBossCardsInSuit = bossCards.Count(c => EffectiveSuit(c) == suit);
                var nLegalCardsInSuit = legalCards.Count(c => EffectiveSuit(c) == suit);
                if (nLegalCardsInSuit <= nBossCardsInSuit)
                    continue;

                var nonBossCardsInSuit = legalCards.Where(c => EffectiveSuit(c) == suit && !IsCardHigh(c, cardsPlayed)).ToList();

                // if opponents are both void in the suit, we can effectively exhaust it (partner should help)
                if (players.OpponentsVoidSuits(player).TryGetValue(suit, out var opponentsVoid) && opponentsVoid)
                {
                    bossCards.AddRange(nonBossCardsInSuit);
                    continue;
                }

                //  otherwise check if we can pull all other remaining cards in the suit out
                var nPlayedCardsInSuit = cardsPlayed.Count(c => EffectiveSuit(c) == suit);
                var nCardsRemainingInSuit = cardsBySuit[suit].Count() - nPlayedCardsInSuit - nLegalCardsInSuit;
                if (nBossCardsInSuit >= nCardsRemainingInSuit)
                {
                    bossCards.AddRange(nonBossCardsInSuit);
                }
            }

            return bossCards;
        }

        private Card TryLeadBackInPartnerSuit(PlayerBase player, IReadOnlyList<Card> legalCards,
            IReadOnlyList<Card> cardsPlayed, PlayersCollectionBase players, bool isDefending, string cardsPlayedInOrder)
        {
            var bossCards = BossCards(player, legalCards, cardsPlayed, players);

            if (CanOrMustCashBossCards(player, players, bossCards, isDefending))
                return null;

            var declarer = players.FirstOrDefault(p => new WhistBid(p.Bid).IsDeclareBid);
            var isCurrentSeatDeclarer = player.Seat == declarer?.Seat;
            var isPartnerDeclarer = declarer != null && players.PartnerOf(player)?.Seat == declarer.Seat;

            var partnerSuit = PartnerIntroducedSuit(player, players, cardsPlayed, cardsPlayedInOrder);
            if (partnerSuit == Suit.Unknown || !legalCards.Any(c => EffectiveSuit(c) == partnerSuit))
                return null;

            if ((isCurrentSeatDeclarer || isDefending) && trump == Suit.Unknown)
            {
                //  NT declarer/defender: come back in the suit partner signaled from the lead (lowest card).
                return legalCards
                    .Where(c => EffectiveSuit(c) == partnerSuit)
                    .OrderBy(RankSort)
                    .FirstOrDefault();
            } else if (isPartnerDeclarer) {
                // Offensive partner: lead the highest card in partner's suit to show strength.
                return legalCards
                    .Where(c => EffectiveSuit(c) == partnerSuit)
                    .OrderByDescending(RankSort)
                    .FirstOrDefault();
            }

            return null;
        }

        private Card TrySignalGoodSuitOnLead(PlayerBase player, IReadOnlyList<Card> legalCards,
            IReadOnlyList<Card> cardsPlayed, PlayersCollectionBase players, bool isDefending)
        {
            if (trump != Suit.Unknown)
                return null;

            var bossCards = BossCards(player, legalCards, cardsPlayed, players);

            if (CanOrMustCashBossCards(player, players, bossCards, isDefending))
                return null;

            //  Detect self-good suits (boss / deck-top + cover + tail), pick the best suit to signal, then lead the lowest card in that suit.
            var knownCards = cardsPlayed.Concat(legalCards).ToList();
            var candidateSignals = new List<(Suit suit, bool isBoss, int suitCount, int rankForSuitOrdering)>();

            foreach (var suitGroup in legalCards.GroupBy(EffectiveSuit))
            {
                var isBoss = false;
                var suit = suitGroup.Key;
                var suitCards = suitGroup.OrderByDescending(RankSort).ToList();
                if (suitCards.Count < 2)
                    continue;

                var top = suitCards.First();
                var bottom = suitCards.Last();
                int? rankForSuitOrdering = null;

                if (IsCardHigh(top, knownCards))
                {
                    if (!IsCardHigh(bottom, knownCards))
                    {
                        isBoss = true;
                        rankForSuitOrdering = RankSort(top);
                    }
                }
                else if (suitCards.Count >= 3)
                {
                    // If we have > 3 cards in a suit, and we determine that we can cover the top card with a stopper such
                    // that it can become the high card, we can signal this suit by leading the lowest card in that suit.
                    if (TopCanBeCovered(top, cardsPlayed))
                        rankForSuitOrdering = RankSort(top);
                }

                if (rankForSuitOrdering != null)
                    candidateSignals.Add((suit, isBoss, suitCards.Count, rankForSuitOrdering.Value));
            }

            //  Choose a qualifying suit (higher rank then longest suit tiebreak); we lead the lowest legal card in that suit below.
            var bestSuit = candidateSignals
                .OrderBy(c => c.isBoss) //  prefer promoting suits over those we already have boss cards in
                .ThenByDescending(c => c.rankForSuitOrdering)
                .ThenByDescending(c => c.suitCount)
                .Select(c => c.suit)
                .FirstOrDefault();

            if (bestSuit != Suit.Unknown)
            {
                return legalCards
                    .Where(c => EffectiveSuit(c) == bestSuit)
                    .OrderBy(RankSort)
                    .FirstOrDefault();
            }

            //  If we don't have any winners with cover, we lead lowest in longest suit instead of falling back to trying to take with a boss card.
            var longestSuitGroup = legalCards
                .GroupBy(EffectiveSuit)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .FirstOrDefault();

            return longestSuitGroup == null
                ? null
                : longestSuitGroup.OrderBy(RankSort).FirstOrDefault();
        }

        //  NT slough helper with some logic based on base bot's LowestCardFromWeakestSuit: pick a low discard from a weak suit.
        private Card LowestCardFromWeakestSuitNT(IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed)
        {
            var cards = legalCards as IList<Card> ?? legalCards.ToList();

            var suitCounts = cards.GroupBy(EffectiveSuit).Select(g => new { suit = g.Key, count = g.Count() }).ToList();

            //  try to ditch a singleton that's not "boss" and whose suit has the most outstanding cards
            var bestSingletonSuitCount = suitCounts.Where(sc => sc.count == 1)
                .Where(sc => !IsCardHigh(cards.Single(c => EffectiveSuit(c) == sc.suit), cardsPlayed))
                .OrderBy(sc => cardsPlayed.Count(c => EffectiveSuit(c) == sc.suit)).FirstOrDefault();

            if (bestSingletonSuitCount != null)
                return cards.Single(c => EffectiveSuit(c) == bestSingletonSuitCount.suit);

            //  now we look at doubletons in the order of the number of remaining cards in the suit
            var doubletonSuitCounts = suitCounts.Where(sc => sc.count == 2).OrderBy(sc => cardsPlayed.Count(c => EffectiveSuit(c) == sc.suit)).ToList();

            foreach (var sc in doubletonSuitCounts)
            {
                var suitCards = cards.Where(c => EffectiveSuit(c) == sc.suit).OrderBy(RankSort).ToList();
                var low = suitCards[0];
                var high = suitCards[1];

                if (!IsCardHigh(high, cardsPlayed) && !HasOnlyOneCardAbove(high, cardsPlayed))
                    return low;
            }

            //  try to slough from suits with 3+ cards to avoid hitting unsuitable cards in the singleton/doubleton logic above
            var longerSuitCards = cards.Where(c => suitCounts.Any(sc => sc.suit == EffectiveSuit(c) && sc.count >= 3)).ToList();

            if (longerSuitCards.Any())
                return longerSuitCards.OrderBy(c => longerSuitCards.Count(c1 => EffectiveSuit(c1) == EffectiveSuit(c))).ThenBy(RankSort).First();

            //  nothing with 3+ cards left; prefer low from a doubleton over a singleton
            var doubletonCards = cards.Where(c => suitCounts.Any(sc => sc.suit == EffectiveSuit(c) && sc.count == 2)).ToList();

            if (doubletonCards.Any())
                return doubletonCards.OrderBy(RankSort).First();

            return cards.OrderBy(RankSort).First();
        }

        private Card TryNTSlough(IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed, IReadOnlyList<Card> trick, bool isDefending)
        {
            if (trump != Suit.Unknown)
                return null;

            // If for some reason we are leading and this was called, return null
            var firstCardInTrick = trick.FirstOrDefault(IsOfValue);
            if (firstCardInTrick == null)
                return null;

            // Fall back to TryTakeEm if we can follow suit
            var trickSuit = EffectiveSuit(firstCardInTrick);
            if (legalCards.Any(c => EffectiveSuit(c) == trickSuit))
                return null;

            // If we have a joker, slough it
            if (legalCards.Any(c => c.suit == Suit.Joker))
                return legalCards.First(c => c.suit == Suit.Joker);

            if (!isDefending)
                return LowestCardFromWeakestSuitNT(legalCards, cardsPlayed);

            return null;
        }

        public override BidBase SuggestBid(SuggestBidState<WhistOptions> state)
        {
            var hand = state.hand;

            var suits = new List<Suit> { Suit.Unknown }.Concat(SuitRank.stdSuits).ToList();
            var lowIsHigh = options._lowIsHigh; // save

            options._lowIsHigh = false; // RankSort looks at this
            var highTricksBySuit = suits.ToDictionary(s => s, s => CountTricks(hand, s));
            var maxTrumpHighTricks = highTricksBySuit.Max(kvp => kvp.Key != Suit.Unknown ? kvp.Value : 0);

            options._lowIsHigh = true; // RankSort looks at this
            var lowTricksBySuit = suits.ToDictionary(s => s, s => CountTricks(hand, s));
            var maxTrumpLowTricks = lowTricksBySuit.Max(kvp => kvp.Key != Suit.Unknown ? kvp.Value : 0);

            var maxNotrumpTricks = Math.Max(highTricksBySuit[Suit.Unknown], lowTricksBySuit[Suit.Unknown]);

            options._lowIsHigh = lowIsHigh; // restore

            return state.legalBids.Where(b => b.value != BidBase.NoBid).OrderBy(b =>
            {
                var wb = new WhistBid(b);

                if (!wb.IsDeclareBid)
                    return -1;

                //  start with 1 extra trick for the widow and 3 for partner
                var tricks = 4;

                if (options.bidderGetsKitty)
                    tricks += 1; //  estimate one more trick if we get the kitty

                if (options._highBidderSeat.HasValue)
                {
                    //  second round of bidding
                    //  get the correct estimated tricks based on suit and whether high or low wins
                    tricks += (wb.HighWins ? highTricksBySuit : lowTricksBySuit)[wb.Suit];
                }
                else
                {
                    //  first round of bidding
                    if (wb.Suit == Suit.Unknown)
                    {
                        //  no-trump, take the best of high/low
                        tricks += maxNotrumpTricks;
                    }
                    else
                    {
                        //  trump, take the best suit's tricks depending on whether high or low wins
                        tricks += wb.HighWins ? maxTrumpHighTricks : maxTrumpLowTricks;
                    }
                }

                return tricks - wb.Tricks;
            }).Last();
        }

        public override List<Card> SuggestDiscard(SuggestDiscardState<WhistOptions> state)
        {
            var (player, hand) = (state.player, state.hand);

            List<Card> cards;

            var count = options.KittySize;
            var theBid = new WhistBid(player.Bid);

            if (theBid.Suit == Suit.Unknown)
            {
                //  in no-trump, throw the lowest cards we have, but make sure to get rid of Jokers first as they're useless here
                //  TODO: try to balance this by keeping cards we need to stop a running suit
                cards = hand.OrderBy(c => c.suit != Suit.Joker).ThenBy(RankSort).Take(count).ToList();
            }
            else
            {
                //  in trump, first group by suits to focus on creating void off-suits
                cards = hand.GroupBy(EffectiveSuit).ToDictionary(g => g.Key, g => g.OrderBy(RankSort).ToList())

                    //  save trump to discard last
                    .OrderBy(kvp => kvp.Key == trump)

                    //  try to get rid of off-suits with no cards we can make boss,
                    .ThenBy(kvp => 0 >= HighRankInSuit(kvp.Key) - RankSort(kvp.Value.Last()) - (kvp.Value.Count - 1))

                    //  followed by those that will take the longest to make a card boss
                    .ThenByDescending(kvp => HighRankInSuit(kvp.Key) - RankSort(kvp.Value.Last()))

                    //  then just get rid of the lowest cards in the shortest suits
                    .ThenBy(kvp => kvp.Value.Count)

                    //  now merge the suits into one flat list and take however many cards we need to discard
                    .SelectMany(kvp => kvp.Value).Take(count).ToList();
            }

            return cards;
        }

        public override Card SuggestNextCard(SuggestCardState<WhistOptions> state)
        {
            var bid = new WhistBid(state.player.Bid);
            var isDefending = !bid.IsDeclareBid && !bid.IsDeclarePartnerBid;
            var legalCards = state.legalCards;
            var players = new PlayersCollectionBase(this, state.players);

            // Leading suggestions for NT
            if (state.trick.Count == 0 && state.trumpSuit == Suit.Unknown)
            {
                // Don't lead jokers in no-trump
                if (legalCards.Any(c => c.suit == Suit.Joker) && legalCards.Any(c => c.suit != Suit.Joker))
                {
                    legalCards = legalCards.Where(c => c.suit != Suit.Joker).ToList();
                }

                // Avoid leading a suit partner is known to be void in
                var avoidPartnerVoidSuits = SuitRank.stdSuits.Where(s =>
                    players.PartnerIsVoidInSuit(state.player, new Card(s, Rank.Ace), state.cardsPlayed)).ToList();
                if (avoidPartnerVoidSuits.Count > 0)
                {
                    var withoutPartnerVoidLead = legalCards.Where(c =>
                        !avoidPartnerVoidSuits.Contains(EffectiveSuit(c)) || IsCardHigh(c, state.cardsPlayed)).ToList();
                    if (withoutPartnerVoidLead.Count > 0)
                        legalCards = withoutPartnerVoidLead;
                }

                var leadBack = TryLeadBackInPartnerSuit(state.player, legalCards, state.cardsPlayed, players, isDefending, state.cardsPlayedInOrder);
                if (leadBack != null)
                    return leadBack;

                var signal = TrySignalGoodSuitOnLead(state.player, legalCards, state.cardsPlayed, players, isDefending);
                if (signal != null)
                    return signal;
            }
            else if (state.trick.Count > 0)
            {
                var slough = TryNTSlough(legalCards, state.cardsPlayed, state.trick, isDefending);
                if (slough != null)
                    return slough;
            }

            return TryTakeEm(state.player,
                state.trick,
                legalCards,
                state.cardsPlayed,
                players,
                state.isPartnerTakingTrick,
                state.cardTakingTrick,
                isDefending);
        }

        public override List<Card> SuggestPass(SuggestPassState<WhistOptions> state)
        {
            throw new NotImplementedException();
        }

        private int CountTricks(IEnumerable<Card> hand, Suit trumpSuit)
        {
            var deckBySuit = DeckBuilder.BuildDeck(DeckType).GroupBy(c => EffectiveSuit(c, trumpSuit)).ToDictionary(g => g.Key, g => g.OrderBy(c => RankSort(c, trumpSuit)).ToList());
            var handBySuit = SuitRank.stdSuits.ToDictionary(s => s, s => hand.Where(c => EffectiveSuit(c, trumpSuit) == s).OrderBy(c => RankSort(c, trumpSuit)).ToList());

            var tricks = 0;

            if (trumpSuit == Suit.Unknown)
            {
                //  in no-trump, count 1 trick for each joker
                tricks += hand.Count(c => c.suit == Suit.Joker);
            }
            else
            {
                //  trump suits are only "good" if we have at least 4 trump
                var trumpCards = handBySuit[trumpSuit];
                if (trumpCards.Count < 4)
                    return 0;

                //  with trump, count trump we can use on suits with singletons or voids
                foreach (var suit in SuitRank.stdSuits.Where(s => s != trumpSuit).ToList())
                {
                    var countInSuit = handBySuit[suit].Count;
                    if (countInSuit < 2)
                    {
                        var trumpIn = Math.Min(2 - countInSuit, trumpCards.Count);
                        trumpCards.RemoveRange(0, trumpIn);
                        tricks += trumpIn;
                    }
                }
            }

            //  then calculate the winners for each suit, accounting for gaps
            foreach (var suit in SuitRank.stdSuits)
            {
                var deck = deckBySuit[suit];
                var cards = handBySuit[suit];

                var highRank = RankSort(deck.Last(), trumpSuit);
                var nextHighestRank = highRank;
                var hasStopper = false;

                while (cards.Any())
                {
                    //  don't give credit for off-suit cards more than two steps below the highest rank in a trump contract
                    //  reasoning: too easy for other players to be void and trump in by that point
                    if (trumpSuit != Suit.Unknown && suit != trumpSuit && highRank - nextHighestRank > 2)
                        break;

                    var targetCard = cards.Last(); //  start with our next highest card
                    var targetRank = RankSort(targetCard, trumpSuit);
                    var gaps = deck.Count(c => targetRank < RankSort(c, trumpSuit) && RankSort(c, trumpSuit) <= nextHighestRank && !cards.Contains(c));
                    var below = cards.Count(c => RankSort(c, trumpSuit) < targetRank);

                    if (gaps > below)
                        break;

                    tricks++;
                    hasStopper = true;
                    nextHighestRank = targetRank;
                    cards.Remove(targetCard);
                    cards.RemoveRange(0, gaps);
                }

                //  if we're looking at no-trump and we don't have a stopper in all suits, bail
                if (trumpSuit == Suit.Unknown && !hasStopper)
                    return 0;
            }

            return tricks;
        }
    }
}