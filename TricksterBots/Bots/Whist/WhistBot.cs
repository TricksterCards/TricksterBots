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
            return string.IsNullOrEmpty(player.CardsTaken) ? 0 : player.CardsTaken.Length / 8;
        }

        private bool CanCashBossCardsToCoverContract(PlayersCollectionBase players, IReadOnlyList<Card> bossCards)
        {
            var declarer = players.FirstOrDefault(p => new WhistBid(p.Bid).IsDeclareBid);
            if (declarer == null)
                return false;

            var contract = new WhistBid(declarer.Bid);
            var partner = players.PartnerOf(declarer);
            var tricksTaken = TricksTaken(declarer);
            if (partner != null)
                tricksTaken += TricksTaken(partner);

            return tricksTaken + bossCards.Count >= contract.Tricks;
        }

        private Suit PartnerIntroducedSuitFromAuctionAndSignal(PlayerBase player, PlayersCollectionBase players, IReadOnlyList<Card> cardsPlayed,
            string cardsPlayedInOrder)
        {
            var partner = players.PartnersOf(player).FirstOrDefault();
            if (partner == null)
                return Suit.Unknown;

            var suit = partner.GoodSuit;
            if (suit != Suit.Unknown && suit != trump)
                return suit;


            if (trump == Suit.Unknown)
            {
                //  Infer a suit partner is promoting: walk completed tricks (newest first) and find
                //  one where partner led and their lead was not the highest card of the lead suit in that trick.
                var playedOrder = cardsPlayedInOrder;
                if (!string.IsNullOrEmpty(playedOrder))
                {
                    var ordered = GetCardsPlayedInOrder(playedOrder);
                    if (ordered.Count > 0 && ordered.All(sc => sc.seat >= 0 && sc.seat < players.Count))
                    {
                        var tricks = GetCardsPlayedByTrick(playedOrder, players.Count);
                        for (var t = tricks.Count - 1; t >= 0; t--)
                        {
                            var trick = tricks[t];
                            if (trick.Count == 0 || trick[0].seat != partner.Seat)
                                continue;

                            var leadCard = trick[0].card;
                            var leadSuit = EffectiveSuit(leadCard);
                            if (leadSuit == Suit.Unknown || leadSuit == Suit.Joker)
                                continue;

                            var maxRankInLeadSuit = trick
                                .Where(sc => EffectiveSuit(sc.card) == leadSuit)
                                .Max(sc => RankSort(sc.card));

                            if (RankSort(leadCard) < maxRankInLeadSuit)
                                return leadSuit;
                        }
                    }
                }
            }

            return Suit.Unknown;
        }

        private Card TryLeadBackInPartnerSuit(PlayerBase player, IReadOnlyList<Card> legalCards,
            IReadOnlyList<Card> cardsPlayed, PlayersCollectionBase players, bool isDefending, string cardsPlayedInOrder)
        {
            if (isDefending)
                return null;

            var bossCards = legalCards.Where(c => IsCardHigh(c, cardsPlayed)).ToList();

            if (CanCashBossCardsToCoverContract(players, bossCards))
                return null;

            var declarer = players.FirstOrDefault(p => new WhistBid(p.Bid).IsDeclareBid);
            var isCurrentSeatDeclarer = player.Seat == declarer?.Seat;

            if (isCurrentSeatDeclarer)
                return null;

            var partnerSuit = PartnerIntroducedSuitFromAuctionAndSignal(player, players, cardsPlayed, cardsPlayedInOrder);
            if (partnerSuit == Suit.Unknown || !legalCards.Any(c => EffectiveSuit(c) == partnerSuit))
                return null;

            return legalCards
                .Where(c => EffectiveSuit(c) == partnerSuit)
                .OrderByDescending(RankSort)
                .FirstOrDefault();
        }

        private Card TrySignalGoodSuitOnLead(PlayerBase player, IReadOnlyList<Card> legalCards,
            IReadOnlyList<Card> cardsPlayed, PlayersCollectionBase players, bool isDefending, string cardsPlayedInOrder)
        {
            if (trump != Suit.Unknown || isDefending)
                return null;

            var bossCards = legalCards.Where(c => IsCardHigh(c, cardsPlayed)).ToList();

            if (CanCashBossCardsToCoverContract(players, bossCards))
                return null;

            var declarer = players.FirstOrDefault(p => new WhistBid(p.Bid).IsDeclareBid);
            var isCurrentSeatDeclarer = player.Seat == declarer?.Seat;

            if (isCurrentSeatDeclarer)
                return null;

            var knownCards = cardsPlayed.Concat(new Hand(player.Hand)).ToList();
            var candidateSignals = new List<(Suit suit, int suitCount, int rankForSuitOrdering)>();

            foreach (var suitGroup in legalCards.GroupBy(EffectiveSuit))
            {
                var suit = suitGroup.Key;
                var suitCards = suitGroup.OrderByDescending(RankSort).ToList();
                if (suitCards.Count < 2)
                    continue;

                var top = suitCards[0];
                int? rankForSuitOrdering = null;

                if (IsCardHigh(top, knownCards))
                    rankForSuitOrdering = RankSort(top);
                else if (suitCards.Count >= 3)
                {
                    if (TopCanBeCovered(top, cardsPlayed))
                        rankForSuitOrdering = RankSort(top);
                }

                if (rankForSuitOrdering != null)
                    candidateSignals.Add((suit, suitCards.Count, rankForSuitOrdering.Value));
            }

            var bestSuit = candidateSignals
                .OrderByDescending(c => c.rankForSuitOrdering)
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

            //  return the lowest card from the shortest suit
            return cards.OrderBy(c => cards.Count(c1 => EffectiveSuit(c1) == c.suit)).ThenBy(RankSort).First();
        }

        private Card TryNTSlough(IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed, IReadOnlyList<Card> trick, bool isDefending)
        {
            if (trump != Suit.Unknown)
                return null;

            var firstCardInTrick = trick.FirstOrDefault(IsOfValue);
            if (firstCardInTrick == null)
                return null;

            var trickSuit = EffectiveSuit(firstCardInTrick);
            if (legalCards.Any(c => EffectiveSuit(c) == trickSuit))
                return null;

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

            if (state.trick.Count == 0)
            {
                if (state.trumpSuit == Suit.Unknown)
                {
                    if (legalCards.Any(c => c.suit == Suit.Joker) && legalCards.Any(c => c.suit != Suit.Joker))
                    {
                        legalCards = legalCards.Where(c => c.suit != Suit.Joker).ToList();
                    }

                    var avoidPartnerVoidSuits = SuitRank.stdSuits.Where(s =>
                        players.PartnerIsVoidInSuit(state.player, new Card(s, Rank.Ace), state.cardsPlayed)).ToList();
                    if (avoidPartnerVoidSuits.Count > 0)
                    {
                        var withoutPartnerVoidLead = legalCards.Where(c =>
                            !avoidPartnerVoidSuits.Contains(EffectiveSuit(c)) || IsCardHigh(c, state.cardsPlayed)).ToList();
                        if (withoutPartnerVoidLead.Count > 0)
                            legalCards = withoutPartnerVoidLead;
                    }
                }

                var leadBack = TryLeadBackInPartnerSuit(state.player, legalCards, state.cardsPlayed, players, isDefending, state.cardsPlayedInOrder);
                if (leadBack != null)
                    return leadBack;

                var signal = TrySignalGoodSuitOnLead(state.player, legalCards, state.cardsPlayed, players, isDefending, state.cardsPlayedInOrder);
                if (signal != null)
                    return signal;
            }
            else
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
                isDefending,
                state.cardsPlayedInOrder);
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