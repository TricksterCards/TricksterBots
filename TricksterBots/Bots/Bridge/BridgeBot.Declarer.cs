using System;
using System.Collections.Generic;
using System.Linq;
using Trickster.cloud;

namespace Trickster.Bots
{
    public partial class BridgeBot
    {
        private Card SuggestDeclarerPlay(SuggestCardState<BridgeOptions> state, AuctionInfo auction)
        {
            var play = DeclarerPlay.TryCreate(this, state, auction);
            var card = play?.SuggestCard();
            var legalCard = card == null ? null : state.legalCards.FirstOrDefault(c => c.suit == card.suit && c.rank == card.rank);
            return legalCard ?? TryTakeEm(state);
        }

        //  Card play for the declaring side (declarer and dummy), following common declarer-play practices.
        //  Bots are stateless, so every decision is re-derived from the visible cards (our two hands, cards played,
        //  and the auction) so declarer's lead and dummy's follow-up (or vice versa) stay consistent.
        private sealed class DeclarerPlay
        {
            private const int NoSeat = -1;
            private static readonly List<Card> NoCards = new List<Card>();

            private readonly BridgeBot bot;
            private readonly AuctionInfo auction;
            private readonly PlayerBase me;
            private readonly PlayerBase partner;
            private readonly PlayerBase lho;
            private readonly PlayerBase rho;
            private readonly bool isDeclarer;
            private readonly Suit trump;
            private readonly IReadOnlyList<Card> trick;
            private readonly int leaderSeat;
            private readonly Card cardTakingTrick;
            private readonly bool isPartnerTakingTrick;
            private readonly int tricksNeeded;
            private readonly int tricksToSpare;
            private readonly List<SeatAndCard> playedInOrder;
            private readonly Dictionary<Suit, List<Card>> own;
            private readonly Dictionary<Suit, List<Card>> pard;
            private readonly Dictionary<Suit, List<Card>> outstanding;
            private readonly Dictionary<Suit, int> topTricksCache = new Dictionary<Suit, int>();
            private readonly Dictionary<Suit, int> estimatedTricksCache = new Dictionary<Suit, int>();
            private readonly Dictionary<int, int> minPointsBySeat = new Dictionary<int, int>();

            private DeclarerPlay(BridgeBot bot, SuggestCardState<BridgeOptions> state, AuctionInfo auction, PlayerBase partner, PlayerBase declarer,
                List<Card> ownCards, List<Card> pardCards)
            {
                this.bot = bot;
                this.auction = auction;
                this.partner = partner;
                me = state.player;
                isDeclarer = me.Seat == declarer.Seat;
                lho = state.players.First(p => p.Seat == (me.Seat + 1) % 4);
                rho = state.players.First(p => p.Seat == (me.Seat + 3) % 4);
                trump = state.trumpSuit;
                trick = state.trick ?? NoCards;
                leaderSeat = (me.Seat + 4 - trick.Count) % 4;
                cardTakingTrick = state.cardTakingTrick;
                isPartnerTakingTrick = state.isPartnerTakingTrick;
                playedInOrder = bot.GetCardsPlayedInOrder(state.cardsPlayedInOrder);

                var level = new DeclareBid(declarer.Bid).level;
                var ourTricks = (me.CardsTaken.Length + partner.CardsTaken.Length) / 8;
                var theirTricks = (lho.CardsTaken.Length + rho.CardsTaken.Length) / 8;
                tricksNeeded = level + 6 - ourTricks;
                tricksToSpare = 13 - (level + 6) - theirTricks;

                own = GroupBySuit(ownCards);
                pard = GroupBySuit(pardCards);

                var known = ownCards.Concat(pardCards).Concat(state.cardsPlayed ?? NoCards).Concat(trick).ToList();
                outstanding = new Dictionary<Suit, List<Card>>();
                foreach (var suit in SuitRank.stdSuits)
                {
                    var cards = new List<Card>();
                    for (var rank = (int)Rank.Ace; rank >= (int)Rank.Two; rank--)
                        if (!known.Any(c => c.suit == suit && (int)c.rank == rank))
                            cards.Add(new Card(suit, (Rank)rank));
                    outstanding[suit] = cards;
                }
            }

            public static DeclarerPlay TryCreate(BridgeBot bot, SuggestCardState<BridgeOptions> state, AuctionInfo auction)
            {
                if (state.players.Count != 4)
                    return null;

                var me = state.player;
                var partner = state.players.FirstOrDefault(p => p.Seat == (me.Seat + 2) % 4);
                var declarer = state.players.FirstOrDefault(p => p.Bid != BidBase.Dummy && p.Bid != BridgeBid.Defend);
                if (partner == null || declarer == null || !DeclareBid.Is(declarer.Bid))
                    return null;

                //  we need to see both of our side's hands to plan the play
                var players = new PlayersCollectionBase(bot, state.players);
                if (!bot.CanSeeHand(players, me, partner))
                    return null;

                var ownCards = KnownCards(me.Hand);
                var pardHand = new Hand(partner.Hand ?? string.Empty);
                if (ownCards.Count == 0 || pardHand.Any(c => !IsKnownCard(c)))
                    return null;

                return new DeclarerPlay(bot, state, auction, partner, declarer, ownCards, KnownCards(partner.Hand));
            }

            public Card SuggestCard()
            {
                switch (trick.Count)
                {
                    case 0:
                        return Lead();
                    case 1:
                        return SecondHand();
                    case 2:
                        return ThirdHand();
                    case 3:
                        return FourthHand();
                    default:
                        return null;
                }
            }

            #region Basic helpers

            private bool IsNoTrump => trump == Suit.Unknown;

            private bool OpponentsHaveTrump => !IsNoTrump && Out(trump).Count > 0;

