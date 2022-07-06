using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Trickster.cloud;

namespace Trickster.Bots
{
    public class PinochleMelder
    {
        public enum NearMeldsAggressiveness
        {
            None,
            Medium,
            High
        }

        private static readonly PinochleMeld[] CollapsibleMelds = { PinochleMeld.NineOfTrump, PinochleMeld.RoyalMarriage, PinochleMeld.CommonMarriage };

        private static readonly PinochleMeld[] MarriageOrBetter =
        {
            PinochleMeld.Run,
            PinochleMeld.DoubleRun,
            PinochleMeld.TripleRun,
            PinochleMeld.QuadrupleRun,
            PinochleMeld.RoyalMarriage,
            PinochleMeld.DoubleRoyalMarriage,
            PinochleMeld.TripleRoyalMarriage,
            PinochleMeld.QuadrupleRoyalMarriage
        };

        private static readonly Rank[] RunRanks = { Rank.Ace, Rank.Ten, Rank.King, Rank.Queen, Rank.Jack };

        //  the scores for the various ranks taken in tricks organized by the trick scoring scheme
        private static readonly Dictionary<PinochleTrickScoring, Dictionary<Rank, int>> ScoreByRankByScoring = new Dictionary<PinochleTrickScoring, Dictionary<Rank, int>>
        {
            { PinochleTrickScoring.AceTenKingOnly, new Dictionary<Rank, int> { { Rank.Ace, 10 }, { Rank.Ten, 10 }, { Rank.King, 10 } } },
            { PinochleTrickScoring.Classic, new Dictionary<Rank, int> { { Rank.Ace, 11 }, { Rank.Ten, 10 }, { Rank.King, 4 }, { Rank.Queen, 3 }, { Rank.Jack, 2 } } },
            { PinochleTrickScoring.Simplified, new Dictionary<Rank, int> { { Rank.Ace, 10 }, { Rank.Ten, 10 }, { Rank.King, 5 }, { Rank.Queen, 5 } } }
        };

        private readonly Dictionary<Suit, Dictionary<Rank, int>> _counts;
        private readonly Hand _hand;
        private readonly PinochleOptions _pinochleOptions;
        private readonly int _rankSortOfAce;
        private readonly int _rankSortOfNine;
        private readonly IBaseBot _bot;
        private readonly Suit _trump;

        public PinochleMelder(IBaseBot bot, Hand hand, Suit trump)
        {
            _bot = bot;
            _hand = hand;
            _trump = trump;

            _counts = SuitRank.stdSuits.ToDictionary(s => s, s => SuitRank.stdRanks.Where(r => r >= Rank.Nine).ToDictionary(r => r, r => _hand.Count(c => c.suit == s && c.rank == r)));
            _pinochleOptions = _bot.Options as PinochleOptions;
            _rankSortOfAce = _bot.RankSort(new Card(_trump, Rank.Ace));
            _rankSortOfNine = _bot.RankSort(new Card(_trump, Rank.Nine));
        }

        public bool HasMarriageOrBetter => GetMelds().Any(IsMarriageOrBetter);

        private bool AcesAround => TestForAround(Rank.Ace, 1);

        private int CommonMarriages
        {
            get
            {
                var marriageCandidates = new Hand(_hand.Where(c => NotTrump(c) && (c.rank == Rank.King || c.rank == Rank.Queen)));

                return SuitRank.stdSuits.Where(s => s != _trump)
                    .Sum(s => Math.Min(marriageCandidates.Count(c => c.suit == s && c.rank == Rank.King),
                        marriageCandidates.Count(c => c.suit == s && c.rank == Rank.Queen)));
            }
        }

        private bool DoubleAcesAround => TestForAround(Rank.Ace, 2);

        private bool DoubleJacksAround => TestForAround(Rank.Jack, 2);

        private bool DoubleKingsAround => TestForAround(Rank.King, 2);

        private bool DoubleQueensAround => TestForAround(Rank.Queen, 2);

        //  this is a single-deck only test
        private bool DoubleRoyalMarriage => _pinochleOptions.doubleRoyalMarriage && !DoubleRun && _counts[_trump][Rank.King] == 2 && _counts[_trump][Rank.Queen] == 2;

        private bool DoubleRun => TestForRun(2);

        private bool JacksAround => TestForAround(Rank.Jack, 1);

        private bool KingsAround => TestForAround(Rank.King, 1);

        private int NinesOfTrump => _counts[_trump][Rank.Nine];

