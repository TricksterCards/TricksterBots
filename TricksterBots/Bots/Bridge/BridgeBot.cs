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
            return SuggestBid(history, hand);
        }

        public BidBase SuggestBid(BridgeBidHistory history, Hand hand)
        {
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
                    .OrderByDescending(s => s.why.Priority)
                    .ThenByDescending(s => s.why.Points.Min)
                    .ThenByDescending(s => s.why.HandShape.Max(hs => hs.Value.Min));
            }
            else
                //  in other phases, prefer finding the best fit first (prioritizing majors), then higher minimum points
            {
                suggestions = suggestions
                    .OrderByDescending(s => s.why.Priority)
                    .ThenBy(s => s.why.HandShape.Any(hs => IsMajor(hs.Key) && hs.Value.Min > 3) ? 0 : 1)
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
            if (IsOpeningLead(state))
                return SuggestDefensiveLead(state);

            var isDefensiveLead = state.trick.Count == 0 && state.player.Bid == BridgeBid.Defend;
            if (isDefensiveLead)
                return SuggestDefensiveLeadAfterFirstTrick(state);

            var isSecondHandDefensivePlay = state.trick.Count == 1 && state.player.Bid == BridgeBid.Defend;
            if (isSecondHandDefensivePlay)
                return SuggestSecondHandDefensivePlay(state);

            var isThirdHandDefensivePlay = state.trick.Count == 2 && state.player.Bid == BridgeBid.Defend;
            if (isThirdHandDefensivePlay)
                return SuggestThirdHandDefensivePlay(state);

            return TryTakeEm(state);
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

        private PlayerBase GetDeclarer(SuggestCardState<BridgeOptions> state)
        {
            return state.players.Single(p => p.Bid != BidBase.Dummy && p.Bid != BridgeBid.Defend);
        }

        private PlayerBase GetDummy(SuggestCardState<BridgeOptions> state)
        {
            return state.players.Single(p => p.Bid == BidBase.Dummy);
        }

        private int GetNextSeat(SuggestCardState<BridgeOptions> state)
        {
            return GetNextSeatAfter(state.player.Seat, state.players.Count);
        }

        private int GetNextSeatAfter(int seat, int nPlayers)
        {
            return (seat + 1) % nPlayers;
        }

        private PlayerBase GetPartner(SuggestCardState<BridgeOptions> state)
        {
            return state.players.Single(p => p.Seat == (state.player.Seat + 2) % state.players.Count);
        }

        private bool IsOpeningLead(SuggestCardState<BridgeOptions> state)
        {
            return state.cardsPlayedInOrder.Length == 0;
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

        private Card SuggestDefensiveLead(SuggestCardState<BridgeOptions> state)
        {
            if (state.trumpSuit == Suit.Unknown)
                return SuggestDefensiveLeadInNoTrump(state);

            return SuggestDefensiveLeadInSuit(state, state.legalCards);
        }

        private List<List<Card>> GetBrokenSequences(IReadOnlyList<Card> cards, int minLength = 0, int minTopRank = 0, int minRank = 0)
        {
            return GetSequences(cards, minLength, minTopRank, minRank, gap: 1);
        }

        private int FindDealerSeat(SuggestCardState<BridgeOptions> state)
        {
            // since dealer is first to bid
            // if bid counts are equal, dealer is the player whose last bid wasn't pass
            // otherwise dealer is offset by the number of players who bid one more than others
            // e.g. if three players bid twice, and one bid three times, then dealer is to the right of the player whose last bid wasn't pass
            var bids = state.players.Select(p => p.BidHistory.Count);
            var maxBids = bids.Max();
            var nMaxBids = bids.Count(b => b == maxBids);
            var lastNonPassBidderSeat = state.players.Where(p => p.BidHistory.Last() != BidBase.Pass).Select(p => p.Seat).First();
            return (4 + lastNonPassBidderSeat - nMaxBids) % 4;
        }

        private Dictionary<Suit, List<Card>> GetCardsBySuit(IReadOnlyList<Card> cards)
        {
            return cards.GroupBy(EffectiveSuit).ToDictionary(g => g.Key, g => g.OrderByDescending(RankSort).ToList());
        }

        private InterpretedBid.PlayerSummary GetPlayerSummary(SuggestCardState<BridgeOptions> state, int playerSeat)
        {
            var dealerSeat = FindDealerSeat(state);
            var players = new PlayersCollectionBase(this, state.players);
            var history = new BridgeBidHistory(players, dealerSeat);
            var interpretedHistory = InterpretedBid.InterpretHistory(history);
            var firstPlayerBidIndex = (4 + playerSeat - dealerSeat) % 4;
            var nPlayerExtraBids = (interpretedHistory.Count - 1 - firstPlayerBidIndex) / 4;
            var lastPlayerBidIndex = firstPlayerBidIndex + 4 * nPlayerExtraBids;
            return new InterpretedBid.PlayerSummary(interpretedHistory, lastPlayerBidIndex);
        }

        private List<Card> GetCardsInPartnersBestBidSuit(SuggestCardState<BridgeOptions> state, Dictionary<Suit, List<Card>> legalCardsBySuit)
        {
            var partner = GetPartner(state);
            var summary = GetPlayerSummary(state, partner.Seat);
            var legalHandShapes = summary.HandShape.Where(hs => legalCardsBySuit.ContainsKey(hs.Key));
            var bestBidSuitLength = legalHandShapes.Max(hs => hs.Value.Min);

            if (bestBidSuitLength == 0)
                return new List<Card>();

            // TODO: May want to exclude if we're only going off partner opening with 1C or 1D (as those are weak)
            var bestBidSuits = legalHandShapes.Where(hs => hs.Value.Min == bestBidSuitLength).Select(hs => hs.Key);
            return legalCardsBySuit[bestBidSuits.First()];
        }

        private List<Card> GetCardsInPartnersFirstLedSuit(SuggestCardState<BridgeOptions> state, Dictionary<Suit, List<Card>> legalCardsBySuit)
        {
            var partner = GetPartner(state);
            var cardsPlayedByTrick = GetCardsPlayedByTrick(state.cardsPlayedInOrder, state.players.Count);
            var partnersFirstLedTrick = cardsPlayedByTrick.FirstOrDefault(t => t.First().seat == partner.Seat);
            var partnersFirstLedSuit = partnersFirstLedTrick?.First().card.suit;

            if (partnersFirstLedSuit.HasValue && legalCardsBySuit.ContainsKey(partnersFirstLedSuit.Value))
                return legalCardsBySuit[partnersFirstLedSuit.Value];

            return new List<Card>();
        }

        private List<Card> GetCardsInPartnersSuit(SuggestCardState<BridgeOptions> state, Dictionary<Suit, List<Card>> legalCardsBySuit)
        {
            if (IsOpeningLead(state))
                return GetCardsInPartnersBestBidSuit(state, legalCardsBySuit);
            else
                return GetCardsInPartnersFirstLedSuit(state, legalCardsBySuit);
        }

        private Suit GetDummysWeakestSuit(SuggestCardState<BridgeOptions> state)
        {
            if (IsOpeningLead(state))
                return Suit.Unknown;

            var dummy = GetDummy(state);
            var dummyHand = new Hand(dummy.Hand);
            var dummyCardsBySuit = GetCardsBySuit(dummyHand)
                .Where(s => s.Key != state.trumpSuit)
                .OrderBy(s => s.Value.Max(c => c.rank))
                .ThenBy(s => s.Value.Count());

            return dummyCardsBySuit.Select(s => s.Key).First();
        }

        private int CountDeclarersCardsInTrump(SuggestCardState<BridgeOptions> state)
        {
            var declarer = GetDeclarer(state);
            var summary = GetPlayerSummary(state, declarer.Seat);
            var nCardsInTrump = 0;

            // Start with the min count we know from bidding
            if (summary.HandShape.ContainsKey(state.trumpSuit))
                return nCardsInTrump = summary.HandShape[state.trumpSuit].Min;

            if (nCardsInTrump <= 0)
                return 0;

            // Then subtract cards already played
            nCardsInTrump -= GetCardsPlayedInOrder(state.cardsPlayedInOrder).Count(sc => sc.seat == declarer.Seat && sc.card.suit == state.trumpSuit);

            return Math.Max(0, nCardsInTrump);
        }

        private List<Card> GetSureWinners(SuggestCardState<BridgeOptions> state)
        {
            var knownCards = state.legalCards.Concat(state.cardsPlayed);
            var bossCards = state.legalCards.Where(c => IsCardHigh(c, knownCards));
            if (state.trumpSuit != Suit.Unknown)
            {
                var lho = state.players.Single(p => p.Seat == (state.player.Seat + 1) % state.players.Count);
                var rho = state.players.Single(p => p.Seat == (state.player.Seat - 1 + state.players.Count) % state.players.Count);

                // Don't count off suit boss cards if opponents still have trump
                if (!lho.VoidSuits.Contains(state.trumpSuit) || !rho.VoidSuits.Contains(state.trumpSuit))
                    bossCards = bossCards.Where(c => c.suit == state.trumpSuit);
            }

            return bossCards.ToList();
        }

        private List<List<Card>> GetSequences(IReadOnlyList<Card> cards, int minLength = 0, int minTopRank = 0, int minRank = 0, int gap = 0)
        {
            var prevSuit = Suit.Unknown;
            var prevRank = -1;
            var remainingGaps = gap;
            List<List<Card>> sequences = new List<List<Card>>();
            foreach (var card in cards.OrderBy(EffectiveSuit).ThenByDescending(RankSort))
            {
                if (RankSort(card) < minRank)
                {
                    continue;
                }
                if (prevSuit == Suit.Unknown || prevSuit != EffectiveSuit(card) || prevRank > RankSort(card) + 1 + gap)
                {
                    remainingGaps = gap;
                    prevSuit = EffectiveSuit(card);
                    prevRank = RankSort(card);
                    sequences.Add(new List<Card> { card });
                }
                else
                {
                    remainingGaps -= prevRank - (RankSort(card) + 1);
                    prevSuit = EffectiveSuit(card);
                    prevRank = RankSort(card);
                    sequences.Last().Add(card);
                }
            }
            return sequences.Where(seq => seq.Count >= minLength && RankSort(seq.First()) >= minTopRank).ToList();
        }

        private List<Suit> GetUnbidSuits(SuggestCardState<BridgeOptions> state)
        {
            var dealerSeat = FindDealerSeat(state);
            var players = new PlayersCollectionBase(this, state.players);
            var history = new BridgeBidHistory(players, dealerSeat);
            var interpretedHistory = InterpretedBid.InterpretHistory(history);
            var unbidSuits = SuitRank.stdSuits.Where(suit => interpretedHistory.All(b => b.HandShape[suit].Min <= 2));
            return unbidSuits.ToList();
        }

        private Card LeadAceOrLowOrFourthBest(List<Card> cards)
        {
            if (cards[0].rank == Rank.Ace)
                return cards[0];

            return cards.Count > 3 ? cards[3] : cards.Last();
        }

        private Card LeadPartnersBestBidSuit(List<Card> cardsInSuit)
        {
            // Lead partner’s bid suit: high from two, low from three or fourth best from 4 or 5(4th best leads).
            if (cardsInSuit.Count == 2)
                return cardsInSuit.First();
            if (cardsInSuit.Count == 3)
                return cardsInSuit.Last();
            if (cardsInSuit.Count == 4 || cardsInSuit.Count == 5)
                return cardsInSuit[3];

            return cardsInSuit[0];
        }

        private Card LeadPartnersFirstLedSuit(List<Card> cardsInSuit)
        {
            // Returning partner’s initially led suit (either partner):
            // * high-low from two (play high first),
            // * low from three (third best from four).
            // * From four small (9 and under), lead second highest.
            if (cardsInSuit.Count == 2)
                return cardsInSuit.First();
            if (cardsInSuit.Count == 3)
                return cardsInSuit.Last();
            if (cardsInSuit.Count == 4 && cardsInSuit.Max(c => c.rank) < Rank.Ten)
                return cardsInSuit[1];
            if (cardsInSuit.Count == 4)
                return cardsInSuit[2];

            return cardsInSuit[0];
        }

        private Card LeadPartnersSuit(SuggestCardState<BridgeOptions> state, List<Card> cardsInSuit)
        {
            if (IsOpeningLead(state))
                return LeadPartnersBestBidSuit(cardsInSuit);
            else
                return LeadPartnersFirstLedSuit(cardsInSuit);
        }

        private Card SuggestDefensiveLeadInNoTrump(SuggestCardState<BridgeOptions> state)
        {
            // Opening Leads: Notrump

            // If you have a sequence of three or more cards with the highest card the 10 or higher,
            // lead top of that sequence if the suit is four cards or longer.
            var sequences = GetSequences(state.legalCards, minLength: 3, minTopRank: (int)Rank.Ten);
            var cardsBySuit = GetCardsBySuit(state.legalCards);
            var suitCounts = state.legalCards.GroupBy(EffectiveSuit).ToDictionary(g => g.Key, g => g.Count());
            var matches = sequences.Where(seq => suitCounts[seq.First().suit] >= 4);
            if (matches.Any())
                return matches.First().First();

            // If you have a broken sequence(KQ10),
            // lead the top.
            var brokenSequences = GetBrokenSequences(state.legalCards, minLength: 3, minRank: (int)Rank.Ten);
            var nonKJT = brokenSequences.Where(seq => seq[0].rank != Rank.King || seq[1].rank != Rank.Jack || seq[2].rank != Rank.Ten);
            if (nonKJT.Any())
                return nonKJT.First().First();

            // If you have KJT,
            // lead the J
            if (brokenSequences.Any())
                return brokenSequences[0][1];

            // If partner has bid or led
            // lead the suit if you have three or more (same rules as for suit contracts) or we're past the opening lead
            // TODO: If you have two and five or fewer points,
            // lead partner’s suit unless you have a 5 + card suit with 3 honor sequence, lead that
            var cardsInPartnersSuit = GetCardsInPartnersSuit(state, cardsBySuit);
            if (cardsInPartnersSuit.Count >= 3 || (!IsOpeningLead(state) && cardsInPartnersSuit.Any()))
                return LeadPartnersSuit(state, cardsInPartnersSuit);

            // If partner hasn’t bid or led,
            // lead fourth best card from longest / strongest suit.
            // If longest strongest suit has no 10 or higher,
            // lead 2nd highest from that suit
            var maxSuitLength = cardsBySuit.Max(sc => sc.Value.Count());
            var longestSuits = cardsBySuit.Where(sc => sc.Value.Count() == maxSuitLength).Select(sc => sc.Key);
            var bestSuit = longestSuits.First(); // TODO: Tie-break by determining stronger suit

            // Prefer dummy's weakest suit (if after opening lead)
            var dummysWeakestSuit = GetDummysWeakestSuit(state);
            if (dummysWeakestSuit != Suit.Unknown && cardsBySuit.Any(sc => sc.Key == dummysWeakestSuit))
                bestSuit = dummysWeakestSuit;

            var cardsInBestSuit = cardsBySuit[bestSuit];
            if (cardsInBestSuit.First().rank >= Rank.Ten)
                return cardsBySuit[bestSuit].Count() < 4 ? cardsInBestSuit.Last() : cardsInBestSuit[3];
            return cardsBySuit[bestSuit][1];
        }

        private Card SuggestDefensiveLeadInSuit(SuggestCardState<BridgeOptions> state, IReadOnlyList<Card> legalCards)
        {
            // Opening Leads: Suits

            // If you have a 3-card sequence starting with ace, lead that
            var threeCardSequences = GetSequences(legalCards, minLength: 3, minTopRank: (int)Rank.Ace);
            if (threeCardSequences.Any())
                return threeCardSequences.First().First();

            // Lead singleton in suit that isn’t trump
            // Except for singleton A, K, or Q (for now)
            var cardsBySuit = GetCardsBySuit(legalCards);
            var nonTrumpSingletons = cardsBySuit.Where(cs => cs.Key != state.trumpSuit && cs.Value.Count() == 1).Select(cs => cs.Value.First());
            var belowQueenNonTrumpSingletons = nonTrumpSingletons.Where(c => c.rank < Rank.Queen);
            if (belowQueenNonTrumpSingletons.Any())
                return belowQueenNonTrumpSingletons.First();

            // If you have a sequence of two or more cards with the highest card the 10 or higher,
            // lead top of that sequence as long as not doubleton, eg KQ.
            // ignore the suit if it has an Ace
            var twoCardSequences = GetSequences(legalCards, minLength: 2, minTopRank: (int)Rank.Ten);
            var nonDoubletonTwoCardSequences = twoCardSequences.Where(seq => cardsBySuit[EffectiveSuit(seq.First())].Count > 2);
            var nonDoubletonTwoCardSequencesWithoutAce = nonDoubletonTwoCardSequences.Where(seq => cardsBySuit[EffectiveSuit(seq.First())][0].rank != Rank.Ace);
            if (nonDoubletonTwoCardSequencesWithoutAce.Any())
                return nonDoubletonTwoCardSequencesWithoutAce.First().First();

            // Lead partner’s bid/led suit: high from two, low from three or fourth best from 4 or 5(4th best leads).
            // Lead highest of partner’s bid suit, we're okay leading a suit with an Ace here
            var cardsInPartnersSuit = GetCardsInPartnersSuit(state, cardsBySuit);
            if (cardsInPartnersSuit.Any())
                return LeadPartnersSuit(state, cardsInPartnersSuit);

            // Do not lead Ace, unless from AK (OR in partner’s suit, handled above)
            var aceKingSequences = twoCardSequences.Where(seq => seq.First().rank == Rank.Ace);
            if (aceKingSequences.Any())
                return aceKingSequences.First().First();

            // Do not lead suit with ace unless leading ace
            // Preference to lead unbid suits. Low from K or Q better than low from J, generally
            // Generally better to lead from three small than away from an honor
            // Lead low from three card suits, 4th from four or five card suits
            var unbidSuits = GetUnbidSuits(state).Where(s => cardsBySuit.ContainsKey(s) && cardsBySuit[s].Count > 0);

            // Prefer dummy's weakest suit (if after opening lead)
            var dummysWeakestSuit = GetDummysWeakestSuit(state);
            if (dummysWeakestSuit != Suit.Unknown && cardsBySuit.Any(sc => sc.Key == dummysWeakestSuit))
                unbidSuits = new List<Suit>() { dummysWeakestSuit };

            var unbidSuitsWithoutAce = unbidSuits.Where(s => cardsBySuit[s][0].rank != Rank.Ace);
            var unbidSuitsWithThreeSmall = unbidSuitsWithoutAce.Where(s => cardsBySuit[s].Count >= 3 && cardsBySuit[s][0].rank < Rank.Ten);
            if (unbidSuitsWithThreeSmall.Any())
                return LeadAceOrLowOrFourthBest(cardsBySuit[unbidSuitsWithThreeSmall.First()]);
            if (unbidSuitsWithoutAce.Any())
                return LeadAceOrLowOrFourthBest(cardsBySuit[unbidSuitsWithoutAce.First()]);
            if (unbidSuits.Any())
                return LeadAceOrLowOrFourthBest(cardsBySuit[unbidSuits.First()]);

            // TODO Advanced:
            // With a weak hand on your right(ie a preempt),
            // it’s ok to lead more aggressively(low from an honor, or even a random ace)

            return LeadAceOrLowOrFourthBest(cardsBySuit.First().Value);
        }

        private Card SuggestDefensiveLeadAfterFirstTrick(SuggestCardState<BridgeOptions> state)
        {
            // Suit contracts:
            // If dummy has shortness and fewer trumps than declarer,
            // play a trump to prevent ruffing in dummy.
            var dummy = GetDummy(state);
            var dummyHand = new Hand(dummy.Hand);
            if (state.trumpSuit != Suit.Unknown && state.legalCards.Any(c => c.suit == state.trumpSuit))
            {
                var dummyHasTrump = dummyHand.Any(c => c.suit == state.trumpSuit);
                var dummyHasShortness = SuitRank.stdSuits.Any(s => s != state.trumpSuit && dummyHand.Count(c => c.suit == s) == 0);
                if (dummyHasTrump && dummyHasShortness)
                {
                    var nDummyTrump = dummyHand.Count(c => c.suit == state.trumpSuit);
                    var nDeclarerTrump = CountDeclarersCardsInTrump(state);
                    if (nDummyTrump < nDeclarerTrump)
                    {
                        var legalTrump = state.legalCards.Where(c => c.suit == state.trumpSuit).ToList();
                        return SuggestDefensiveLeadInSuit(state, legalTrump);
                    }
                }
            }

            // General principle: if you have a trick that is now good as the defender,
            // and it is the "setting trick" aka the trick to defeat the contract,
            // take it.
            var declarer = GetDeclarer(state);
            var partner = GetPartner(state);
            var level = new DeclareBid(declarer.Bid).level;
            var takenTricks = state.player.CardsTaken.Length / 8 + partner.CardsTaken.Length / 8;
            var neededTricksToSet = 14 - (level + 6) - takenTricks;
            var sureWinners = GetSureWinners(state);
            if (sureWinners.Count() >= neededTricksToSet)
                return TryTakeEm(state);

            // Other defensive rules against both suit and notrump:

            // If dummy has a long side suit that will be good, like AKQxx,
            // try to win tricks quickly
            var knownCards = dummyHand.Concat(state.cardsPlayed).ToList();
            var sideSuits = SuitRank.stdSuits.Where(s => s != state.trumpSuit);
            var goodSideSuits = sideSuits.Where(s =>
            {
                var cardsInSuit = dummyHand.Where(c => c.suit == s);
                var hasEnoughLength = cardsInSuit.Count() >= 5;
                var hasEnoughStrength = cardsInSuit.Count(c => IsCardHigh(c, knownCards)) >= 3;
                return hasEnoughLength && hasEnoughStrength;
            });
            if (goodSideSuits.Any())
                return TryTakeEm(state);

            // TODO: or AQJ10xx (when the player behind dummy does not have K),
            //       try to take tricks quickly

            // Leads after trick 1: same general rules apply (playing touching honors, etc).
            // * Give priority to returning partner's suit (especially in NT),
            // * unless you have a sequence of your own
            // * (if neither works, leading dummy's weakest suit is a good fallback)
            return SuggestDefensiveLead(state);
        }

        private Card SuggestSecondHandDefensivePlay(SuggestCardState<BridgeOptions> state)
        {
            var ledCard = state.trick[0];
            Card coverHonor = null;

            // If an honor is led, cover with an honor (so if they lead the J, cover with the Q)
            if (ledCard.rank >= Rank.Ten)
            {
                var possibleHonors = state.legalCards.Where(c => c.suit == ledCard.suit && c.rank > ledCard.rank);
                coverHonor = possibleHonors.OrderBy(c => c.rank).FirstOrDefault();
            }

            // Exceptions:
            // * If dummy can be seen with the rest of the missing cards
            //   (so don’t cover J if dummy has AK1098, but do cover if dummy has AK102).
            if (coverHonor != null)
            {
                var dummyCardsInSuit = new Hand(GetDummy(state).Hand).Where(c => c.suit == ledCard.suit).ToList();
                var combinedCards = dummyCardsInSuit.Concat(state.legalCards).ToList();
                var knownCardsInSuit = combinedCards.Concat(state.cardsPlayed.Where(c => c.suit == ledCard.suit)).ToList();
                knownCardsInSuit.Add(ledCard);

                if (IsCardHigh(ledCard, knownCardsInSuit) && dummyCardsInSuit.All(c => IsCardHigh(c, knownCardsInSuit)))
                    coverHonor = null;
            }

            // If lead is from dummy and they play an honor
            if (coverHonor != null && state.trickTaker.Bid == BidBase.Dummy && ledCard.rank >= Rank.Ten)
            {
                // Do not cover unless dummy does not also have a touching honor.
                // In other words, from Qxx,
                // do not cover J from dummy if dummy has J10x,
                // but do cover if J is played and dummy has J32.
                var dummyCardsInSuit = new Hand(GetDummy(state).Hand).Where(c => c.suit == ledCard.suit).ToList();
                var dummyHasTouchingHonor = dummyCardsInSuit.Any(c => Math.Abs(c.rank - ledCard.rank) == 1);

                // An exception to the exception: if you have two honors higher than dummy, cover on the first.
                // So if J10x and you have KQ9, you’d cover the J with the Q and then the 10 with the 9)
                var nHonorsHigherThanDummy = state.legalCards.Count(c => c.suit == ledCard.suit && c.rank > ledCard.rank);

                // TODO: Do we need to remember dummy had two touching honors?
                // If so, for how long? Until the end of the hand? Until dummy's next lead?
                // Otherwise JTx and KQ9 looks like Tx and K9 on the second trick.
                // So without memory we'd cover on the 2nd trick too in this case
                // because dummy no longer has a touching honor.

                if (dummyHasTouchingHonor && nHonorsHigherThanDummy < 2)
                    coverHonor = null;
            }

            if (coverHonor != null)
                return coverHonor;

            // Second hand rules: play low generally best.
            return LowestCardFromWeakestSuit(state.legalCards, state.cardsPlayed.Concat(state.legalCards).ToList());
        }

        private bool IsAboveGaps(Card card, int highRank, IEnumerable<Card> cardsPlayed)
        {
            return RankSort(card) == highRank ||
                   cardsPlayed.Count(c => EffectiveSuit(c) == EffectiveSuit(card) && RankSort(c) > RankSort(card)) ==
                   highRank - RankSort(card);
        }

        private Card SuggestThirdHandDefensivePlay(SuggestCardState<BridgeOptions> state)
        {
            // Generally third and fourth hand play as high as necessary.
            // General rule: we're trying to push declarer to have to play their higher cards to win the trick

            var legalCards = state.legalCards.OrderByDescending(c => c.rank).ToList();
            var legalCardsInWinningSuit = legalCards.Where(c => c.suit == state.cardTakingTrick.suit);

            var knownCards = state.cardsPlayed.Concat(legalCards).ToList();
            var dummy = GetDummy(state);
            var dummyHand = new Hand(dummy.Hand);
            var isDummyRHO = state.player.Seat == GetNextSeatAfter(dummy.Seat, state.players.Count);

            if (isDummyRHO)
                knownCards = knownCards.Concat(dummyHand).ToList();

            // If partner is winning with an honor, play low
            if (state.isPartnerTakingTrick && state.cardTakingTrick.rank >= Rank.Ten)
                return LowestCardFromWeakestSuit(legalCards, knownCards);

            // If 4th seat is void, should we play low if partner is winning? Probably.
            var fourthSeatPlayer = state.players.Single(p => p.Seat == GetNextSeat(state));
            var isFourthSeatVoid = fourthSeatPlayer.VoidSuits.Contains(state.cardTakingTrick.suit);
            if (state.isPartnerTakingTrick && isFourthSeatVoid)
                return LowestCardFromWeakestSuit(legalCards, knownCards);

            // If dummy has Qxx, and you play third after dummy plays a low card from KJx, you'd play J.
            // If you have touching honors, play the lower one
            // If J is in dummy, and we're playing 3rd with KQT (or KQTx)
            // * If dummy played J, we'd play Q
            // * Otherwise we'd play T
            // If partner is winning with 9 and dummy did not have J and we're playing 3rd with KQTx, we'd play Q
            var legalNonBossHonorsInWinningSuit = legalCardsInWinningSuit.Where(c => c.rank >= Rank.Ten && !IsCardHigh(c, knownCards));
            var highestLegalNonBossHonorRank = legalNonBossHonorsInWinningSuit.Any() ? (int)legalNonBossHonorsInWinningSuit.Max(c => c.rank) : 0;
            var minimumWinningNonBossHonorAboveGaps = legalNonBossHonorsInWinningSuit.LastOrDefault(c => c.rank > state.cardTakingTrick.rank && IsAboveGaps(c, highestLegalNonBossHonorRank, knownCards));
            if (minimumWinningNonBossHonorAboveGaps != null)
                return minimumWinningNonBossHonorAboveGaps;

            // Third hand play rules: if the second card played will win the trick, play a higher card.
            var minimumWinner = legalCardsInWinningSuit.LastOrDefault(c => c.rank > state.cardTakingTrick.rank);
            if (!state.isPartnerTakingTrick && minimumWinner != null)
                return minimumWinner;

            // Should we trump in if possible (with our lowest trump)? Probably.
            // TODO: If we're trumping in and 4th seat is void (but may still have trump),
            //       should we trump in with an honor if possible? Probably.
            if (!state.isPartnerTakingTrick && legalCards.Any(c => c.suit == state.trumpSuit) && state.cardTakingTrick.suit != state.trumpSuit)
                return legalCards.Last(c => c.suit == state.trumpSuit);

            return LowestCardFromWeakestSuit(legalCards, knownCards);
        }

        private Card TryTakeEm(SuggestCardState<BridgeOptions> state)
        {
            var (players, trick, legalCards, cardsPlayed, player, isPartnerTakingTrick, cardTakingTrick) = (new PlayersCollectionBase(this, state.players), state.trick, state.legalCards, state.cardsPlayed,
                state.player, state.isPartnerTakingTrick, state.cardTakingTrick);

            return TryTakeEm(player, players.PartnerOf(player), trick, legalCards, cardsPlayed, players, isPartnerTakingTrick, cardTakingTrick);
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