            private bool AmMasterTrumpHand => Own(trump).Count > Pard(trump).Count || Own(trump).Count == Pard(trump).Count && isDeclarer;

            private static bool IsKnownCard(Card c)
            {
                return c.suit != Suit.Unknown && c.rank >= Rank.Two && c.rank <= Rank.Ace;
            }

            private static List<Card> KnownCards(string hand)
            {
                var cards = new List<Card>();
                foreach (var card in new Hand(hand ?? string.Empty).Where(IsKnownCard))
                    if (!cards.Any(c => c.suit == card.suit && c.rank == card.rank))
                        cards.Add(card);
                return cards;
            }

            private Dictionary<Suit, List<Card>> GroupBySuit(IEnumerable<Card> cards)
            {
                return cards.GroupBy(S).ToDictionary(g => g.Key, g => g.OrderByDescending(R).ToList());
            }

            private int R(Card c)
            {
                return bot.RankSort(c);
            }

            private Suit S(Card c)
            {
                return bot.EffectiveSuit(c);
            }

            private List<Card> Own(Suit s)
            {
                return own.TryGetValue(s, out var cards) ? cards : NoCards;
            }

            private List<Card> Pard(Suit s)
            {
                return pard.TryGetValue(s, out var cards) ? cards : NoCards;
            }

            private List<Card> Out(Suit s)
            {
                return outstanding.TryGetValue(s, out var cards) ? cards : NoCards;
            }

            private List<Card> Combined(Suit s)
            {
                return Own(s).Concat(Pard(s)).OrderByDescending(R).ToList();
            }

            private int MaxLength(Suit s)
            {
                return Math.Max(Own(s).Count, Pard(s).Count);
            }

            private IEnumerable<Suit> SideSuits => SuitRank.stdSuits.Where(s => s != trump);

            //  number of opponents' cards that can beat this card
            private int Beaters(Card c)
            {
                return Out(S(c)).Count(o => R(o) > R(c));
            }

            private bool IsWinner(Card c)
            {
                return Beaters(c) == 0;
            }

            //  two of our cards are equivalent if the opponents hold no card ranked between them
            private bool AreEquivalent(Card a, Card b)
            {
                if (S(a) != S(b))
                    return false;

                var lo = Math.Min(R(a), R(b));
                var hi = Math.Max(R(a), R(b));
                return !Out(S(a)).Any(o => R(o) > lo && R(o) < hi);
            }

            private Card LowestEquivalent(Card card, IEnumerable<Card> cards)
            {
                return cards.Where(c => AreEquivalent(c, card)).OrderBy(R).First();
            }

            private Card HighestEquivalent(Card card, IEnumerable<Card> cards)
            {
                return cards.Where(c => AreEquivalent(c, card)).OrderByDescending(R).First();
            }

            private bool IsOpponent(int seat)
            {
                return seat == lho.Seat || seat == rho.Seat;
            }

            private PlayerBase OpponentAt(int seat)
            {
                return seat == lho.Seat ? lho : seat == rho.Seat ? rho : null;
            }

            private bool IsKnownVoid(PlayerBase p, Suit s)
            {
                return Out(s).Count == 0 || p.VoidSuits.Contains(s);
            }

            private int SeatOfTrickCard(int index)
            {
                return (leaderSeat + index) % 4;
            }

            #endregion

            #region Trick counting

            //  tricks we can take in this suit without losing the lead
            private int TopTricks(Suit s)
            {
                if (topTricksCache.TryGetValue(s, out var cached))
                    return cached;

                var combined = Combined(s);
                var outS = Out(s);
                var tricks = outS.Count == 0 ? MaxLength(s) : Math.Min(combined.Count(c => R(c) > R(outS[0])), MaxLength(s));
                topTricksCache[s] = tricks;
                return tricks;
            }

            //  tricks we expect to take in this suit if we play it out ourselves (assuming an even split)
            private int EstimatedTricks(Suit s)
            {
                if (estimatedTricksCache.TryGetValue(s, out var cached))
                    return cached;

                var tricks = SimulateSuit(s, MaxLength(s)).won;
                estimatedTricksCache[s] = tricks;
                return tricks;
            }

            private int TotalTopTricks()
            {
                return SuitRank.stdSuits.Sum(TopTricks);
            }

            private int SideTopTricks()
            {
                return SideSuits.Sum(TopTricks);
            }

            //  Plays the suit out from the top for a number of rounds: we win a round when our best card beats theirs,
            //  otherwise they win it with their best card. Each side follows with its lowest card(s).
            private (int won, int lost) SimulateSuit(Suit s, int rounds)
            {
                var ours = Combined(s).Select(R).ToList();
                var theirs = Out(s).Select(R).ToList();
                var shortLength = Math.Min(Own(s).Count, Pard(s).Count);
                int won = 0, lost = 0;

                for (var round = 0; round < rounds && ours.Count > 0; round++)
                {
                    var bothHandsFollow = round < shortLength;
                    if (theirs.Count == 0 || ours[0] > theirs[0])
                    {
                        won++;
                        ours.RemoveAt(0);
                        RemoveLowest(ours, bothHandsFollow ? 1 : 0);
                        RemoveLowest(theirs, 2);
                    }
                    else
                    {
                        lost++;
                        theirs.RemoveAt(0);
                        RemoveLowest(theirs, 1);
                        RemoveLowest(ours, bothHandsFollow ? 2 : 1);
                    }
                }

                return (won, lost);
            }

            private static void RemoveLowest(List<int> ranks, int count)
            {
                for (var i = 0; i < count && ranks.Count > 0; i++)
                    ranks.RemoveAt(ranks.Count - 1);
            }

