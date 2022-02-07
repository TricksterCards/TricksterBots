using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Trickster.cloud;
using JsonSerializer = System.Text.Json.JsonSerializer;
#if DEBUG
using System.Text;
using Newtonsoft.Json;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
#endif

namespace Trickster.Bots.Controllers
{
    public class Suggester
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { IncludeFields = true };

        public static string SuggestBid<OT>(string postData, Func<SuggestBidState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestBidState<OT>>(FixPostedJson(postData), _jsonSerializerOptions);

            if (state == null)
                return null;

            state.SortCardMembers();
            var bot = getBot(state);

            BidBase bid;
            try
            {
                bid = bot.SuggestBid(state);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }

#if DEBUG
            CompareBidResults(state, bid, bot);
#endif

            return JsonSerializer.Serialize(bid);
        }

        public static string SuggestDiscard<OT>(string postData, Func<SuggestDiscardState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestDiscardState<OT>>(FixPostedJson(postData), _jsonSerializerOptions);

            if (state == null)
                return null;

            state.SortCardMembers();

            var bot = getBot(state);

            List<Card> discard;
            try
            {
                discard = bot.SuggestDiscard(state);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }

#if DEBUG
            CompareDiscardResults(state, discard, bot);
#endif

            return JsonSerializer.Serialize(discard.Select(SuitRank.FromCard));
        }

        public static string SuggestNextCard<OT>(string postData, Func<SuggestCardState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestCardState<OT>>(FixPostedJson(postData), _jsonSerializerOptions);

            if (state == null || state.legalCards.Count == 0)
                return null;

            //  if there's only one card, play it
            if (state.legalCards.Count == 1)
                return JsonSerializer.Serialize(SuitRank.FromCard(state.legalCards[0]));

            state.SortCardMembers();

            var bot = getBot(state);

            Card card;
            try
            {
                card = bot.SuggestNextCard(state);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }

#if DEBUG
            CompareCardResults(state, card, bot, getBot);
#endif

            return JsonSerializer.Serialize(SuitRank.FromCard(card));
        }

        public static string SuggestPass<OT>(string postData, Func<SuggestPassState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestPassState<OT>>(FixPostedJson(postData), _jsonSerializerOptions);

            if (state == null)
                return null;

            state.SortCardMembers();

            var bot = getBot(state);

            List<Card> pass;
            try
            {
                pass = bot.SuggestPass(state);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }

#if DEBUG
            ComparePassResults(state, pass, bot);
#endif

            return JsonSerializer.Serialize(pass.Select(SuitRank.FromCard));
        }

        private static string FixPostedJson(string postData)
        {
            return postData.Replace("\"r\":", "\"rank\":").Replace("\"s\":", "\"suit\":");
        }

#if DEBUG
        private static void CompareBidResults<OT>(SuggestBidState<OT> state, BidBase bid, BaseBot<OT> baseBot) where OT : GameOptions
        {
            CompareResults<SuggestBidState<OT>, OT>(state, () =>
            {
                var same = bid.value == state.cloudBid.value;

                Debug.WriteLineIf(!same,
                    $"\nSeat {state.player.Seat}: Bot-suggested bid of {bid.value} mismatches the cloud-suggested bid of {state.cloudBid.value} ({state.options.gameCode}).");

                state.cloudBid = null;
                return same;
            });
        }

        private static void CompareCardResults<OT>(SuggestCardState<OT> state, Card botCard, BaseBot<OT> bot, Func<SuggestCardState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            CompareResults<SuggestCardState<OT>, OT>(state, () =>
            {
                var cloudCard = state.cloudCard;
                var same = botCard.suit == cloudCard.suit && botCard.rank == cloudCard.rank;

                Debug.WriteLineIf(!same,
                    $"\nSeat {state.player.Seat}: Bot-suggested card of {botCard.rank} of {botCard.suit} mismatches the cloud-suggested card of {cloudCard.rank} of {cloudCard.suit} ({state.options.gameCode}).");
                Debug.WriteLineIf(!state.legalCards.Any(lc => lc.SameAs(botCard)), "Suggested card is not in the legal cards.");

                return same;
            });
        }