        private int Pinochles => Math.Min(_counts[Suit.Diamonds][Rank.Jack], _counts[Suit.Spades][Rank.Queen]);

        private bool QuadrupleAcesAround => TestForAround(Rank.Ace, 4);

        private bool QuadrupleJacksAround => TestForAround(Rank.Jack, 4);

        private bool QuadrupleKingsAround => TestForAround(Rank.King, 4);

        private bool QuadrupleQueensAround => TestForAround(Rank.Queen, 4);

        private bool QuadrupleRun => TestForRun(4);

        private bool QueensAround => TestForAround(Rank.Queen, 1);

        private bool Run => TestForRun(1);

        //  this is a single-deck only test
        private bool RunWithExtraKing => _pinochleOptions.runWithExtras && !RunWithExtraMarriage && Run && !DoubleRoyalMarriage && _counts[_trump][Rank.King] == 2;

        //  this is a single-deck only test
        private bool RunWithExtraMarriage => _pinochleOptions.runWithExtras && Run && !DoubleRoyalMarriage && _counts[_trump][Rank.King] == 2 && _counts[_trump][Rank.Queen] == 2;

        //  this is a single-deck only test
        private bool RunWithExtraQueen => _pinochleOptions.runWithExtras && !RunWithExtraMarriage && Run && !DoubleRoyalMarriage && _counts[_trump][Rank.Queen] == 2;

        private bool TripleAcesAround => TestForAround(Rank.Ace, 3);

        private bool TripleJacksAround => TestForAround(Rank.Jack, 3);

        private bool TripleKingsAround => TestForAround(Rank.King, 3);

        private bool TripleQueensAround => TestForAround(Rank.Queen, 3);

        private bool TripleRun => TestForRun(3);

        public static int CardPoints(PinochleOptions options, Card card)
        {
            return ScoreByRankByScoring.TryGetValue(options.trickScoring, out var scoresByRank)
                ? scoresByRank.TryGetValue(card.rank, out var rankScore)
                    ? rankScore / options.TrickScoreDivisor
                    : 0
                : 0;
        }

        public static List<TakenPointCards> GetTakenPointCards(List<Card> hand, IBaseBot bot, PinochleOptions options)
        {
            var taken = new List<TakenPointCards>();

            if (ScoreByRankByScoring.TryGetValue(options.trickScoring, out var scoresByRank))
            {
                foreach (var rankScore in scoresByRank.Where(rs => rs.Value > 0))
                {
                    var tpc = new TakenPointCards { taken = new Hand(hand.Where(c => c.rank == rankScore.Key).OrderBy(bot.SuitSort)) };

                    if (tpc.taken.Count > 0)
                    {
                        tpc.points = tpc.taken.Count * rankScore.Value / options.TrickScoreDivisor;
                        taken.Add(tpc);
                    }
                }
            }

            return taken;
        }

        public static bool IsMarriageOrBetter(PinochleMeld m)
        {
            return MarriageOrBetter.Contains(m);
        }

        public static int ScoreTrick(PinochleOptions options, Hand trick, bool lastTrick)
        {
            var trickScore = lastTrick ? options.ClassicTrickScoreForLastTrick : 0;

            if (ScoreByRankByScoring.TryGetValue(options.trickScoring, out var scoresByRank))
                trickScore += trick.Sum(c => scoresByRank.TryGetValue(c.rank, out var rankScore) ? rankScore : 0);

            return trickScore / options.TrickScoreDivisor;
        }

        public List<Card> GetCardsNotUsedInMelds()
        {
            return new Hand(_hand).RemoveCards(PinochleMelds.MinimumCardsRequired(GetMeldCards()));
        }

        public List<Card> GetCardsNotUsedInMelds(List<PinochleMelds.Meld> meldCards)
        {
            return new Hand(_hand).RemoveCards(PinochleMelds.MinimumCardsRequired(meldCards));
        }

        public List<PinochleMelds.Meld> GetMeldCards(Suit[] playerOrderedSuits = null, bool collapseMelds = true, bool collapseNines = true)
        {
            return GetMeldCards(GetMelds(), playerOrderedSuits, collapseMelds, collapseNines);
        }

        public MeldInfo GetMeldInfo(Suit suit, Suit[] playerOrderedSuits)
        {
            var meldCards = GetMeldCards(playerOrderedSuits);

            return new MeldInfo
            {
                points = GetMeldPoints(_pinochleOptions, meldCards),
                cardsUsed = PinochleMelds.MinimumCardsRequired(meldCards),
                detailHtml = GetMeldDetailHtml(suit, meldCards, _pinochleOptions)
            };
        }