            private int TrumpLosers()
            {
                return OpponentsHaveTrump ? MaxLength(trump) - EstimatedTricks(trump) : 0;
            }

            //  losers the opponents can cash in this suit before one of our hands runs out and can ruff
            private int QuickLosers(Suit s)
            {
                if (Own(s).Count == 0 || Pard(s).Count == 0)
                    return 0;

                return SimulateSuit(s, Math.Min(Own(s).Count, Pard(s).Count)).lost;
            }

            private int RoundsToDrawTrump()
            {
                var n = Out(trump).Count;
                if (n == 0)
                    return 0;

                if (lho.VoidSuits.Contains(trump) || rho.VoidSuits.Contains(trump))
                    return n;

                return (n + 1) / 2;
            }

            //  side-suit losers in the long-trump hand that can be ruffed in the short-trump hand
            private int RuffableLosers(Suit s)
            {
                var master = AmMasterTrumpHand ? Own(s) : Pard(s);
                var shortHand = AmMasterTrumpHand ? Pard(s) : Own(s);
                if (s == trump || shortHand.Count >= master.Count)
                    return 0;

                var losers = master.Count - Math.Min(master.Count, EstimatedTricks(s));
                return Math.Min(losers, master.Count - shortHand.Count);
            }

            private int TotalRuffableLosers()
            {
                var shortTrumps = AmMasterTrumpHand ? Pard(trump).Count : Own(trump).Count;
                return Math.Min(shortTrumps, SideSuits.Sum(RuffableLosers));
            }

            #endregion

            #region Inferences

            private bool IsMiniBridge => bot.options.variation == BridgeVariation.Mini;

            private SeatAndCard OpeningLead => playedInOrder.Count > 0 && IsOpponent(playedInOrder[0].seat) ? playedInOrder[0] : null;

            private int MinPoints(PlayerBase p)
            {
                if (!minPointsBySeat.TryGetValue(p.Seat, out var points))
                {
                    points = IsMiniBridge ? 0 : GetPlayerSummary(auction, p.Seat).Points.Min;
                    minPointsBySeat[p.Seat] = points;
                }

                return points;
            }

            //  the opponent most likely to hold an outstanding card (or NoSeat if we can't tell)
            private int LikelyHolder(Card card)
            {
                var s = S(card);
                var lhoVoid = lho.VoidSuits.Contains(s);
                var rhoVoid = rho.VoidSuits.Contains(s);
                if (lhoVoid != rhoVoid)
                    return lhoVoid ? rho.Seat : lho.Seat;

                //  honor leads are top of a sequence: K/Q/J/T promise the next card down; Q/J/T deny the next card up
                var lead = OpeningLead;
                if (lead != null && S(lead.card) == s && lead.card.rank >= Rank.Ten && lead.card.rank <= Rank.King)
                {
                    if (R(card) == R(lead.card) - 1)
                        return lead.seat;

                    if (R(card) == R(lead.card) + 1 && lead.card.rank != Rank.King)
                        return lead.seat == lho.Seat ? rho.Seat : lho.Seat;
                }

                //  place missing honors with the opponent who has shown values in the auction
                if (card.rank >= Rank.Jack && !IsMiniBridge)
                {
                    var lhoPoints = MinPoints(lho);
                    var rhoPoints = MinPoints(rho);
                    if (lhoPoints - rhoPoints >= 5)
                        return lho.Seat;
                    if (rhoPoints - lhoPoints >= 5)
                        return rho.Seat;
                }

                return NoSeat;
            }

            //  in notrump, the opponent who can run an established suit if they gain the lead
            private int DangerSeat()
            {
                var lead = OpeningLead;
                if (!IsNoTrump || lead == null)
                    return NoSeat;

                var s = S(lead.card);
                var leader = OpponentAt(lead.seat);
                if (Out(s).Count == 0 || TopTricks(s) > 0 || leader.VoidSuits.Contains(s))
                    return NoSeat;

                return lead.seat;
            }

            //  with enough top cards and few cards outstanding, playing for the drop beats a finesse
            //  ("eight ever, nine never" for a missing queen; "eleven ever, ten never" for a missing king)
            private bool PreferDrop(Suit s, Card missing)
            {
                var oursAtStart = Combined(s).Concat(TrickCards(ours: true).Where(c => S(c) == s)).ToList();
                var mastersAbove = oursAtStart.Count(c => R(c) > R(missing));
                var outAtStart = Out(s).Count + TrickCards(ours: false).Count(c => S(c) == s);
                return mastersAbove > 0 && outAtStart <= 2 * mastersAbove;
            }

            private IEnumerable<Card> TrickCards(bool ours)
            {
                for (var i = 0; i < trick.Count; i++)
                    if (IsOpponent(SeatOfTrickCard(i)) != ours)
                        yield return trick[i];
            }

            //  the highest card in a holding that is beaten only by a single outstanding card (the card to finesse or lead toward)
            private Card FinesseCard(List<Card> cards)
            {
                return cards.FirstOrDefault(c => Beaters(c) == 1);
            }

            private Card MissingAbove(Card card)
            {
                return Out(S(card)).LastOrDefault(o => R(o) > R(card));
            }

            #endregion

            #region Leading

            private Card Lead()
            {
                if (!IsNoTrump)
                {
                    var card = SuitContractLead();
                    if (card != null)
                        return card;
                }

                if (TotalTopTricks() >= tricksNeeded)
                {
                    var cash = CashOut();
                    if (cash != null)
                        return cash;
                }

                return Develop() ?? CashOut() ?? ExitCard();
            }

