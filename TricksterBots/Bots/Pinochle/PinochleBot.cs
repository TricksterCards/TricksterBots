using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public class PinochleBot : BaseBot<PinochleOptions>
    {
        //  use a dictionary for RankSort. Tens take the place of King and King, Queen, and Jack go down one rank
        private static readonly Dictionary<Rank, int> SortByRank = SuitRank.stdRanks.Where(r => r >= Rank.Nine)
            .ToDictionary(r => r, r => r == Rank.Ten ? (int)Rank.King : r == Rank.King || r == Rank.Queen || r == Rank.Jack ? (int)r - 1 : (int)r);

        public PinochleBot(PinochleOptions options, Suit trumpSuit) : base(options, trumpSuit)
        {
        }

        public override DeckType DeckType => options.doubleDeck ? DeckType.Pinochle80 : DeckType.Pinochle48;

        public override BidBase SuggestBid(SuggestBidState<PinochleOptions> state)
        {
            var (legalBids, players, player, hand) = (state.legalBids, new PlayersCollectionBase(this, state.players), state.player, state.hand);

            var passBid = legalBids.FirstOrDefault(b => b.value == BidBase.Pass);

            //  don't bid up your partner if the opponents have passed and we can pass
            var partner = players.PartnerOf(player);
            if (partner != null && PinochleBid.BidIsPoints(partner.Bid) && players.Opponents(player).All(o => PinochleBid.BidIsLikePass(o.Bid)) &&
                passBid != null)
                return passBid;

            //  we're bidding to choose trump if all the legal bids are choose trump bids
            var chooseTrumpRound = legalBids.All(b => new PinochleBid(b).IsTrumpBid || b.value == BidBase.NoBid);

            var winnersBySuit = BossMan.GetWinnersBySuit(this, hand);
            var existingMeldsBySuit = SuitRank.stdSuits.ToDictionary(s => s, s => new PinochleMelder(this, hand, s).GetMeldCards(collapseMelds: false));
            var possibleMeldsBySuit = new Dictionary<Suit, List<PinochleMelds.Meld>>();

            //  only adjust for the kitty when we're in the points round of bidding. we've picked up the kitty by the time we bid the choose trump round.
            //  only adjust for kitty when there's 4 cards in it.
            if (options.kitty == PinochleKitty.FourCard && !chooseTrumpRound || options.passCount > 0)
            {
                foreach (var suitMelds in existingMeldsBySuit)
                {
                    possibleMeldsBySuit[suitMelds.Key] =
                        new PinochleMelder(this, hand, suitMelds.Key).GetPossibleMelds(PinochleMelder.NearMeldsAggressiveness.Medium);
                }
            }
            else
                possibleMeldsBySuit = existingMeldsBySuit;

            //  take a guess on points per trick
            var assumedPointsPerTrick = options.players == 4 ? 25.0 : 20.0;

            bool OkayToBidSuit(Suit s)
            {
                if (options.marriageOrBetter && !possibleMeldsBySuit[s].Any(meld => PinochleMelder.IsMarriageOrBetter(meld.m)))
                    return false;

                if (options.min20InMeld && possibleMeldsBySuit[s].Sum(m => m.Points(options)) < 20 * options.MeldPointsMultiplier)
                    return false;

                return true;
            }

            //  filter the suits we'll bid based on them having enough meld to be trump (in double deck)
            var suitsToBid = SuitRank.stdSuits.Where(OkayToBidSuit).ToArray();

            if (suitsToBid.Length == 0)
            {
                //  if its the points bidding round and no suits qualify and we can pass, pass
                if (!chooseTrumpRound && passBid != null)
                    return passBid;

                // else, consider all the suits
                suitsToBid = SuitRank.stdSuits;
            }

            //  select the best suit as the one with the most combined meld and trick taking points
            var bestSuit = suitsToBid
                .OrderByDescending(s =>
                    possibleMeldsBySuit[s].Sum(m => m.Points(options)) +
                    (winnersBySuit.TryGetValue(s, out var n) ? n.Count : 0) * assumedPointsPerTrick / options.TrickScoreDivisor)
                .ThenByDescending(s => hand.Count(c => EffectiveSuit(c) == s))
                .First();

            if (chooseTrumpRound)
                return legalBids.First(b => new PinochleBid(b).TrumpBidSuit == bestSuit);

            //  compute the tricks I might be able to take
            var myTricks = winnersBySuit.TryGetValue(bestSuit, out var j) ? j.Count : 0;
            var totalTricks = myTricks;

            //  if there's not passing, assume my partner will take their share of the tricks I'm not taking
            var partnerTricks = IsPartnership && options.passCount == 0 ? (CardsDealtPerPlayer(players.Count) - totalTricks) / (players.Count - 1) : 0;
            totalTricks += partnerTricks;

            //  count on getting some winners from the kitty or the pass
            var addlTricks = (options.kitty != PinochleKitty.None ? 1 : 0) + Math.Min(2, options.passCount);
            totalTricks += addlTricks;

            var partnerMeldPoints = IsPartnership ? 3 * options.MeldPointsMultiplier : 0; // assume 30 meld points from partner
            var existingMeldPoints = existingMeldsBySuit[bestSuit].Sum(m => m.Points(options));
            var possibleMeldPoints = possibleMeldsBySuit[bestSuit].Sum(m => m.Points(options));
            var meldPoints = options.passCount > 2 ? possibleMeldPoints : (existingMeldPoints + possibleMeldPoints) / 2;
            var trickPoints = totalTricks * assumedPointsPerTrick / options.TrickScoreDivisor;
            var maybeGet = trickPoints + meldPoints + partnerMeldPoints;
            BidBase bid = PinochleBid.FromPoints((int)Math.Ceiling(maybeGet));

            var legalPointsBids = legalBids.Where(b => PinochleBid.BidIsPoints(b.value));

            //  bid the minimum if we're auction-style or last to bid. bid as high as we can if we're single-bid style
            var bidMin = options.bidStyle == PinochleBidStyle.Auction || players.Where(p => p.Seat != player.Seat).All(p => p.Bid != BidBase.NoBid);
            var pointsBid = bidMin ? legalPointsBids.FirstOrDefault(b => b.value <= bid.value) : legalPointsBids.LastOrDefault(b => b.value <= bid.value);

            //  be sure to cover the "stick the dealer" case when there is no pass bid
            var theBid = pointsBid ?? legalBids.FirstOrDefault(b => b.value == BidBase.Pass) ?? legalBids.First();

#if DEBUG
            theBid.explanation = new BidExplanation
            {
                Description =
                    $"{bestSuit} to {maybeGet:N0}. Winners = {(winnersBySuit.TryGetValue(bestSuit, out var w) ? string.Join(" ", w.Select(c => c.StdNotation)) : "none")}."
                    + $" Thinking {totalTricks} tricks ({myTricks} mine + {partnerTricks} partner + {addlTricks} kitty or passed) for {trickPoints:N0}."
                    + $" Possible melds = [ {string.Join(", ", possibleMeldsBySuit[bestSuit].OrderByDescending(m => m.Points(options)).Select(m => m.m.ToString()))} ] for {possibleMeldPoints}"
                    + $" (current = [ {string.Join(", ", existingMeldsBySuit[bestSuit].OrderByDescending(m => m.Points(options)).Select(m => m.m.ToString()))} ] for {existingMeldPoints})."
                    + $" Plus {partnerMeldPoints} for partner melds."
            };
#endif

            return theBid;
        }

        public override List<Card> SuggestDiscard(SuggestDiscardState<PinochleOptions> state)
        {
            return new PinochleMelder(this, state.hand, trump).SuggestDiscard(options.KittySize);
        }

        public override Card SuggestNextCard(SuggestCardState<PinochleOptions> state)
        {
            return PinochleTakeEm(new PlayersCollectionBase(this, state.players), state.trick, state.legalCards, state.cardsPlayed, state.player,
                state.isPartnerTakingTrick, state.cardTakingTrick);
        }

        public override List<Card> SuggestPass(SuggestPassState<PinochleOptions> state)
        {
            return PinochleBid.BidIsPoints(state.player.Bid)
                ? new PinochleMelder(this, state.hand, trump).SuggestDeclarerPass(options.passCount)
                : new PinochleMelder(this, state.hand, trump).SuggestDeclarerPartnerPass(options.passCount);
        }

        protected override int RankSort(Card c, Suit trumpSuit)
        {
            return SortByRank.TryGetValue(c.rank, out var sort) ? sort : base.RankSort(c, trumpSuit);
        }

        private int CardsDealtPerPlayer(int nPlayers)
        {
            var cardsDealtPerPlayer = DeckBuilder.DeckSize(DeckType) / nPlayers;

            if (options.KittySize == nPlayers)
                cardsDealtPerPlayer -= 1;

            return cardsDealtPerPlayer;
        }

        private IEnumerable<Card> GetCardsKnownToPlayerSeat(int playerSeat)
        {
            return new[] { options._cardsDiscardedBySeat, options._cardsPassedBySeat }.Where(d => d != null && d.ContainsKey(playerSeat))
                .SelectMany(d => d[playerSeat]).ToList();
        }

        private Card PinochleTakeEm(PlayersCollectionBase players, IReadOnlyList<Card> trick, IReadOnlyList<Card> legalCards, IReadOnlyList<Card> cardsPlayed,
            PlayerBase player, bool isPartnerTakingTrick,
            Card cardTakingTrick)
        {
            var bossMan = new BossMan(this, cardsPlayed, new Hand(player.Hand), GetCardsKnownToPlayerSeat(player.Seat), trick,
                players.OpponentsVoidSuits(player));

            if (isPartnerTakingTrick)
            {
                var lastSeat = trick.Count == players.Count - 1;
                var rankSortOfAce = RankSort(new Card(trump, Rank.Ace));

                //  throw partner points (avoiding aces) if we're in the last seat or they're surely or likely to take the trick
                if (lastSeat || bossMan.IsSureWinner(cardTakingTrick, true) || bossMan.IsLikelyWinner(cardTakingTrick, true))
                {
                    return legalCards.OrderBy(IsTrump)
                        .ThenBy(c => RankSort(c) == rankSortOfAce)
                        .ThenByDescending(c => PinochleMelder.CardPoints(options, c))
                        .ThenBy(RankSort)
                        .First();
                }
            }

            var countBySuit = legalCards.GroupBy(EffectiveSuit).ToDictionary(g => g.Key, g => g.Count());

            List<Card> orderedLegalCards;
            if (trick.Count == 0)
            {
                //  when leading, look first at trump (unless the opponents are void in trump) then at shorter suits, then top-down by rank
                var orderedByTrump = bossMan.OpponentsVoidIn(trump)
                    ? legalCards.All(IsTrump) ? legalCards : legalCards.Where(c => !IsTrump(c)).ToList()
                    : legalCards.OrderByDescending(IsTrump).ToList();
                orderedLegalCards = orderedByTrump.OrderBy(c => countBySuit[EffectiveSuit(c)]).ThenBy(EffectiveSuit).ThenByDescending(RankSort).ToList();
            }
            else
            {
                //  if we're following, our legalCards collection is already so specific to led suit and trick-taking rules, just go bottom-up by rank favoring trump
                //  is this assumption correct for Never Head Trick option?
                orderedLegalCards = legalCards.OrderByDescending(IsTrump).ThenBy(RankSort).ToList();
            }

            //  look first for sure winners then likely winners
            var winner = orderedLegalCards.FirstOrDefault(c => bossMan.IsSureWinner(c)) ?? orderedLegalCards.FirstOrDefault(c => bossMan.IsLikelyWinner(c));
            if (winner != null)
                return winner;

            //  we can't likely win the trick and our partner isn't likely taking it, look for what we can dump
            //  select preferring non-trump, lowest valued cards, longest suit, lowest rank
            return legalCards.OrderBy(IsTrump)
                .ThenBy(c => PinochleMelder.CardPoints(options, c))
                .ThenByDescending(c => countBySuit[EffectiveSuit(c)])
                .ThenBy(RankSort)
                .First();
        }
    }
}