        public List<PinochleMelds.Meld> GetPossibleMelds(NearMeldsAggressiveness aggressiveness)
        {
            if (aggressiveness == NearMeldsAggressiveness.None)
                return GetMeldCards(collapseMelds: false);

            var hand = new Hand(_hand);
            var runRanksAdded = false;

            if (!DoubleRun && !Run)
            {
                var missingRunRanks = RunRanks.Where(r => !hand.ContainsSuitAndRank(_trump, r)).ToList();

                if (missingRunRanks.Count <= (aggressiveness == NearMeldsAggressiveness.High ? 2 : 1))
                {
                    hand.AddCards(missingRunRanks.Select(r => new Card(_trump, r)));
                    runRanksAdded = true;
                }
            }

            if (!runRanksAdded && !DoubleAcesAround && !AcesAround)
            {
                var suitsWithoutAces = SuitRank.stdSuits.Where(s => !hand.ContainsSuitAndRank(s, Rank.Ace)).ToList();

                if (suitsWithoutAces.Count == 1 && suitsWithoutAces[0] == _trump)
                    hand.AddCards(suitsWithoutAces.Select(s => new Card(s, Rank.Ace)));
            }

            return new PinochleMelder(_bot, hand, _trump).GetMeldCards(collapseMelds: false);
        }

        //  based on https://www.pagat.com/marriage/pintip.html#passingcards
        //  made more aggressive passing pinochle parts
        public List<Card> SuggestDeclarerPartnerPass(int passCount)
        {
            var hand = new Hand(_hand);

            var pass = new List<Card>();

            //  start passing one of each rank in trump above nine (looking to complete a run)
            foreach (var card in hand.Where(IsTrumpAboveNine).OrderByDescending(_bot.RankSort).ToList())
            {
                //  only pass the rank if we haven't yet passed it
                if (pass.Count < passCount && pass.All(c => c.rank != card.rank))
                    pass.Add(hand.RemoveCard(card));
            }

            //  pass any remaining trump preferring high ranks
            if (pass.Count < passCount)
                pass.AddRange(hand.RemoveAndReturn(hand.Where(IsTrumpAboveNine).OrderByDescending(_bot.RankSort).Take(passCount - pass.Count)));

            //  add pinochle helpers if trump is one of the Pinochle suits
            if (pass.Count < passCount)
            {
                if (_trump == Suit.Spades)
                    pass.AddRange(hand.RemoveAndReturn(hand.Where(c => c.suit == Suit.Diamonds && c.rank == Rank.Jack).Take(passCount - pass.Count)));
                else if (_trump == Suit.Diamonds)
                    pass.AddRange(hand.RemoveAndReturn(hand.Where(c => c.suit == Suit.Spades && c.rank == Rank.Queen).Take(passCount - pass.Count)));
            }

            //  add off-suit aces
            if (pass.Count < passCount)
                pass.AddRange(hand.RemoveAndReturn(hand.Where(c => NotTrump(c) && c.rank == Rank.Ace).Take(passCount - pass.Count)));

            //  pass parts of a pinochle regardless of trump if we can pass all of them (but don't pass any if we have 3 parts)
            if (pass.Count < passCount)
            {
                var pinochleParts = hand.Where(c => c.suit == Suit.Diamonds && c.rank == Rank.Jack || c.suit == Suit.Spades && c.rank == Rank.Queen).ToList();
                if (pinochleParts.Count == 1 || pinochleParts.Count == 2 && passCount - pass.Count >= 2)
                    pass.AddRange(hand.RemoveAndReturn(pinochleParts));
            }

            //  if we still need more, take from the remaining cards favoring trump and low points
            if (pass.Count < passCount)
                pass.AddRange(hand.OrderByDescending(IsTrump).ThenBy(c => CardPoints(_pinochleOptions, c)).Take(passCount - pass.Count));

            return pass;
        }