            private Card SuitContractLead()
            {
                if (!OpponentsHaveTrump)
                    return RuffInShortHandAfterDrawingTrump();

                var shortTrumps = AmMasterTrumpHand ? Pard(trump).Count : Own(trump).Count;
                var baseTricks = SideTopTricks() + EstimatedTricks(trump);
                var ruffable = TotalRuffableLosers();
                var ruffsNeeded = Math.Min(ruffable, tricksNeeded - baseTricks);

                //  ruffs still available in the short-trump hand after it follows to the rounds needed to draw trump
                var drawTrumpTricks = baseTricks + Math.Max(0, Math.Min(ruffable, shortTrumps - RoundsToDrawTrump()));
                var crossRuff = CrossRuffCard(drawTrumpTricks);
                if (crossRuff != null)
                    return crossRuff;

                //  take needed ruffs in the short-trump hand before drawing trump would use up its trumps
                if (ruffsNeeded > 0 && shortTrumps - RoundsToDrawTrump() < ruffsNeeded)
                {
                    var ruff = RuffFirstCard();
                    if (ruff != null)
                        return ruff;
                }

                //  if we must give up the lead in trump, first discard losers the opponents could cash
                var trumpLosers = TrumpLosers();
                if (trumpLosers > 0 && trumpLosers + SideSuits.Sum(QuickLosers) > tricksToSpare)
                {
                    var discard = QuickDiscardCard();
                    if (discard != null)
                        return discard;
                }

                if (ShouldDrawTrump())
                    return DrawTrumpCard();

                return null;
            }

            private bool ShouldDrawTrump()
            {
                var outTrump = Out(trump);
                var ourTrump = Combined(trump);
                if (outTrump.Count == 0 || ourTrump.Count == 0)
                    return false;

                //  don't draw trump when the opponents have as many as we do (we'd lose control)
                if (ourTrump.Count <= outTrump.Count)
                    return false;

                //  don't spend two of our trumps to draw a lone master trump (or trumps that all beat ours)
                return outTrump.Any(o => R(o) < R(ourTrump[0]));
            }

            private Card DrawTrumpCard()
            {
                if (Own(trump).Count == 0)
                    return CrossToPartner(Suit.Unknown, allowTrump: false);

                var plan = SuitPlan(trump);
                return plan.card ?? Own(trump).Last();
            }

            private Card RuffFirstCard()
            {
                var shortHandSuits = SideSuits
                    .Where(s => RuffableLosers(s) > 0)
                    .OrderBy(s => (AmMasterTrumpHand ? Pard(s) : Own(s)).Count)
                    .ThenByDescending(RuffableLosers);

                foreach (var s in shortHandSuits)
                {
                    var shortHand = AmMasterTrumpHand ? Pard(s) : Own(s);
                    if (shortHand.Count == 0)
                    {
                        //  lead a loser from the long-trump hand for the short-trump hand to ruff
                        if (AmMasterTrumpHand && Own(s).Count > 0)
                            return Own(s).Last();

                        //  short-trump hand is on lead: get over to the long-trump hand with a side entry
                        if (!AmMasterTrumpHand)
                        {
                            var cross = CrossToPartner(s, allowTrump: false);
                            if (cross != null)
                                return cross;
                        }
                    }
                    else if (Own(s).Count > 0)
                    {
                        //  void the short-trump hand: cash our winners first, otherwise give up a round
                        return (TopTricks(s) > 0 ? CashCard(s) : null) ?? Own(s).Last();
                    }
                }

                return null;
            }

            private Card RuffInShortHandAfterDrawingTrump()
            {
                if (!AmMasterTrumpHand || Pard(trump).Count == 0 || TotalTopTricks() >= tricksNeeded)
                    return null;

                var suit = SideSuits.Where(s => Pard(s).Count == 0 && Own(s).Any(c => !IsWinner(c)))
                    .Select(s => (Suit?)s).FirstOrDefault();
                return suit.HasValue ? Own(suit.Value).Last() : null;
            }

            private Card QuickDiscardCard()
            {
                foreach (var s in SideSuits.Where(s => Own(s).Count > 0 || Pard(s).Count > 0).OrderByDescending(TopTricks))
                {
                    var top = TopTricks(s);
                    foreach (var discarding in new[] { true, false })
                    {
                        //  the hand that discards (Y) must be shorter than the winners in this suit,
                        //  and must hold quick losers in another suit where it's the shorter hand
                        var y = discarding ? Own(s) : Pard(s);
                        if (top <= y.Count)
                            continue;

                        var hasLosersToDiscard = SideSuits.Any(t => t != s && QuickLosers(t) > 0 &&
                                                                    (discarding ? Own(t).Count <= Pard(t).Count : Pard(t).Count <= Own(t).Count));
                        if (!hasLosersToDiscard)
                            continue;

                        var cash = CashCard(s);
                        if (cash != null)
                            return cash;
                    }
                }

                return null;
            }

            private Card CrossRuffCard(int drawTrumpTricks)
            {
                if (Own(trump).Count < 2 || Pard(trump).Count < 2)
                    return null;

                var iRuff = SideSuits.Where(s => Own(s).Count == 0).Sum(s => Pard(s).Count(c => !IsWinner(c)));
                var partnerRuffs = SideSuits.Where(s => Pard(s).Count == 0).Sum(s => Own(s).Count(c => !IsWinner(c)));
                if (iRuff == 0 || partnerRuffs == 0)
                    return null;

                var crossRuffTricks = Math.Min(Own(trump).Count, iRuff) + Math.Min(Pard(trump).Count, partnerRuffs);
                var sideTop = SideTopTricks();
                if (sideTop + crossRuffTricks < tricksNeeded || drawTrumpTricks >= tricksNeeded || sideTop + crossRuffTricks <= drawTrumpTricks)
                    return null;

                //  cash side winners before cross-ruffing so the opponents can't discard and ruff them later
                //  (first where both hands follow, then where partner is void and can discard a loser)
                foreach (var s in SideSuits.Where(s => Own(s).Count > 0 && Pard(s).Count > 0 && TopTricks(s) > 0))
                {
                    var cash = CashCard(s);
                    if (cash != null)
                        return cash;
                }

                var winnerToCash = SideSuits.Where(s => Pard(s).Count == 0 && Own(s).Count > 0 && IsWinner(Own(s)[0]))
                    .Select(s => Own(s)[0]).FirstOrDefault();
                if (winnerToCash != null)
                    return winnerToCash;

                var ruffSuit = SideSuits.Where(s => Pard(s).Count == 0 && Own(s).Any(c => !IsWinner(c)))
                    .OrderByDescending(s => Own(s).Count).Select(s => (Suit?)s).FirstOrDefault();
                return ruffSuit.HasValue ? Own(ruffSuit.Value).Last() : null;
            }