        private static void CompareCardsPlayed<OT>(SuggestCardState<OT> state, SuggestCardState<OT> cloudState) where OT : GameOptions
        {
            if (cloudState == default)
                return;

            var cardsPlayedJson = JsonConvert.SerializeObject(state.cardsPlayed);
            var savedCardsPlayedJson = JsonConvert.SerializeObject(cloudState.cardsPlayed);

            Debug.WriteLineIf(cardsPlayedJson != savedCardsPlayedJson, $"state.cardsPlayed differs ({cardsPlayedJson} != {savedCardsPlayedJson})");
        }

        private static void ComparePlayer(PlayerBase statePlayer, PlayerBase savedStatePlayer)
        {
            Debug.WriteLineIf(statePlayer.Folded != savedStatePlayer.Folded,
                $"Player in seat {statePlayer.Seat}: Folded differs ({statePlayer.Folded} != {savedStatePlayer.Folded})");

            Debug.WriteLineIf(statePlayer.GoodSuit != savedStatePlayer.GoodSuit,
                $"Player in seat {statePlayer.Seat}: GoodSuit differs ({statePlayer.GoodSuit} != {savedStatePlayer.GoodSuit})");

            Debug.WriteLineIf(statePlayer.Hand != savedStatePlayer.Hand,
                $"Player in seat {statePlayer.Seat}: Hand differs ({statePlayer.Hand} != {savedStatePlayer.Hand})");

            Debug.WriteLineIf(JsonConvert.SerializeObject(statePlayer.VoidSuits) != JsonConvert.SerializeObject(savedStatePlayer.VoidSuits),
                $"Player in seat {statePlayer.Seat}: VoidSuits differs ({JsonConvert.SerializeObject(statePlayer.VoidSuits)} != {JsonConvert.SerializeObject(savedStatePlayer.VoidSuits)})");

            Debug.WriteLineIf(statePlayer.CardsTaken != savedStatePlayer.CardsTaken,
                $"Player in seat {statePlayer.Seat}: CardsTaken differs ({statePlayer.CardsTaken} != {savedStatePlayer.CardsTaken})");

            Debug.WriteLineIf(JsonConvert.SerializeObject(statePlayer.PlayedCards) != JsonConvert.SerializeObject(savedStatePlayer.PlayedCards),
                $"Player in seat {statePlayer.Seat}: PlayedCards differs ({JsonConvert.SerializeObject(statePlayer.PlayedCards)} != {JsonConvert.SerializeObject(savedStatePlayer.PlayedCards)})");
        }

        private static void CompareDiscardResults<OT>(SuggestDiscardState<OT> state, List<Card> discard, object baseBot) where OT : GameOptions
        {
            CompareResults<SuggestDiscardState<OT>, OT>(state, () =>
            {
                var discardJson = JsonConvert.SerializeObject(SuggestSorter.SortCardList(discard));
                var cloudDiscardJson = JsonConvert.SerializeObject(SuggestSorter.SortCardList(state.cloudDiscard));
                state.cloudDiscard = null;

                var same = discardJson == cloudDiscardJson;
                Debug.WriteLineIf(!same,
                    $"\nSeat {state.player.Seat}: Bot-suggested discard of {discardJson} mismatches the cloud-suggested discard of {cloudDiscardJson} ({state.options.gameCode}).");
                return same;
            });
        }