        //  based on https://www.pagat.com/marriage/pintip.html#passingcards
        public List<Card> SuggestDeclarerPass(int passCount)
        {
            //  get the melds to use in GetCardsNotUsedInMelds
            var allMelds = GetMeldCards(collapseMelds: false, collapseNines: false);
            var mustHoldMelds = GetMustHoldMelds(allMelds);

            var candidateGroups = new[]
            {
                GetCardsNotUsedInMelds(allMelds).Where(c => NotTrump(c) && NotAce(c)).ToList(),
                GetCardsNotUsedInMelds(mustHoldMelds).Where(c => NotTrump(c) && NotAce(c)).ToList(),
                GetCardsNotUsedInMelds(mustHoldMelds).Where(c => !IsTrumpAboveNine(c) && NotAce(c)).ToList(),
                GetCardsNotUsedInMelds(mustHoldMelds).Where(c => !IsTrumpAboveNine(c)).ToList(),
                _hand.Where(c => !IsTrumpAboveNine(c) && NotAce(c)).ToList()
            };

            foreach (var candidates in candidateGroups.Where(cards => cards.Count >= passCount))
            {
                //  our candidates set has enough cards - figure out the best of the group to send
                if (candidates.Count == passCount)
                    return candidates;

                //  if we have melds within the candidates, try not to break them up
                var pass = new List<Card>();
                var candidateHand = new Hand(candidates);
                var candidateMelds = new PinochleMelder(_bot, candidateHand, _trump).GetMeldCards(collapseMelds: false, collapseNines: false);

                if (candidateMelds.Count > 0)
                {
                    var minCards = PinochleMelds.MinimumCardsRequired(candidateMelds);
                    if (minCards.Count <= passCount)
                    {
                        //  if we can pass all the cards of these melds, do so
                        pass.AddRange(candidateHand.RemoveAndReturn(minCards));
                    }
                    else
                    {
                        //  figure out groups of non-overlapping melds
                        //  none of the candidate groups that come through here include trump above nine
                        //  that means, we're not going to see runs, royal marriages, arounds, or abounds
                        //  so we're left with pinochles not including trump cards, common marriages, nines of trump
                        //  of these, we only have to worry about overlapping pinochles and marriages
                        //  so, here's our approach:
                        //      if we have a double pinochle and are passing 4+ points, pass it whole and don't worry about marriages
                        //      if we have a single pinochle with a single overlapping marriage, pass those three cards if we can
                        if (candidateMelds.Any(cm => cm.m == PinochleMeld.DoublePinochle) && passCount >= 4)
                        {
                            var doublePinochle = candidateMelds.First(cm => cm.m == PinochleMeld.DoublePinochle);
                            pass.AddRange(candidateHand.RemoveAndReturn(doublePinochle.Cards));
                            candidateMelds.Remove(doublePinochle);
                        }
                        else
                        {
                            var pinochle = candidateMelds.FirstOrDefault(cm => cm.m == PinochleMeld.Pinochle);
                            if (pinochle != null)
                            {
                                var overlappingMarriages = candidateMelds.Where(cm => cm.m == PinochleMeld.CommonMarriage && cm.Cards.Any(c => pinochle.Cards.ContainsCard(c))).ToList();
                                if (overlappingMarriages.Count == 1 && passCount >= 3)
                                {
                                    pass.AddRange(candidateHand.RemoveAndReturn(pinochle.Cards));
                                    pass.AddRange(candidateHand.RemoveAndReturn(overlappingMarriages[0].Cards.Where(c => !pinochle.Cards.ContainsCard(c))));

                                    candidateMelds.Remove(pinochle);
                                    candidateMelds.Remove(overlappingMarriages[0]);
                                }
                            }
                        }

                        //  add any remaining melds in decreasing point order (assuming their cards fit)
                        foreach (var meld in candidateMelds.OrderByDescending(m => m.Points(_pinochleOptions)).Where(m => m.Cards.Count <= passCount - pass.Count))
                            pass.AddRange(candidateHand.RemoveAndReturn(meld.Cards.Where(c => candidateHand.ContainsCard(c))));
                    }
                }

                //  if we're still short, add high value cards, favoring non-trump and low ranked cards
                if (pass.Count < passCount)
                    pass.AddRange(candidateHand.RemoveAndReturn(candidateHand.OrderBy(IsTrump).ThenByDescending(c => CardPoints(_pinochleOptions, c)).ThenBy(_bot.RankSort).Take(passCount - pass.Count)));

                Debug.Assert(pass.Count == passCount, "pass.Count == passCount");

                return pass;
            }

            //  we must have a hell of a hand. just pass low ranked cards favoring non-trump
            return _hand.OrderBy(IsTrump).ThenBy(_bot.RankSort).Take(passCount).ToList();
        }