            //  cash winners, choosing suits so the short side's winners are played first (unblocking)
            private Card CashOut()
            {
                Card best = null;
                var bestScore = int.MinValue;

                foreach (var s in SuitRank.stdSuits.Where(s => Own(s).Count > 0 && TopTricks(s) > 0))
                {
                    var card = CashCard(s);
                    if (card == null)
                        continue;

                    var iHaveWinners = Own(s).Any(IsWinner);
                    int score;
                    if (IsBlockedByMe(s) || IsBlockedByPartner(s))
                        score = 50;
                    else if (iHaveWinners)
                        score = 30;
                    else
                        score = 10;

                    //  draw any remaining trump before cashing side winners
                    if (s == trump && OpponentsHaveTrump)
                        score += 20;

                    score += TopTricks(s);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = card;
                    }
                }

                return best;
            }

            //  the card to lead when cashing winners in a suit: high from the short side, then low to the long side's winners
            private Card CashCard(Suit s)
            {
                var mine = Own(s);
                if (mine.Count == 0)
                    return null;

                var myWinners = mine.Where(IsWinner).ToList();
                var partnerWinners = Pard(s).Where(IsWinner).ToList();

                if (myWinners.Count > 0)
                {
                    if (partnerWinners.Count == 0 || mine.Count <= Pard(s).Count)
                        return myWinners[0];

                    //  we're the long side and partner's cards are all winners (would block): partner's last card
                    //  must fall on a trick we win, or we need an entry back to our hand
                    if (IsBlockedByPartner(s))
                    {
                        var theirs = Pard(s);
                        if (theirs.Count == 1)
                        {
                            //  drop partner's singleton under our winner when giving up its trick costs nothing (or we have no entry back)
                            var drop = mine.LastOrDefault(c => R(c) > R(theirs[0]));
                            if (drop != null && (mine.All(IsWinner) || !PartnerHasEntryToMe(s)))
                                return drop;
                        }

                        //  lead low so partner wins with a high card now, keeping their lowest to be overtaken later
                        if (R(mine.Last()) < R(theirs[0]))
                            return mine.Last();
                    }

                    return myWinners[0];
                }

                return partnerWinners.Count > 0 ? mine.Last() : null;
            }

            //  partner is the short side and holds only winners, so they must be cashed before our long side's winners
            private bool IsBlockedByPartner(Suit s)
            {
                var theirs = Pard(s);
                return theirs.Count > 0 && theirs.Count < Own(s).Count && theirs.All(IsWinner) && Own(s).Count(IsWinner) > 0;
            }

            //  we are the short side holding only winners while partner has more winners to run
            private bool IsBlockedByMe(Suit s)
            {
                var mine = Own(s);
                return mine.Count > 0 && mine.Count < Pard(s).Count && mine.All(IsWinner) && Pard(s).Count(IsWinner) > 0;
            }

            private Card Develop()
            {
                Card best = null;
                var bestValue = 0.0;
                var bestLength = 0;

                void Consider(Card card, double value, int length)
                {
                    if (card == null || value <= 0)
                        return;

                    if (value > bestValue || Math.Abs(value - bestValue) < 0.001 && length > bestLength)
                    {
                        best = card;
                        bestValue = value;
                        bestLength = length;
                    }
                }

                var danger = DangerSeat();
                foreach (var s in SuitRank.stdSuits)
                {
                    if (s == trump && OpponentsHaveTrump && !ShouldDrawTrump())
                        continue;

                    if (Own(s).Count > 0)
                    {
                        var plan = SuitPlan(s);
                        Consider(plan.card, plan.value, Combined(s).Count);
                    }

                    //  our own tenace wants partner to lead toward it: cross to partner first
                    var myFinesse = FinesseCard(Own(s));
                    if (myFinesse != null && Pard(s).Any(c => R(c) < R(myFinesse)))
                    {
                        var missing = MissingAbove(myFinesse);
                        var holder = LikelyHolder(missing);
                        if (holder != lho.Seat && !PreferDrop(s, missing))
                        {
                            var value = EstimatedTricks(s) - TopTricks(s) + 0.25 + (holder == rho.Seat ? 0.5 : 0);
                            if (lho.Seat == danger)
                                value -= 1;
                            Consider(CrossToPartner(s, allowTrump: !OpponentsHaveTrump), value, Combined(s).Count);
                        }
                    }
                }

                return best;
            }

