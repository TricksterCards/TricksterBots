using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public class FiveHundredBot : BaseBot<FiveHundredOptions>
    {
        public FiveHundredBot(FiveHundredOptions options, Suit trumpSuit) : base(options, trumpSuit)
        {
        }

        public override DeckType DeckType
        {
            get
            {
                switch (options.deckSize)
                {
                    case 43:
                        return options.players == 6 ? DeckType.FiveHundred63Card : DeckType.FiveHundred43Card;
                    case 45:
                        return options.players == 6 ? DeckType.FiveHundred65Card : DeckType.FiveHundred45Card;
                    case 46:
                        return options.players == 6 ? DeckType.FiveHundred66Card : DeckType.FiveHundred46Card;
                    default:
                        return options.players == 6 ? DeckType.FiveHundred63Card : DeckType.FiveHundred43Card;
                }
            }
        }

        private int KittySize => options.deckSize - 40;

        public override BidBase SuggestBid(SuggestBidState<FiveHundredOptions> state)
        {
            var (players, hand, legalBids, player) = (new PlayersCollectionBase(this, state.players), state.hand, state.legalBids, state.player);
            var opponentsBids = players.Opponents(player).Select(p => new FiveHundredBid(p.Bid)).ToList();
            var partnersBids = players.PartnersOf(player).Select(p => new FiveHundredBid(p.Bid)).ToList();
            var playerLastBid = player.BidHistory.Any() ? new FiveHundredBid(player.BidHistory.Last()) : new FiveHundredBid(BidBase.NoBid);
            const int defaultPartnerTricks = 2;

            //  calculate the raw number of tricks we can take with a given trump suit
            var tricksBySuit = FiveHundredBid.suitRank.Keys.ToDictionary(s => s, s => CountTricks(hand, s));
            var hasJoker = hand.Any(c => c.suit == Suit.Joker);

            //  then add adjustments based on hand shape, other players, and the kitty
            foreach (var suit in FiveHundredBid.suitRank.Keys)
            {
                //  deduct one for each number of cards we have below five in trump
                var count = hand.Count(c => EffectiveSuit(c, suit) == suit);
                if (suit != Suit.Unknown && count < 5)
                    tricksBySuit[suit] -= 5 - count;

                //  if opponents bid a suit, stay clear of it
                if (opponentsBids.Any(b => b.IsContractor && b.Suit == suit))
                    tricksBySuit[suit] = 0;

                //  if any partner bid a suit, add our tricks to theirs (minus the two they expect from us)
                else if (partnersBids.Any(b => b.IsContractor && b.Suit == suit) && !(playerLastBid.IsContractor && playerLastBid.Suit == suit))
                    tricksBySuit[suit] += partnersBids.Last(b => b.IsContractor && b.Suit == suit).Tricks - defaultPartnerTricks;

                //  otherwise assume two tricks from partner plus one/two from the kitty (depending on size)
                //  also progressively reduce how many additional tricks we'll assume as our own trick count increases
                else
                    tricksBySuit[suit] += (int)Math.Floor((defaultPartnerTricks + Math.Round(KittySize / 3.0)) * Math.Min(4.0 / tricksBySuit[suit], 1.0));

                //  don't bid above 8 without the joker
                if (tricksBySuit[suit] >= 9 && !hasJoker)
                    tricksBySuit[suit] = 8;
            }

            var matches = legalBids.Where(b =>
            {
                var fhb = new FiveHundredBid(b.value);

                if (!fhb.IsContractor)
                    return false;

                if (fhb.IsLikeNullo)
                    return CanBidNullo(hand, fhb.IsOpen);

                return fhb.Tricks <= tricksBySuit[fhb.Suit];
            }).ToList();

            //  when playing Australian rules, signal with our best suit with our first bid if any partner hasn't passed
            var best = matches.LastOrDefault();
            var bestFHB = best != null ? new FiveHundredBid(best.value) : null;
            var shouldSignal = options.variation == FiveHundredVariation.Australian && players.PartnersOf(player).Any(p => p.Bid != BidBase.Pass) && player.BidHistory.Count == 0;
            var suggestion = shouldSignal && bestFHB != null ? matches.FirstOrDefault(b => new FiveHundredBid(b.value).Suit == bestFHB.Suit) : best;

            return suggestion ?? new BidBase(BidBase.Pass);
        }

        public override List<Card> SuggestDiscard(SuggestDiscardState<FiveHundredOptions> state)
        {
            var (player, hand) = (state.player, state.hand);
            var count = KittySize;
            var theBid = new FiveHundredBid(player.Bid);
            List<Card> cards;

            if (theBid.IsLikeNullo)
            {
                //  throw the highest cards we have (trump in this case hits the jokers)
                cards = hand.Where(IsTrump).OrderBy(RankSort).Take(count).ToList();
                if (cards.Count < count)
                    cards.AddRange(hand.Where(c => !IsTrump(c)).OrderByDescending(RankSort).Take(count - cards.Count));
            }
            else if (theBid.Suit == Suit.Unknown)
            {
                //  in no-trump, throw the lowest cards we have
                //  TODO: try to balance this by keeping cards we need to stop a running suit
                cards = hand.OrderBy(RankSort).Take(count).ToList();
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

        public override Card SuggestNextCard(SuggestCardState<FiveHundredOptions> state)
        {
            var (players, trick, legalCards, cardsPlayed, player, isPartnerTakingTrick, cardTakingTrick) = (new PlayersCollectionBase(this, state.players), state.trick, state.legalCards, state.cardsPlayed,
                state.player, state.isPartnerTakingTrick, state.cardTakingTrick);

            //  if we're leading in a no-trump contract and we have a joker (but not all jokers), remove the joker(s) from our legal cards so we don't suggest a lead of joker unless we have to
            if (trump == Suit.Unknown && trick.Count == 0 && legalCards.Any(c => c.suit == Suit.Joker) && legalCards.Any(c => c.suit != Suit.Joker))
                legalCards = legalCards.Where(c => c.suit != Suit.Joker).ToList();

            var bid = new FiveHundredBid(player.Bid);
            if (bid.IsLikeNullo || options.nulloPlaysPartner && players.PartnersOf(player).Any(p => new FiveHundredBid(p.Bid).IsLikeNullo && player.HandScore + p.HandScore == 0))
                return TryDumpEm(trick, legalCards, players.Count);

            if (players.Opponents(player).Any(p => new FiveHundredBid(p.Bid).IsLikeNullo && p.HandScore == 0))
                return TryBustNullo(player, trick, legalCards, cardsPlayed, players, cardTakingTrick);

            return TryTakeEm(player, trick, legalCards, cardsPlayed, players, isPartnerTakingTrick, cardTakingTrick, !bid.IsContractor && !bid.IsContractorPartner);
        }

        public override List<Card> SuggestPass(SuggestPassState<FiveHundredOptions> state)
        {
            throw new NotImplementedException();
        }

        protected override Suit EffectiveSuit(Card c, Suit trumpSuit)
        {
            //  in notrump, the suit of the Joker can be nominated by a player when led
            if (c.suit == Suit.Joker && trumpSuit == Suit.Unknown && options._jokerSuit != Suit.Joker)
                return options._jokerSuit;

            //  in trump, the joker is included plus the jack in the other suit of the same color as the trump suit
            if (c.suit == Suit.Joker || c.rank == Rank.Jack && c.Color == Card.ColorOfSuit(trumpSuit))
                return trumpSuit;

            //  everything else is natural
            return c.suit;
        }

        protected override int RankSort(Card c, Suit trumpSuit)
        {
            // the black joker is the higher joker in 500
            var aceRank = options.players == 6 ? 17 : (int)Rank.Ace;

            if (c.suit == Suit.Joker && c.rank == Rank.Low)
                return aceRank + 4;

            // the red joker is the lower joker in 500
            if (c.suit == Suit.Joker && c.rank == Rank.High)
                return aceRank + 3;

            if (c.rank == Rank.Jack && c.suit == trumpSuit)
                return aceRank + 2;

            if (c.rank == Rank.Jack && c.Color == Card.ColorOfSuit(trumpSuit))
                return aceRank + 1;

            if (options.players == 6 && c.rank >= Rank.Jack)
            {
                var colorOfTrumpAdjust = c.Color == Card.ColorOfSuit(trumpSuit) ? 1 : 0;

                switch (c.rank)
                {
                    case Rank.Ace:
                        return (int)Rank.Ace + 3;
                    case Rank.King:
                        return (int)Rank.King + 3;
                    case Rank.Queen:
                        return (int)Rank.Queen + 3;
                    case Rank.Jack:
                        return (int)Rank.Jack + 3;
                    case Rank.Thirteen:
                        return 13 + colorOfTrumpAdjust;
                    case Rank.Twelve:
                        return 12 + colorOfTrumpAdjust;
                    case Rank.Eleven:
                        return 11 + colorOfTrumpAdjust;
                }
            }

            //  raise rank of all cards below the jacks in the color of trump to avoid a gap
            if (c.rank < Rank.Jack && c.Color == Card.ColorOfSuit(trumpSuit))
                return (int)c.rank + 1;

            return (int)c.rank;
        }

        protected override int SuitOrder(Suit s)
        {
            return FiveHundredBid.suitOrder[s];
        }

        //  we're trying not to take the trick
        protected Card TryDumpEm(IReadOnlyList<Card> trick, IReadOnlyList<Card> legalCards, int nPlayers, bool takeWithHigh = false)
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

        private bool CanBidNullo(IReadOnlyList<Card> hand, bool isOpen)
        {
            //  never bid nullo with the Joker
            if (hand.Any(c => c.suit == Suit.Joker))
                return false;

            var allowedGaps = isOpen ? 0 : 3;

            IReadOnlyList<Card> deck = DeckBuilder.BuildDeck(DeckType);
            var deckBySuit = SuitRank.stdSuits.ToDictionary(s => s, s => deck.Where(c => EffectiveSuit(c) == s).OrderBy(RankSort).ToList());
            var handBySuit = SuitRank.stdSuits.ToDictionary(s => s, s => hand.Where(c => EffectiveSuit(c) == s).OrderBy(RankSort).ToList());

            //  otherwise tolerate up to three gaps per suit below our cards
            foreach (var suit in SuitRank.stdSuits)
            {
                var cards = handBySuit[suit];
                var lowRank = RankSort(deckBySuit[suit].First());

                foreach (var rank in cards.Select(RankSort))
                {
                    var gaps = rank - lowRank - cards.Count(c => lowRank < RankSort(c) && RankSort(c) < rank);

                    if (gaps > allowedGaps)
                        return false;
                }
            }

            return true;
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

        private Card TryBustNullo(PlayerBase player, IReadOnlyList<Card> trick, IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed, PlayersCollectionBase players, Card cardTakingTrick)
        {
            //  we'll target the first nullo-bidder counter-clockwise from us (so RHO, then across, then LHO)
            var targetNulloBidder = players.Opponents(player).Where(p => new FiveHundredBid(p.Bid).IsLikeNullo && p.HandScore == 0).OrderBy(p => player.Seat - p.Seat).First();
            var targetsPartners = players.PartnersOf(targetNulloBidder); // will be empty in non-partnership games

            if (trick.Count == 0)
            {
                //  we're leading: try to pick something intelligent
                var avoidSuits = new List<Suit>();

                var isTargetsPartnerVoidInTrump = targetsPartners.All(target => players.TargetIsVoidInSuit(player, target, new Card(trump, Rank.Ace), cardsPlayed));

                foreach (var suit in SuitRank.stdSuits)
                {
                    //  avoid leading a suit the nullo bidder is void in
                    if (players.TargetIsVoidInSuit(player, targetNulloBidder, new Card(suit, Rank.Ace), cardsPlayed))
                        avoidSuits.Add(suit);

                    //  also avoid suits the nullo bidder's partner is void in unless the partner is also void in trump
                    if (!isTargetsPartnerVoidInTrump && targetsPartners.All(target => players.TargetIsVoidInSuit(player, target, new Card(suit, Rank.Ace), cardsPlayed)))
                        avoidSuits.Add(suit);
                }

                var preferredLegalCards = legalCards.Where(c => !avoidSuits.Contains(EffectiveSuit(c))).ToList();

                return TryDumpEm(trick, preferredLegalCards.Count > 0 ? preferredLegalCards : legalCards, players.Count);
            }

            //  we're not leading: check the trick to determine what to do
            var targetOffset = (player.Seat - targetNulloBidder.Seat + options.players) % options.players;
            if (trick.Count > targetOffset)
            {
                //  nullo bidder has played and is taking the trick:
                //  try to get under them, but go high if we can't
                if (trick[trick.Count - targetOffset] == cardTakingTrick)
                    return TryDumpEm(trick, legalCards, players.Count, true);

                //  play our highest card, preferring trump; this improves our ability to duck under the nullo bidder later
                return legalCards.Where(IsTrump).OrderByDescending(RankSort).FirstOrDefault() ?? legalCards.OrderByDescending(RankSort).First();
            }

            //  nullo bidder has not played: try to end up under them
            return TryDumpEm(trick, legalCards, players.Count);
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
        private Card TryTakeEm(PlayerBase player, IReadOnlyList<Card> trick, IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed, PlayersCollectionBase players, bool isPartnerTakingTrick, Card cardTakingTrick,
            bool isDefending)
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