        public List<Card> SuggestDiscard(int discardCount)
        {
            var notUsedInMelds = GetCardsNotUsedInMelds();

            var notUsedInMeldsAndNotTrumpOrAces = notUsedInMelds.Where(c => NotTrump(c) && NotAce(c)).ToList();
            if (notUsedInMeldsAndNotTrumpOrAces.Count >= discardCount)
                return notUsedInMeldsAndNotTrumpOrAces.OrderBy(_bot.RankSort).Take(discardCount).ToList();

            return notUsedInMelds.Count >= discardCount
                ? notUsedInMelds.OrderBy(IsTrump).ThenBy(_bot.RankSort).Take(discardCount).ToList()
                : new Hand(_hand).OrderBy(IsTrump).ThenBy(_bot.RankSort).Take(discardCount).ToList();
        }

        private static void AppendMeldRows(StringBuilder sb, IEnumerable<PinochleMelds.Meld> myMelds, PinochleOptions options)
        {
            foreach (var meld in myMelds.OrderByDescending(m => m.Points(options)).ThenBy(m => m.Group))
            {
                sb.Append("<tr>");
                sb.Append($"<td>{meld.Name}");
                sb.Append($" ({string.Join("&thinsp;", meld.Cards.Select(c => c.StdNotation))})</td>");
                sb.Append($"<td>{meld.Points(options):N0}</td>");
                sb.Append("</tr>");
            }
        }

        public static List<PinochleMelds.Meld> CollapseMelds(List<PinochleMelds.Meld> meldCards, PinochleMeld[] collapsible = null)
        {
            if (collapsible == null)
                collapsible = CollapsibleMelds;

            if (!meldCards.Any(mc => collapsible.Contains(mc.m)))
                return meldCards;

            foreach (var pinochleMeld in collapsible)
            {
                var collapseThese = meldCards.Where(mc => mc.m == pinochleMeld).ToList();

                if (collapseThese.Count > 1)
                {
                    meldCards.Add(new PinochleMelds.Meld(pinochleMeld, new Hand(collapseThese.SelectMany(ct => ct.Cards)), collapseThese.Count));

                    foreach (var ct in collapseThese)
                        meldCards.Remove(ct);
                }
            }

            return meldCards;
        }

        private static string GetMeldDetailHtml(Suit suit, IReadOnlyCollection<PinochleMelds.Meld> melds, PinochleOptions options)
        {
            var sb = new StringBuilder();
            sb.Append("<table class='pinochle-score-explained'>");
            sb.Append($"<tr><th colspan='2'>{Card.SuitSymbol(suit)} Meld</span></th></tr>");

            if (melds.Count == 0)
                sb.Append("<tr><td colspan='2'>None</td></tr>");
            else
            {
                AppendMeldRows(sb, melds, options);
                sb.Append($"<tr class='total'><td>{Card.SuitSymbol(suit)} Meld Points</td><td>{melds.Sum(m => m.Points(options)):N0}</td></tr>");
            }

            sb.Append("</table>");
            return sb.ToString();
        }

        private static int GetMeldPoints(PinochleOptions options, IEnumerable<PinochleMelds.Meld> meldCards)
        {
            return meldCards.Sum(mc => mc.Points(options));
        }

        private static IEnumerable<Card> GetPinochleCards(int pinochles)
        {
            var cards = new List<Card>();

            for (var i = 0; i < pinochles; ++i)
            {
                cards.Add(new Card(Suit.Diamonds, Rank.Jack));
                cards.Add(new Card(Suit.Spades, Rank.Queen));
            }

            return cards;
        }

        private static int KingsOrQueensUsed(IEnumerable<PinochleMeld> melds)
        {
            var used = 0;

            foreach (var meld in melds)
            {
                switch (meld)
                {
                    case PinochleMeld.Run:
                        used += 1;
                        break;
                    case PinochleMeld.DoubleRun:
                        used += 2;
                        break;
                    case PinochleMeld.TripleRun:
                        used += 3;
                        break;
                    case PinochleMeld.QuadrupleRun:
                        used += 4;
                        break;
                    case PinochleMeld.RoyalMarriage:
                        used += 1;
                        break;
                    case PinochleMeld.DoubleRoyalMarriage:
                    case PinochleMeld.RunWithExtraKing:
                    case PinochleMeld.RunWithExtraQueen:
                    case PinochleMeld.RunWithExtraMarriage:
                        used += 2;
                        break;
                    case PinochleMeld.TripleRoyalMarriage:
                        used += 3;
                        break;
                    case PinochleMeld.QuadrupleRoyalMarriage:
                        used += 4;
                        break;
                }
            }

            return used;
        }