            //  the best way to play a suit when we're on lead, along with how many extra tricks that's expected to develop
            private (Card card, double value) SuitPlan(Suit s)
            {
                var mine = Own(s);
                if (mine.Count == 0)
                    return (null, 0);

                var theirs = Pard(s);
                var outS = Out(s);
                if (outS.Count == 0)
                    return (CashCard(s) ?? mine.Last(), 0);

                //  developing tricks costs tempo: each round we lose gives the opponents the lead (and a trick)
                var lost = SimulateSuit(s, MaxLength(s)).lost;
                double gain = EstimatedTricks(s) - TopTricks(s) - 0.25 * lost;
                if (lost > tricksToSpare)
                    gain -= 2;
                var danger = DangerSeat();

                //  lead low toward partner's honor/tenace (finesse), unless playing for the drop is better
                var partnerFinesse = FinesseCard(theirs);
                if (partnerFinesse != null && mine.Any(c => R(c) < R(partnerFinesse)))
                {
                    var missing = MissingAbove(partnerFinesse);
                    var holder = LikelyHolder(missing);
                    if (holder != rho.Seat && !PreferDrop(s, missing))
                    {
                        var value = gain + 0.5 + (holder == lho.Seat ? 0.5 : 0);
                        if (rho.Seat == danger)
                            value -= 1;
                        return (mine.Last(), value);
                    }
                }

                //  lead an honor from a sequence toward partner's higher winner (e.g. QJ10 toward the A), letting it run
                var myTop = mine[0];
                if (Beaters(myTop) == 1 && mine.Count >= 2 && AreEquivalent(mine[0], mine[1]) && theirs.Any(IsWinner))
                {
                    var missing = MissingAbove(myTop);
                    var holder = LikelyHolder(missing);
                    if (holder != rho.Seat && !PreferDrop(s, missing))
                    {
                        var value = gain + 0.5 + (holder == lho.Seat ? 0.5 : 0);
                        if (rho.Seat == danger)
                            value -= 1;
                        return (myTop, value);
                    }
                }

                //  leading from our own tenace gives away the finesse position
                var myFinesse = FinesseCard(mine);
                var tenacePenalty = myFinesse != null && IsWinner(mine[0]) && mine[0] != myFinesse && theirs.Any(c => R(c) < R(myFinesse)) &&
                                    !PreferDrop(s, MissingAbove(myFinesse))
                    ? 1.0
                    : 0.0;

                Card card;
                if (mine.Any(IsWinner) || theirs.Any(IsWinner))
                {
                    card = CashCard(s);
                }
                else
                {
                    //  knock out the opponents' high cards: top of our sequence from the short side, otherwise low toward partner's
                    var combined = Combined(s);
                    var mineInTopSequence = mine.Where(c => AreEquivalent(c, combined[0])).ToList();
                    if (mineInTopSequence.Count > 0 && (mine.Count <= theirs.Count || !theirs.Any(c => AreEquivalent(c, combined[0]))))
                        card = mineInTopSequence[0];
                    else
                        card = mine.Last();

                    var missing = MissingAbove(combined[0]);
                    if (danger != NoSeat && missing != null && LikelyHolder(missing) != OtherOpponent(danger))
                        gain -= 0.5;
                }

                return (card ?? mine.Last(), gain - tenacePenalty);
            }

            private int OtherOpponent(int seat)
            {
                return seat == lho.Seat ? rho.Seat : lho.Seat;
            }

            //  lead low in a suit where partner holds a winner to transfer the lead to partner
            private Card CrossToPartner(Suit avoid, bool allowTrump)
            {
                Card best = null;
                var bestScore = int.MinValue;

                foreach (var s in SuitRank.stdSuits.Where(s => s != avoid && (allowTrump || s != trump) && Own(s).Count > 0))
                {
                    var partnerWinners = Pard(s).Where(IsWinner).ToList();
                    var lowCard = Own(s).Last();
                    if (partnerWinners.Count == 0 || R(lowCard) > R(partnerWinners[0]))
                        continue;

                    //  avoid suits an opponent may ruff
                    if (!IsNoTrump && s != trump && OpponentsHaveTrump && (IsKnownVoid(lho, s) || IsKnownVoid(rho, s)))
                        continue;

                    var score = partnerWinners.Count * 2 + Own(s).Count - (Own(s).Any(IsWinner) ? 3 : 0);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = lowCard;
                    }
                }

                return best;
            }

            //  give up the lead as harmlessly as possible
            private Card ExitCard()
            {
                Card best = null;
                var bestScore = int.MaxValue;
                foreach (var s in SuitRank.stdSuits.Where(s => Own(s).Count > 0))
                {
                    var score = 0;
                    if (FinesseCard(Own(s)) != null)
                        score += 3;
                    if (Own(s).Any(IsWinner))
                        score += 2;
                    if (s == trump && OpponentsHaveTrump)
                        score += 1;
                    if (FinesseCard(Pard(s)) != null)
                        score -= 1;
                    if (!IsNoTrump && s != trump && OpponentsHaveTrump && (IsKnownVoid(lho, s) || IsKnownVoid(rho, s)))
                        score += 4;

                    score = score * 20 - Own(s).Count;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = Own(s).Last();
                    }
                }