        private static void CompareStateByLine(object clientState, object cloudState)
        {
            var cloudFormatted = JsonConvert.SerializeObject(cloudState, Formatting.Indented);
            var clientFormatted = JsonConvert.SerializeObject(clientState, Formatting.Indented);

            var diff = InlineDiffBuilder.Diff(cloudFormatted, clientFormatted);

            if (diff.Lines.Count > 0)
            {
                var sb = new StringBuilder();

                foreach (var line in diff.Lines)
                    switch (line.Type)
                    {
                        case ChangeType.Unchanged:
                            break;
                        case ChangeType.Deleted:
                            sb.AppendLine($"<<\t{line.Text}");
                            break;
                        case ChangeType.Inserted:
                            sb.AppendLine($">>\t\t\t\t\t\t\t\t{line.Text}");
                            break;
                        case ChangeType.Imaginary:
                            sb.AppendLine($"??  {line.Text}");
                            break;
                        case ChangeType.Modified:
                            sb.AppendLine($"!=  {line.Text}");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                Debug.WriteLine(sb.Length > 0 ? sb.ToString() : "\tNo different lines found");
            }
            else
            {
                Debug.WriteLine("\tdiff.Lines.Count == 0");
            }
        }

        private static void CompareResults<ST, OT>(ST state, Func<bool> checkResult)
            where ST : SuggestStateBase<OT>
            where OT : GameOptions
        {
            var same = checkResult();

#if NO_COMPARE_STATE
            if (same)
                return;
#endif

            if (string.IsNullOrEmpty(state.cloudStateJson))
            {
                Debug.WriteLine("state.cloudState is missing");
                return;
            }

            var cloudStateJson = state.cloudStateJson;
            var cloudState = JsonConvert.DeserializeObject<ST>(cloudStateJson);

            state.cloudStateJson = null;
            var stateJson = JsonConvert.SerializeObject(state);

            if (stateJson != cloudStateJson)
            {
                Debug.WriteLine($"client-sent and cloud-saved states differ.\n\tclient: {stateJson}\n\t cloud: {cloudStateJson}");
                CompareStateByLine(state, cloudState);

                CompareOptions(state.options, cloudState.options);

                if (state is SuggestCardState<OT> cardState && cloudState is SuggestCardState<OT> cloudCardState)
                {
                    CompareCardsPlayed(cardState, cloudCardState);
                    ComparePlayer(state.player, cloudCardState.player);

                    foreach (var playerSeat in cardState.players.Select(p => p.Seat).Where(seat => seat != state.player.Seat))
                        ComparePlayer(cardState.players.Single(p => p.Seat == playerSeat), cloudCardState.players.Single(p => p.Seat == playerSeat));
                }
            }
            else if (same)
            {
                Debug.WriteLine("client-sent and cloud-saved states are identical!");
            }
        }

        private static void ComparePassResults<OT>(SuggestPassState<OT> state, List<Card> pass, object baseBot) where OT : GameOptions
        {
            CompareResults<SuggestPassState<OT>, OT>(state, () =>
            {
                var passJson = JsonConvert.SerializeObject(SuggestSorter.SortCardList(pass));
                var cloudPassJson = JsonConvert.SerializeObject(SuggestSorter.SortCardList(state.cloudPass));
                state.cloudPass = null;

                var same = passJson == cloudPassJson;
                Debug.WriteLineIf(!same,
                    $"\nSeat {state.player.Seat}: Bot-suggested pass of {passJson} mismatches the cloud-suggested pass of {cloudPassJson} ({state.options.gameCode}).");
                return same;
            });
        }

        private static void CompareOptions<OT>(OT options, OT savedOptions) where OT : GameOptions
        {
            var optionsJson = JsonConvert.SerializeObject(options);
            var savedOptionsJson = JsonConvert.SerializeObject(savedOptions);
            Debug.WriteLineIf(optionsJson != savedOptionsJson,
                $"client-passed options do not match cloud-saved options:\n\tclient: {optionsJson}\n\t cloud: {savedOptionsJson}");

            switch (options.gameCode)
            {
                case GameCode.FiveHundred:
                {
                    var same = (options as FiveHundredOptions)._jokerSuit == (savedOptions as FiveHundredOptions)._jokerSuit;
                    var symbol = same ? "==" : "!=";
                    Debug.WriteLineIf(!same,
                        $"options._jokerSuit {symbol} savedOptions._jokerSuit ({(options as FiveHundredOptions)._jokerSuit} {symbol} {(savedOptions as FiveHundredOptions)._jokerSuit})");
                    break;
                }

                case GameCode.Pinochle:
                {
                    var same = JsonConvert.SerializeObject((options as PinochleOptions)._cardsDiscardedBySeat) ==
                               JsonConvert.SerializeObject((savedOptions as PinochleOptions)._cardsDiscardedBySeat);
                    var symbol = same ? "==" : "!=";
                    Debug.WriteLineIf(!same,
                        $"options._cardsDiscardedBySeat {symbol} savedOptions._cardsDiscardedBySeat"
                        + $" ({JsonConvert.SerializeObject((options as PinochleOptions)._cardsDiscardedBySeat)} {symbol} {JsonConvert.SerializeObject((savedOptions as PinochleOptions)._cardsDiscardedBySeat)})");

                    same = JsonConvert.SerializeObject((options as PinochleOptions)._cardsPassedBySeat) ==
                           JsonConvert.SerializeObject((savedOptions as PinochleOptions)._cardsPassedBySeat);
                    symbol = same ? "==" : "!=";
                    Debug.WriteLineIf(!same,
                        $"options._cardsPassedBySeat {symbol} savedOptions._cardsPassedBySeat"
                        + $" ({JsonConvert.SerializeObject((options as PinochleOptions)._cardsPassedBySeat)} {symbol} {JsonConvert.SerializeObject((savedOptions as PinochleOptions)._cardsPassedBySeat)})");

                    var cardsDiscardedByPlayerId = (options as PinochleOptions)._cardsDiscarded;
                    var cardsDiscardedByPlayerSeat = (options as PinochleOptions)._cardsDiscardedBySeat;
                    if (cardsDiscardedByPlayerId != null && cardsDiscardedByPlayerSeat != null)
                        foreach (var kvp in cardsDiscardedByPlayerId)
                        {
                            var discardedById = JsonConvert.SerializeObject(kvp.Value);
                            var discardedBySeat = JsonConvert.SerializeObject(cardsDiscardedByPlayerSeat[(int)kvp.Key - 1]);
                            Debug.WriteLineIf(discardedById != discardedBySeat,
                                $"_cardsDiscarded by player id {kvp.Key} mismatch those discarded by seat {kvp.Key - 1}!");
                        }

                    var cardsPassedByPlayerId = (options as PinochleOptions)._cardsPassed;
                    var cardsPassedByPlayerSeat = (options as PinochleOptions)._cardsPassedBySeat;
                    if (cardsPassedByPlayerId != null && cardsPassedByPlayerSeat != null)
                        foreach (var kvp in cardsPassedByPlayerId)
                        {
                            var passedById = JsonConvert.SerializeObject(kvp.Value);
                            var passedBySeat = JsonConvert.SerializeObject(cardsPassedByPlayerSeat[(int)kvp.Key - 1]);
                            Debug.WriteLineIf(passedById != passedBySeat, $"_cardsPassed by player id {kvp.Key} mismatch those passed by seat {kvp.Key - 1}!");
                        }

                    break;
                }

                case GameCode.Pitch:
                {
                    var same = (options as PitchOptions)._callPartnerSeat == (savedOptions as PitchOptions)._callPartnerSeat;
                    var symbol = same ? "==" : "!=";
                    Debug.WriteLineIf(!same,
                        $"options._callPartnerSeat {symbol} savedOptions._callPartnerSeat ({(options as PitchOptions)._callPartnerSeat} {symbol} {(savedOptions as PitchOptions)._callPartnerSeat})");
                    break;
                }

                case GameCode.Whist:
                {
                    var same = (options as WhistOptions)._highBidderSeat == (savedOptions as WhistOptions)._highBidderSeat;
                    var symbol = same ? "==" : "!=";
                    Debug.WriteLineIf(!same,
                        $"options._highBidderSeat {symbol} savedOptions._highBidderSeat ({(options as WhistOptions)._highBidderSeat?.ToString() ?? "null"} {symbol} {(savedOptions as WhistOptions)._highBidderSeat?.ToString() ?? "null"})");

                    same = (options as WhistOptions)._lowIsHigh == (savedOptions as WhistOptions)._lowIsHigh;
                    symbol = same ? "==" : "!=";
                    Debug.WriteLineIf(!same,
                        $"options._lowIsHigh {symbol} savedOptions._lowIsHigh ({(options as WhistOptions)._lowIsHigh} {symbol} {(savedOptions as WhistOptions)._lowIsHigh})");
                    break;
                }
            }
        }
#endif
    }
}