        private IEnumerable<Card> GetAroundCards(IEnumerable<Suit> playerOrderedSuits, Rank rank, int count)
        {
            return playerOrderedSuits.SelectMany(s => _hand.Where(c => c.suit == s && c.rank == rank).Take(count));
        }

        private List<PinochleMelds.Meld> GetMeldCards(IEnumerable<PinochleMeld> melds, Suit[] playerOrderedSuits = null, bool collapseMelds = true, bool collapseNines = true)
        {
            if (playerOrderedSuits == null)
                playerOrderedSuits = SuitRank.stdSuits.OrderBy(_bot.SuitOrder).ToArray();

            var meldCards = new List<PinochleMelds.Meld>();
            var commonMarriageCandidates = new Hand(_hand.Where(c => NotTrump(c) && (c.rank == Rank.King || c.rank == Rank.Queen)));

            //  code assumes the melds are valid
            //  returns cards in the order they are expected to be displayed
            foreach (var meld in melds)
            {
                IEnumerable<Card> cards = null;

                switch (meld)
                {
                    case PinochleMeld.Run:
                        cards = GetRunCards(1);
                        break;
                    case PinochleMeld.DoubleRun:
                        cards = GetRunCards(2);
                        break;
                    case PinochleMeld.RunWithExtraKing:
                        cards = RunRanks.SelectMany(r => _hand.Where(c => IsTrump(c) && c.rank == r).Take(r == Rank.King ? 2 : 1)).OrderByDescending(_bot.RankSort);
                        break;
                    case PinochleMeld.RunWithExtraQueen:
                        cards = RunRanks.SelectMany(r => _hand.Where(c => IsTrump(c) && c.rank == r).Take(r == Rank.Queen ? 2 : 1)).OrderByDescending(_bot.RankSort);
                        break;
                    case PinochleMeld.RunWithExtraMarriage:
                        cards = RunRanks.SelectMany(r => _hand.Where(c => IsTrump(c) && c.rank == r).Take(r == Rank.Queen || r == Rank.King ? 2 : 1)).OrderByDescending(_bot.RankSort);
                        break;
                    case PinochleMeld.RoyalMarriage:
                        cards = _hand.Where(c => IsTrump(c) && c.rank == Rank.King).Take(1).Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.Queen).Take(1));
                        break;
                    case PinochleMeld.DoubleRoyalMarriage:
                        cards = _hand.Where(c => IsTrump(c) && c.rank == Rank.King).Take(1).Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.Queen).Take(1))
                            .Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.King).Take(1)).Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.Queen).Take(1));
                        break;
                    case PinochleMeld.NineOfTrump:
                        cards = _hand.Where(c => IsTrump(c) && c.rank == Rank.Nine).Take(1);
                        break;
                    case PinochleMeld.AcesAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.Ace, 1);
                        break;
                    case PinochleMeld.KingsAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.King, 1);
                        break;
                    case PinochleMeld.QueensAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.Queen, 1);
                        break;
                    case PinochleMeld.JacksAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.Jack, 1);
                        break;
                    case PinochleMeld.DoubleAcesAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.Ace, 2);
                        break;
                    case PinochleMeld.DoubleKingsAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.King, 2);
                        break;
                    case PinochleMeld.DoubleQueensAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.Queen, 2);
                        break;
                    case PinochleMeld.DoubleJacksAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.Jack, 3);
                        break;
                    case PinochleMeld.CommonMarriage:

                        //  we have a meld entry for every common marriage we have. we need to return them uniquely, not always the same pair
                        foreach (var suit in playerOrderedSuits.Where(s => s != _trump))
                        {
                            if (commonMarriageCandidates.ContainsSuitAndRank(suit, Rank.King) && commonMarriageCandidates.ContainsSuitAndRank(suit, Rank.Queen))
                            {
                                cards = new[] { commonMarriageCandidates.RemoveSuitRank(suit, Rank.King), commonMarriageCandidates.RemoveSuitRank(suit, Rank.Queen) };
                                break; // from for loop
                            }
                        }

                        break;
                    case PinochleMeld.Pinochle:
                        cards = GetPinochleCards(1);
                        break;
                    case PinochleMeld.DoublePinochle:
                        cards = GetPinochleCards(2);
                        break;
                    case PinochleMeld.TripleRun:
                        cards = GetRunCards(3);
                        break;
                    case PinochleMeld.QuadrupleRun:
                        cards = GetRunCards(4);
                        break;
                    case PinochleMeld.TriplePinochle:
                        cards = GetPinochleCards(3);
                        break;
                    case PinochleMeld.QuadruplePinochle:
                        cards = GetPinochleCards(4);
                        break;
                    case PinochleMeld.TripleAcesAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.Ace, 3);
                        break;
                    case PinochleMeld.QuadrupleAcesAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.Ace, 4);
                        break;
                    case PinochleMeld.TripleKingsAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.King, 3);
                        break;
                    case PinochleMeld.QuadrupleKingsAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.King, 4);
                        break;
                    case PinochleMeld.TripleQueensAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.Queen, 3);
                        break;
                    case PinochleMeld.QuadrupleQueensAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.Queen, 4);
                        break;
                    case PinochleMeld.TripleJacksAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.Jack, 3);
                        break;
                    case PinochleMeld.QuadrupleJacksAround:
                        cards = GetAroundCards(playerOrderedSuits, Rank.Jack, 4);
                        break;
                    case PinochleMeld.TripleRoyalMarriage:
                        cards = _hand.Where(c => IsTrump(c) && c.rank == Rank.King).Take(1).Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.Queen).Take(1))
                            .Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.King).Take(1)).Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.Queen).Take(1))
                            .Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.King).Take(1)).Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.Queen).Take(1));
                        break;
                    case PinochleMeld.QuadrupleRoyalMarriage:
                        cards = _hand.Where(c => IsTrump(c) && c.rank == Rank.King).Take(1).Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.Queen).Take(1))
                            .Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.King).Take(1)).Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.Queen).Take(1))
                            .Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.King).Take(1)).Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.Queen).Take(1))
                            .Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.King).Take(1)).Concat(_hand.Where(c => IsTrump(c) && c.rank == Rank.Queen).Take(1));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                meldCards.Add(new PinochleMelds.Meld(meld, new Hand(cards)));
            }

            return collapseMelds ? CollapseMelds(meldCards) : collapseNines ? CollapseMelds(meldCards, new[] { PinochleMeld.NineOfTrump }) : meldCards;
        }

        private IEnumerable<PinochleMeld> GetMelds()
        {
            var melds = new List<PinochleMeld>();

            //  group 1 melds
            if (Run && !RunWithExtraKing && !RunWithExtraQueen && !RunWithExtraMarriage)
                melds.Add(PinochleMeld.Run);

            if (_pinochleOptions.runWithExtras)
            {
                if (RunWithExtraKing)
                    melds.Add(PinochleMeld.RunWithExtraKing);
                else if (RunWithExtraQueen)
                    melds.Add(PinochleMeld.RunWithExtraQueen);
                else if (RunWithExtraMarriage)
                    melds.Add(PinochleMeld.RunWithExtraMarriage);
            }

            if (DoubleRun)
                melds.Add(PinochleMeld.DoubleRun);

            if (TripleRun)
                melds.Add(PinochleMeld.TripleRun);

            if (QuadrupleRun)
                melds.Add(PinochleMeld.QuadrupleRun);

            if (_pinochleOptions.doubleRoyalMarriage)
            {
                //  in double deck, allow a double marriage with a single run but not a double run
                switch (RoyalMarriages(_pinochleOptions.doubleDeck ? KingsOrQueensUsed(melds.Where(m => m != PinochleMeld.Run)) : 0))
                {
                    case 4:
                        melds.Add(PinochleMeld.QuadrupleRoyalMarriage);
                        break;
                    case 3:
                        melds.Add(PinochleMeld.TripleRoyalMarriage);
                        break;
                    case 2:
                        melds.Add(PinochleMeld.DoubleRoyalMarriage);
                        break;
                    default:
                        if (RoyalMarriages(KingsOrQueensUsed(melds)) > 0)
                            melds.Add(PinochleMeld.RoyalMarriage);
                        break;
                }
            }
            else
            {
                for (var i = RoyalMarriages(KingsOrQueensUsed(melds)); i > 0; --i)
                    melds.Add(PinochleMeld.RoyalMarriage);
            }

            for (var i = CommonMarriages; i > 0; --i)
                melds.Add(PinochleMeld.CommonMarriage);

            for (var i = NinesOfTrump; i > 0; --i)
                melds.Add(PinochleMeld.NineOfTrump);

            //  group 2 melds
            if (AcesAround)
                melds.Add(PinochleMeld.AcesAround);

            if (DoubleAcesAround)
                melds.Add(PinochleMeld.DoubleAcesAround);

            if (TripleAcesAround)
                melds.Add(PinochleMeld.TripleAcesAround);

            if (QuadrupleAcesAround)
                melds.Add(PinochleMeld.QuadrupleAcesAround);

            if (KingsAround)
                melds.Add(PinochleMeld.KingsAround);

            if (DoubleKingsAround)
                melds.Add(PinochleMeld.DoubleKingsAround);

            if (TripleKingsAround)
                melds.Add(PinochleMeld.TripleKingsAround);

            if (QuadrupleKingsAround)
                melds.Add(PinochleMeld.QuadrupleKingsAround);

            if (QueensAround)
                melds.Add(PinochleMeld.QueensAround);

            if (DoubleQueensAround)
                melds.Add(PinochleMeld.DoubleQueensAround);

            if (TripleQueensAround)
                melds.Add(PinochleMeld.TripleQueensAround);

            if (QuadrupleQueensAround)
                melds.Add(PinochleMeld.QuadrupleQueensAround);

            if (JacksAround)
                melds.Add(PinochleMeld.JacksAround);

            if (DoubleJacksAround)
                melds.Add(PinochleMeld.DoubleJacksAround);

            if (TripleJacksAround)
                melds.Add(PinochleMeld.TripleJacksAround);

            if (QuadrupleJacksAround)
                melds.Add(PinochleMeld.QuadrupleJacksAround);

            //  group 3 melds
            var pinochles = Pinochles;
            switch (pinochles)
            {
                case 1:
                    melds.Add(PinochleMeld.Pinochle);
                    break;
                case 2 when _pinochleOptions.noDoublePinochle:
                    melds.AddRange(new[] { PinochleMeld.Pinochle, PinochleMeld.Pinochle });
                    break;
                case 2 when !_pinochleOptions.noDoublePinochle:
                    melds.Add(PinochleMeld.DoublePinochle);
                    break;
                case 3:
                    melds.Add(PinochleMeld.TriplePinochle);
                    break;
                case 4:
                    melds.Add(PinochleMeld.QuadruplePinochle);
                    break;
            }

            return melds;
        }

        private List<PinochleMelds.Meld> GetMustHoldMelds(IReadOnlyCollection<PinochleMelds.Meld> allMelds)
        {
            var mustHold = new List<PinochleMelds.Meld>();

            var trumpPinochle = allMelds.FirstOrDefault(m => m.m == PinochleMeld.Pinochle && m.Cards.Any(IsTrump));

            foreach (var meld in allMelds)
            {
                switch (meld.m)
                {
                    case PinochleMeld.NineOfTrump:
                        //  throw a nine of trump if we must
                        continue;

                    case PinochleMeld.Pinochle when meld.Cards.All(NotTrump):
                        //  throw a non-trump pinochle
                        continue;

                    case PinochleMeld.CommonMarriage:
                        //  throw a common marriage that doesn't overlap a trump pinochle
                        if (trumpPinochle == null || meld.Cards.All(c => !trumpPinochle.Cards.ContainsCard(c)))
                            continue;

                        mustHold.Add(meld);
                        break;

                    default:
                        mustHold.Add(meld);
                        break;
                }
            }

            return mustHold;
        }

        private IEnumerable<Card> GetRunCards(int count)
        {
            return RunRanks.SelectMany(r => _hand.Where(c => IsTrump(c) && c.rank == r).Take(count)).OrderByDescending(_bot.RankSort);
        }

        private bool IsTrump(Card card)
        {
            return card.suit == _trump;
        }

        private bool IsTrumpAboveNine(Card card)
        {
            return IsTrump(card) && _bot.RankSort(card) > _rankSortOfNine;
        }

        private bool NotAce(Card c)
        {
            return _bot.RankSort(c) != _rankSortOfAce;
        }

        private bool NotTrump(Card card)
        {
            return card.suit != _trump;
        }

        //  Royal Marriages have to adjust for any Kings and Queens used in runs and runs with extras
        private int RoyalMarriages(int used)
        {
            return Math.Max(0, Math.Min(_counts[_trump][Rank.King], _counts[_trump][Rank.Queen]) - used);
        }

        private bool TestForAround(Rank rank, int min)
        {
            return SuitRank.stdSuits.All(suit => _counts[suit][rank] >= min) && !SuitRank.stdSuits.All(suit => _counts[suit][rank] > min);
        }

        private bool TestForRun(int min)
        {
            return RunRanks.All(rank => _counts[_trump][rank] >= min) && !RunRanks.All(rank => _counts[_trump][rank] > min);
        }

        public class TakenPointCards
        {
            public int points { get; set; }
            public Hand taken { get; set; }
        }
    }
}