                return best;
            }

            #endregion

            #region Following

            private Card SecondHand()
            {
                var led = trick[0];
                var s = S(led);
                var follow = Own(s);
                if (follow.Count == 0)
                    return VoidPlay();

                if (ShouldHoldUp(s))
                    return follow.Last();

                //  second hand low when partner (playing last) is sure to win the trick
                if (PartnerCanWinFourthByFollowing(s, led) || PartnerCanRuffFourth(s))
                    return follow.Last();

                var beaters = follow.Where(c => R(c) > R(led)).ToList();
                if (beaters.Count == 0)
                    return follow.Last();

                //  cover an honor with an honor when that promotes one of our lower cards (unless partner can capture it)
                if (led.rank >= Rank.Ten)
                {
                    if (Pard(s).Any(c => R(c) > R(led)))
                        return follow.Last();

                    var cover = beaters.Last();
                    var promotes = Combined(s).Any(c => R(c) < R(led) && AreEquivalent(c, led) && Out(s).Any(o => R(o) > R(cover)));
                    if (IsWinner(cover) || promotes)
                        return cover;

                    return follow.Last();
                }

                var winners = beaters.Where(IsWinner).ToList();
                if (winners.Count > 0)
                    return follow.Count <= Pard(s).Count ? HighestEquivalent(winners.Last(), winners) : winners.Last();

                return follow.Last();
            }

            private Card ThirdHand()
            {
                var lead = trick[0];
                var s = S(lead);
                var follow = Own(s);
                if (follow.Count == 0)
                    return VoidPlay();

                //  RHO ruffed: we can't win by following
                if (!IsNoTrump && s != trump && S(cardTakingTrick) == trump)
                    return follow.Last();

                //  in third seat, RHO has already played and only LHO is left to play
                if (isPartnerTakingTrick)
                {
                    if (IsSafeFromLho(cardTakingTrick))
                        return UnblockOrLow(follow, s);

                    //  partner led an honor from a sequence toward our winner: let it run (finesse)
                    if (lead.suit == cardTakingTrick.suit && lead.rank == cardTakingTrick.rank && Beaters(lead) == 1 && follow.Any(IsWinner) && Pard(s).Any(c => AreEquivalent(c, lead)))
                    {
                        var holder = LikelyHolder(MissingAbove(lead));
                        if (holder != lho.Seat)
                            return follow.Last();
                    }
                }

                //  cards already in the trick aren't outstanding, so equivalence checks must be limited to cards that beat the current winner
                var candidates = follow.Where(c => R(c) > R(cardTakingTrick)).ToList();
                if (candidates.Count == 0)
                    return follow.Last();

                //  LHO can neither follow nor ruff, so any card that beats the current winner takes the trick
                if (LhoCannotFollowOrRuff(s))
                {
                    var cheapest = candidates.Last();
                    return follow.Count <= Pard(s).Count ? HighestEquivalent(cheapest, candidates) : cheapest;
                }

                var winners = candidates.Where(IsWinner).ToList();
                var finesse = candidates.Where(c => Beaters(c) == 1).ToList();

                if (finesse.Count > 0)
                {
                    var finesseCard = finesse.Last();
                    var missing = MissingAbove(finesseCard);
                    var holder = LikelyHolder(missing);
                    var dropIsBetter = winners.Count > 0 && PreferDrop(s, missing);
                    if (holder != lho.Seat && !dropIsBetter)
                        return LowestEquivalent(finesseCard, candidates);
                }

                if (winners.Count > 0)
                    return follow.Count <= Pard(s).Count ? HighestEquivalent(winners.Last(), winners) : winners.Last();

                //  force out the opponents' high cards with the bottom of our sequence when developing the suit
                //  (the sequence may extend below the current winner, but we must still play a card that beats it)
                var topCandidate = candidates[0];
                var sequenceLength = follow.Count(c => AreEquivalent(c, topCandidate));
                if (sequenceLength >= 2 && EstimatedTricks(s) > TopTricks(s))
                    return LowestEquivalent(topCandidate, candidates);

                return follow.Last();
            }

            private Card FourthHand()
            {
                var s = S(trick[0]);
                var follow = Own(s);
                if (follow.Count == 0)
                    return VoidPlay();

                if (isPartnerTakingTrick)
                    return UnblockOrLow(follow, s);

                if (!IsNoTrump && s != trump && S(cardTakingTrick) == trump)
                    return follow.Last();

                var beaters = follow.Where(c => R(c) > R(cardTakingTrick)).ToList();
                if (beaters.Count == 0 || ShouldHoldUp(s))
                    return follow.Last();

                //  win as cheaply as possible (but high from the short side among equivalent cards)
                var cheapest = beaters.Last();
                return follow.Count <= Pard(s).Count ? HighestEquivalent(cheapest, beaters) : cheapest;
            }

            //  we can't follow suit: ruff if that's needed to win the trick, otherwise discard
            private Card VoidPlay()
            {
                var s = S(trick[0]);
                if (IsNoTrump || s == trump || Own(trump).Count == 0)
                    return Discard();

                var ourSideWinning = false;
                var partnerMayBeRuffed = false;
                switch (trick.Count)
                {
                    case 1:
                        //  let partner win by following, or ruff in fourth seat (unless we're the hand that should ruff)
                        ourSideWinning = PartnerCanWinFourthByFollowing(s, trick[0]) || PartnerCanRuffFourth(s) && AmMasterTrumpHand;
                        break;
                    case 2:
                        ourSideWinning = isPartnerTakingTrick && IsSafeFromLho(cardTakingTrick);
                        partnerMayBeRuffed = isPartnerTakingTrick && !ourSideWinning && lho.VoidSuits.Contains(s);
                        break;
                    case 3:
                        ourSideWinning = isPartnerTakingTrick;
                        break;
                }

                if (ourSideWinning)
                    return Discard();

                var highestTrumpInTrick = trick.Where(c => S(c) == trump).Select(R).DefaultIfEmpty(0).Max();
                var overRuffs = Own(trump).Where(c => R(c) > highestTrumpInTrick).ToList();

                //  partner's card would win unless LHO ruffs it: protect the trick with a trump LHO can't beat
                if (partnerMayBeRuffed)
                {
                    var masterTrump = overRuffs.LastOrDefault(IsWinner);
                    if (masterTrump != null)
                        return masterTrump;
                }

                return overRuffs.Count > 0 ? overRuffs.Last() : Discard();
            }

            private Card UnblockOrLow(List<Card> follow, Suit s)
            {
                var partnerCards = Pard(s);

                //  partner just played their last card in the suit: overtake if leaving them on lead would strand our winners
                if (partnerCards.Count == 0 && isPartnerTakingTrick && S(cardTakingTrick) == s)
                {
                    var overtake = follow.LastOrDefault(c => R(c) > R(cardTakingTrick) && IsWinner(c));
                    if (overtake != null)
                    {
                        var remaining = follow.Where(c => c != overtake).ToList();
                        if (remaining.Any(IsWinner) && (remaining.All(IsWinner) || !PartnerHasEntryToMe(s)))
                            return overtake;
                    }
                }

                //  unblock: as the short side holding only winners, play high so partner's long side can win the last round
                if (partnerCards.Count >= follow.Count && partnerCards.Count > 0 && follow.All(IsWinner))
                    return follow[0];

                return follow.Last();
            }

            //  partner can get back to our hand by leading another suit to one of our winners
            private bool PartnerHasEntryToMe(Suit s)
            {
                return SuitRank.stdSuits.Any(t => t != s && Own(t).Count > 0 && IsWinner(Own(t)[0]) && Pard(t).Any(c => R(c) < R(Own(t)[0])));
            }

            //  in second seat: partner (fourth) holds a winner over the led card
            private bool PartnerCanWinFourthByFollowing(Suit s, Card led)
            {
                var partnerCards = Pard(s);
                if (partnerCards.Count == 0 || R(partnerCards[0]) <= R(led) || !IsWinner(partnerCards[0]))
                    return false;

                //  LHO (third) may ruff
                return IsNoTrump || s == trump || !OpponentsHaveTrump || !lho.VoidSuits.Contains(s);
            }

            //  in second seat: partner (fourth) is void and can ruff
            private bool PartnerCanRuffFourth(Suit s)
            {
                return !IsNoTrump && s != trump && Pard(s).Count == 0 && Pard(trump).Count > 0 &&
                       (!OpponentsHaveTrump || !lho.VoidSuits.Contains(s) || IsWinner(Pard(trump)[0]));
            }

            //  in third seat: the card winning the trick can't be beaten by LHO (the last to play)
            private bool IsSafeFromLho(Card card)
            {
                var ledSuit = S(trick[0]);
                if (S(card) != ledSuit)
                    return !lho.VoidSuits.Contains(ledSuit) || !Out(trump).Any(o => R(o) > R(card));

                if (LhoCannotFollowOrRuff(ledSuit))
                    return true;

                if (Out(ledSuit).Any(o => R(o) > R(card)))
                    return false;

                return IsNoTrump || ledSuit == trump || !OpponentsHaveTrump || !lho.VoidSuits.Contains(ledSuit);
            }

            //  LHO is known to be out of the led suit and can't ruff it
            private bool LhoCannotFollowOrRuff(Suit ledSuit)
            {
                return IsKnownVoid(lho, ledSuit) && (IsNoTrump || ledSuit == trump || IsKnownVoid(lho, trump));
            }

            //  notrump hold-up with a single stopper in the opponents' suit (Rule of 7)
            private bool ShouldHoldUp(Suit s)
            {
                //  ducking concedes a trick, so we can't hold up when the opponents already have all the tricks they can afford
                if (!IsNoTrump || !IsOpponent(leaderSeat) || tricksToSpare <= 0 || TopTricks(s) != 1 || TotalTopTricks() >= tricksNeeded)
                    return false;

                var ourSeats = new[] { me.Seat, partner.Seat };
                var ourPlayedInSuit = playedInOrder.Count(sc => ourSeats.Contains(sc.seat) && S(sc.card) == s);
                var originalLength = Combined(s).Count + ourPlayedInSuit;
                var holdUps = 7 - originalLength;

                var roundsPlayed = 0;
                for (var i = 0; i + 4 <= playedInOrder.Count; i += 4)
                    if (S(playedInOrder[i].card) == s)
                        roundsPlayed++;

                return roundsPlayed < holdUps;
            }

            //  choose a discard: keep winners, stoppers, trumps, and length in suits we're establishing
            private Card Discard()
            {
                Card best = null;
                var bestScore = int.MaxValue;
                var shortTrumpHand = !IsNoTrump && Own(trump).Count > 0 && !AmMasterTrumpHand;

                foreach (var s in SuitRank.stdSuits.Where(s => Own(s).Count > 0))
                {
                    var mine = Own(s);
                    var theirs = Pard(s);
                    var card = mine.Last();
                    var score = 0;

                    if (!IsNoTrump && s == trump)
                        score += 100;

                    if (IsWinner(card))
                        score += 50;
                    else if (mine.Any(IsWinner) && mine.Count <= theirs.Count)
                        score += 5;

                    if (IsNoTrump && Out(s).Count > 0 && HasStopper(mine) && !HasStopper(theirs))
                        score += 30;

                    if (mine.Count > theirs.Count && EstimatedTricks(s) > TopTricks(s))
                        score += 20;

                    //  keep guards for an honor that isn't yet a winner
                    var guarded = mine.FirstOrDefault(c => c.rank >= Rank.Ten && !IsWinner(c));
                    if (guarded != null && mine.Count <= Beaters(guarded) + 1)
                        score += 10;

                    //  create a void for ruffing in the short-trump hand
                    if (shortTrumpHand && s != trump && mine.Count <= 2 && theirs.Count > mine.Count)
                        score -= 5;

                    //  our cards are redundant when partner's winners cover the suit
                    if (theirs.Count(IsWinner) >= mine.Count)
                        score -= 3;

                    if (score < bestScore || score == bestScore && best != null && R(card) < R(best))
                    {
                        bestScore = score;
                        best = card;
                    }
                }

                return best;
            }

            private bool HasStopper(List<Card> cards)
            {
                for (var i = 0; i < cards.Count; i++)
                    if (Beaters(cards[i]) <= cards.Count - 1 - i)
                        return true;

                return false;
            }

            #endregion
        }
    }
}
