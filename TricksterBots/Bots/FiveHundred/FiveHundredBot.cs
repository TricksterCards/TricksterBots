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
                        return options.players == 6 ? DeckType.FiveHundred63Card : options.players == 3 ? DeckType.FiveHundred33Card : DeckType.FiveHundred43Card;
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
            var defaultPartnerTricks = players.Count == 3 ? 1 : 2;
            var estimatedKittyTricks = (int)Math.Floor(KittySize / 3.0);
            var minimumToBid6NT = 6 - defaultPartnerTricks - estimatedKittyTricks;

            //  calculate the raw number of tricks we can take with a given trump suit
            var tricksBySuit = FiveHundredBid.suitRank.Keys.ToDictionary(s => s, s => CountTricks(hand, s));
            var hasJoker = hand.Any(c => c.suit == Suit.Joker);

            //  in Australian: if we haven't bid yet, we have a Joker, and we can bid 6NT, do so (if our hand has at least some strength)
            if (options.variation == FiveHundredVariation.Australian
                && !player.BidHistory.Any()
                && hasJoker
                && tricksBySuit[Suit.Unknown] >= minimumToBid6NT)
            {
                var sixNTBid = new FiveHundredBid(Suit.Unknown, 6);
                var sixNT = legalBids.FirstOrDefault(b => new FiveHundredBid(b.value) == sixNTBid);
                if (sixNT != null)
                    return sixNT;
            }

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

                //  avoid NT without the Joker (unless a partner has bid it)
                else if (suit == Suit.Unknown && !hasJoker && !partnersBids.Any(b => b.IsContractor && b.Suit == suit))
                    tricksBySuit[suit] = 0;

                //  if any partner bid a suit, add our tricks to theirs (minus the two they expect from us)
                else if (partnersBids.Any(b => b.IsContractor && b.Suit == suit) && !(playerLastBid.IsContractor && playerLastBid.Suit == suit))
                    tricksBySuit[suit] += partnersBids.Last(b => b.IsContractor && b.Suit == suit).Tricks - defaultPartnerTricks;

                //  otherwise assume two tricks from partner plus one/two from the kitty (depending on size) unless already estimating 8+
                //  also progressively reduce how many additional tricks we'll assume as our own trick count increases
                else if (tricksBySuit[suit] < 8)
                    tricksBySuit[suit] += (int)Math.Floor((defaultPartnerTricks + estimatedKittyTricks) * Math.Min(4.0 / tricksBySuit[suit], 1.0));

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

            //  only bid as high as necessary to win the game
            var suggestionIsPastGameOver = suggestion != null && BidValue(new FiveHundredBid(suggestion.value)) + player.GameScore >= options.gameOverScore;
            if (suggestion != null && suggestionIsPastGameOver)
            {
                var partners = players.PartnersOf(player);
                var highBid = players.Select(p => new FiveHundredBid(p.Bid)).OrderByDescending(b => b).First();
                var highBidIsPastGameOver = BidValue(highBid) + player.GameScore >= options.gameOverScore;
                var teamHasHighBid = players.Any(p => p.Bid == highBid && (p.Seat == player.Seat || partners.Any(partner => p.Seat == partner.Seat)));
                var canReenterBidding = options.bidAfterPass != BidAfterPass.Never;
                var opponentsHavePassed = players.Opponents(player).All(p => p.Bid == BidBase.Pass);

                //  pass if our team has the high bid, it's past the game over score, and opponents have all passed or we'll get the chance to bid again
                if (teamHasHighBid && highBidIsPastGameOver && (opponentsHavePassed || canReenterBidding))
                    return new BidBase(BidBase.Pass);

                //  otherwise bid, but only as high as needed to win the game
                suggestion = matches.FirstOrDefault(b =>
                {
                    var fhb = new FiveHundredBid(b.value);
                    if (fhb.Suit != bestFHB.Suit)
                        return false;

                    return BidValue(fhb) + player.GameScore >= options.gameOverScore;
                });
            }

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
            var (players, trick, legalCards, cardsPlayed, player, isPartnerTakingTrick, cardTakingTrick, trickTaker) = (new PlayersCollectionBase(this, state.players), state.trick, state.legalCards, state.cardsPlayed,
                state.player, state.isPartnerTakingTrick, state.cardTakingTrick, state.trickTaker);

            //  if we're leading in a no-trump contract and we have a joker (but not all jokers), remove the joker(s) from our legal cards so we don't suggest a lead of joker unless we have to
            if (trump == Suit.Unknown && trick.Count == 0 && legalCards.Any(c => c.suit == Suit.Joker) && legalCards.Any(c => c.suit != Suit.Joker))
                legalCards = legalCards.Where(c => c.suit != Suit.Joker).ToList();

            if (IsNulloPlayer(player))
                return TryDumpEm(trick, legalCards, players.Count);

            if (players.Opponents(player).Any(p => new FiveHundredBid(p.Bid).IsLikeNullo))
                return TryBustNullo(player, trick, legalCards, cardsPlayed, players, trickTaker);

            // 3-player only: team up with the other opponent if declarer is in the lead (unless we're the declarer)
            var effectivePlayers = players;
            var isEffectivePartnerTakingTrick = isPartnerTakingTrick;
            if (players.Count == 3 && trickTaker != null)
            {
                var declarer = players.Single(p => p.Bid != FiveHundredBid.NotContractorBid);
                if (player.Seat != declarer.Seat && trickTaker.Seat != declarer.Seat && declarer.GameScore == players.Max(p => p.GameScore))
                {
                    var effectivePartners = players.Where(p => p.Seat != declarer.Seat && p.Seat != player.Seat);
                    effectivePlayers = new EffectivePartnerPlayersCollection(this, players, effectivePartners);
                    isEffectivePartnerTakingTrick = true;
                }
            }

            var bid = new FiveHundredBid(player.Bid);
            return TryTakeEm(player, trick, legalCards, cardsPlayed, effectivePlayers, isEffectivePartnerTakingTrick, cardTakingTrick, !bid.IsContractor && !bid.IsContractorPartner);
        }

        public override List<Card> SuggestPass(SuggestPassState<FiveHundredOptions> state)
        {
            throw new NotImplementedException();
        }

        public override int SuitOrder(Suit s)
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
            var nJokers = hand.Count(c => c.suit == Suit.Joker);

            if (trumpSuit == Suit.Unknown)
            {
                //  in no-trump, count 1 trick for each joker
                tricks += nJokers;
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
            var remainingJokers = nJokers;
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

                    var usingJokerAsStopper = false;
                    if (gaps > below)
                    {
                        //  Jokers can be a stopper for any suit in NT (but can only be used to stop once)
                        if (trumpSuit == Suit.Unknown && remainingJokers > 0 && gaps - 1 <= below)
                        {
                            usingJokerAsStopper = true;
                            remainingJokers--;
                        }
                        else
                            break;
                    }

                    tricks++;
                    hasStopper = true;
                    nextHighestRank = targetRank - 1;
                    cards.Remove(targetCard);
                    if (!usingJokerAsStopper)
                        cards.RemoveRange(0, gaps);
                }

                //  if we're looking at no-trump and we don't have a stopper in all suits, bail
                if (trumpSuit == Suit.Unknown && !hasStopper && nJokers == 0)
                    return 0;
            }

            return tricks;
        }

        private static bool IsNulloPlayer(PlayerBase player)
        {
            if (!player.IsActivelyPlaying)
                return false;

            //  active nullo players are those that either bid nullo (can be multiple if playing solo)
            //  OR partners of nullo bidders who are still in the game (will be face-up dummy hands played by the nullo bidder)
            return player.Bid == BidBase.Dummy || new FiveHundredBid(player.Bid).IsLikeNullo;
        }

        private int BidValue(FiveHundredBid theBid)
        {
            if (!theBid.IsContractor)
                return 0;

            if (theBid.IsLikeNullo)
                return theBid.IsOpen ? options.OpenNulloPoints : options.NulloPoints;

            //  Avondale scoring from https://en.wikipedia.org/wiki/500_(card_game)
            return 20 + FiveHundredBid.suitRank[theBid.Suit] * 20 + (theBid.Tricks - 6) * 100;
        }

        private Card TryBustNullo(PlayerBase player, IReadOnlyList<Card> trick, IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed, PlayersCollectionBase players, PlayerBase trickTaker)
        {
            var nulloPlayers = players.Where(IsNulloPlayer).ToList();

            if (trick.Count == 0)
            {
                //  we're leading: try to pick something intelligent
                var avoidSuits = new List<Suit>();

                foreach (var suit in SuitRank.stdSuits)
                {
                    if (nulloPlayers.Any(p => players.TargetIsVoidInSuit(player, p, suit, cardsPlayed)))
                        avoidSuits.Add(suit);
                }

                var knownCards = cardsPlayed.Concat(legalCards).ToList();
                var preferredLegalCards = legalCards
                    .Where(c => !avoidSuits.Contains(EffectiveSuit(c))) //  avoid leading a suit any nullo player is void in
                    .Where(c => !IsCardHigh(c, knownCards)) //  also avoid leading a card that is known to be high
                    .ToList();

                //  fall back to avoiding cards known to be high, but allowing suits where nullo is void
                //  (gives partner a chance to take the lead)
                if (preferredLegalCards.Count == 0)
                    preferredLegalCards = legalCards.Where(c => !IsCardHigh(c, knownCards)).ToList();

                if (preferredLegalCards.Count > 0)
                    legalCards = preferredLegalCards;

                return TryDumpEm(trick, legalCards, players.Count);
            }

            //  if we're not leading, but a nullo player has yet to play and isn't void in the led suit, play low
            if (nulloPlayers.Any(p => p.Hand.Length == player.Hand.Length && !players.TargetIsVoidInSuit(player, p, trick[0], cardsPlayed)))
                return TryDumpEm(trick, legalCards, players.Count);

            //  if a nullo player is taking the trick, try to get under them (but go high if we can't)
            if (nulloPlayers.Any(p => p.Seat == trickTaker.Seat))
                return TryDumpEm(trick, legalCards, players.Count, takeWithHigh: true);

            //  play our highest card, preferring trump; this improves our ability to duck under nullo players later
            return legalCards.Where(IsTrump).OrderByDescending(RankSort).FirstOrDefault() ?? legalCards.OrderByDescending(RankSort).First();
        }

        private class EffectivePartnerPlayersCollection : PlayersCollectionBase
        {
            private readonly PlayerBase[] effectivePartners;

            public EffectivePartnerPlayersCollection(IBaseBot bot, IEnumerable<PlayerBase> players, IEnumerable<PlayerBase> effectivePartners) : base(bot, players)
            {
                this.effectivePartners = effectivePartners.ToArray();
            }

            protected override bool IsPartnership => true;

            public override PlayerBase[] PartnersOf(PlayerBase player)
            {
                return this.effectivePartners;
            }
        }
    }
}