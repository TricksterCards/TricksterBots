using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Trickster.cloud;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Trickster.Bots.Controllers
{
    public class Suggester
    {
        public static string SuggestBid<OT>(string postData, Func<SuggestBidState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestBidState<OT>>(FixPostedJson(postData));

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

            Debug.Assert(state.legalBids.Any(lb => lb.value == bid.value));

            if (bid.value != state.cloudBid.value)
            {
                Debug.WriteLine(string.Empty);
                CompareBidState(state, bid);
                Debug.WriteLine($"Seat {state.player.Seat}: Bot-suggested bid of {bid.value} mismatches the cloud-suggested bid of {state.cloudBid.value} ({state.options.gameCode}).");
            }

            return JsonSerializer.Serialize(bid);
        }

        public static string SuggestDiscard<OT>(string postData, Func<SuggestDiscardState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestDiscardState<OT>>(FixPostedJson(postData));

            if (state == null)
                return null;

            state.SortCardMembers();

//#if DEBUG
//            CompareDiscardState(state);
//#endif

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

            return JsonSerializer.Serialize(discard.Select(SuitRank.FromCard));
        }

        public static string SuggestFromStateFile(string filename)
        {
            var cc = HttpContext.Current;
            var apiPath = cc.Request.MapPath(".");
            //  will be path like GitHub/TricksterBot/WebApi/test/filename

            var savePath = Path.GetFullPath($@"{apiPath}\..\Errors\{filename}.json");
            var savedJson = File.ReadAllText(savePath);

            var fnParts = filename.Split('_');
            Enum.TryParse(fnParts[1], out GameCode gameCode);

            switch (fnParts[0])
            {
                case "Card":
                    switch (gameCode)
                    {
                        case GameCode.Hearts:
                            break;
                        case GameCode.Spades:
                            break;
                        case GameCode.Euchre:
                            break;
                        case GameCode.Pitch:
                            break;
                        case GameCode.Bridge:
                            break;
                        case GameCode.FiveHundred:
                            break;
                        case GameCode.OhHell:
                            break;
                        case GameCode.Pinochle:
                            return SuggestNextCard<PinochleOptions>(FixJson<SuggestCardState<PinochleOptions>>(savedJson),
                                state => new PinochleBot(state.options, state.trumpSuit));
                        case GameCode.Whist:
                            break;
                        case GameCode.Unknown:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                case "Bid":
                    break;
            }

            return "Unimplemented game or suggestion type";
        }

        public static string SuggestNextCard<OT>(string postData, Func<SuggestCardState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestCardState<OT>>(FixPostedJson(postData));

            if (state == null || state.legalCards.Count == 0)
                return null;

            //  if there's only one card, play it
            if (state.legalCards.Count == 1)
                return JsonSerializer.Serialize(SuitRank.FromCard(state.legalCards[0]));

            state.SortCardMembers();

            CompareCardsPlayed(state);

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

            Debug.Assert(state.legalCards.Any(lc => lc.SameAs(card)));

            var cloudCard = state.cloudCard;
            if (card.suit != cloudCard.suit || card.rank != cloudCard.rank)
            {
                Debug.WriteLine(string.Empty);
                CompareCardState(state, card);
                Debug.WriteLine(
                    $"Seat {state.player.Seat}: Bot-suggested card of {card.rank} of {card.suit} mismatches the cloud-suggested card of {cloudCard.rank} of {cloudCard.suit} ({state.options.gameCode}).");
            }

            return JsonSerializer.Serialize(SuitRank.FromCard(card));
        }

        public static string SuggestPass<OT>(string postData, Func<SuggestPassState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestPassState<OT>>(FixPostedJson(postData));

            if (state == null)
                return null;

            state.SortCardMembers();

//#if DEBUG
//            ComparePassState(state);
//#endif

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

            return JsonSerializer.Serialize(pass.Select(SuitRank.FromCard));
        }

        private static string FixJson<T>(string savedJson)
        {
            return savedJson.Contains("\"r\":") ? JsonSerializer.Serialize(JsonConvert.DeserializeObject<T>(savedJson)) : savedJson;
        }

        private static string FixPostedJson(string postData)
        {
            return postData.Replace("\"r\":", "\"rank\":").Replace("\"s\":", "\"suit\":");
        }

#if DEBUG
        private static void CompareBidState<OT>(SuggestBidState<OT> state, BidBase botBid) where OT : GameOptions
        {
            var savedState = LoadState<SuggestBidState<OT>>(state.player.Seat);

            if (savedState == null)
                return;

            CompareOptions(state.options, savedState.options);

            SaveErrorState(state, savedState, $"Bid_{state.options.gameCode}_bot_{botBid}_cloud_{state.cloudBid}");
        }

        private static void CompareCardState<OT>(SuggestCardState<OT> state, Card botCard) where OT : GameOptions
        {
            var savedState = LoadState<SuggestCardState<OT>>(state.player.Seat);

            Debug.WriteLine($"Player in seat {state.player.Seat} is playing.");

            //  the saved state doesn't have cloudCard. save it, set it to null, and restore it so we don't create unnecessary differences.
            var cloudCard = state.cloudCard;
            state.cloudCard = null;
            var stateJson = JsonConvert.SerializeObject(state);
            state.cloudCard = cloudCard;

            var savedStateJson = JsonConvert.SerializeObject(savedState);
            Debug.WriteLineIf(stateJson != savedStateJson, $"client-sent and cloud-saved states differ.\nclient: {stateJson}\ncloud: {savedStateJson}");

            if (savedState == null)
                return;

            CompareOptions(state.options, savedState.options);
            ComparePlayer(state.player, savedState.player);

            foreach (var playerSeat in state.players.Select(p => p.Seat))
                ComparePlayer(state.players.Single(p => p.Seat == playerSeat), savedState.players.Single(p => p.Seat == playerSeat));

            SaveErrorState(state, savedState, $"Card_{state.options.gameCode}_bot_{botCard.StdNotation}_cloud_{new Card(state.cloudCard).StdNotation}");
        }

        private static void CompareCardsPlayed<OT>(SuggestCardState<OT> state) where OT : GameOptions
        {
            var savedState = LoadState<SuggestCardState<OT>>(state.player.Seat);

            if (savedState == null)
                return;

            var cardsPlayedJson = JsonConvert.SerializeObject(state.cardsPlayed);
            var savedCardsPlayedJson = JsonConvert.SerializeObject(savedState.cardsPlayed);

            Debug.WriteLineIf(cardsPlayedJson != savedCardsPlayedJson, $"state.cardsPlayed differs ({cardsPlayedJson} != {savedCardsPlayedJson})");
        }

        private static void SaveErrorState(object state, object cloudState, string filename)
        {
            try
            {
                var cc = HttpContext.Current;
                var apiPath = cc.Request.MapPath(".");
                //  will be path like GitHub/TricksterBot/WebApi/suggest/pinochle/card

                if (Directory.Exists($@"{apiPath}\..\..\Errors"))
                {
                    var savePath = Path.GetFullPath($@"{apiPath}\..\..\Errors\{filename}_client.json");
                    File.WriteAllText(savePath, JsonSerializer.Serialize(state));

                    savePath = Path.GetFullPath($@"{apiPath}\..\..\Errors\{filename}_cloud.json");
                    File.WriteAllText(savePath, JsonSerializer.Serialize(cloudState));
                }
            }
            catch
            {
                //  ignore
            }
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

/*
        private static void CompareDiscardState<OT>(SuggestDiscardState<OT> state) where OT : GameOptions
        {
            var savedState = LoadState<SuggestDiscardState<OT>>(state.player.Seat);

            if (savedState == null)
                return;

            CompareOptions(state.options, savedState.options);
        }
*/

/*
        private static void ComparePassState<OT>(SuggestPassState<OT> state) where OT : GameOptions
        {
            var savedState = LoadState<SuggestPassState<OT>>(state.player.Seat);

            if (savedState == null)
                return;

            CompareOptions(state.options, savedState.options);
        }
*/

        private static T LoadState<T>(int seat)
        {
            try
            {
                var cc = HttpContext.Current;
                var apiPath = cc.Request.MapPath(".");
                //  will be path like GitHub/TricksterBot/WebApi/suggest/pinochle/card

                var savePath = Path.GetFullPath($@"{apiPath}\..\..\..\..\state_seat{seat}.json");
                var savedJson = File.ReadAllText(savePath);
                return JsonConvert.DeserializeObject<T>(savedJson);
            }
            catch
            {
                return default;
            }
        }

        private static void CompareOptions<OT>(OT options, OT savedOptions) where OT : GameOptions
        {
            var optionsJson = JsonConvert.SerializeObject(options);
            var savedOptionsJson = JsonConvert.SerializeObject(savedOptions);
            Debug.WriteLineIf(optionsJson != savedOptionsJson,
                $"client-passed options do not match cloud-saved options:\nclient: {optionsJson}\ncloud: {savedOptionsJson}");

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