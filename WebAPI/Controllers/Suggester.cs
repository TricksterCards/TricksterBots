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

#if DEBUG
            CompareBidResults(state, bid, bot);
#endif

            return JsonSerializer.Serialize(bid);
        }

        public static string SuggestDiscard<OT>(string postData, Func<SuggestDiscardState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestDiscardState<OT>>(FixPostedJson(postData));

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
            var state = JsonSerializer.Deserialize<SuggestCardState<OT>>(FixPostedJson(postData));

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
            CompareCardResults(state, card, bot);
#endif

            return JsonSerializer.Serialize(SuitRank.FromCard(card));
        }

        public static string SuggestPass<OT>(string postData, Func<SuggestPassState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestPassState<OT>>(FixPostedJson(postData));

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
            Debug.Assert(state.legalBids.Any(lb => lb.value == bid.value));

            var cloudBid = state.cloudBid;
            if (bid.value == cloudBid.value)
                return;

            Debug.WriteLine(
                $"\nSeat {state.player.Seat}: Bot-suggested bid of {bid.value} mismatches the cloud-suggested bid of {cloudBid.value} ({state.options.gameCode}).");

            SuggestBidState<OT> cloudState;
            try
            {
                cloudState = LoadState<SuggestBidState<OT>>(state.player.Seat);

                if (cloudState == default)
                {
                    Debug.WriteLine("Null cloud state returned.");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return;
            }

            //  the saved state doesn't have cloudBid. save it, set it to null, and restore it so we don't create unnecessary differences.
            state.cloudBid = null;
            var stateJson = JsonConvert.SerializeObject(state);
            state.cloudBid = cloudBid;

            var cloudStateJson = JsonConvert.SerializeObject(cloudState);

            if (stateJson != cloudStateJson)
            {
                Debug.WriteLine($"client-sent and cloud-saved states differ.\nclient: {stateJson}\n cloud: {cloudStateJson}");

                CompareOptions(state.options, cloudState.options);
            }
            else
            {
                Debug.WriteLine("client-sent and cloud-saved states are identical!");
            }
        }

        private static void CompareCardResults<OT>(SuggestCardState<OT> state, Card botCard, BaseBot<OT> bot) where OT : GameOptions
        {
            Debug.Assert(state.legalCards.Any(lc => lc.SameAs(botCard)));

            var cloudCard = state.cloudCard;
            if (botCard.suit == cloudCard.suit && botCard.rank == cloudCard.rank)
                return;

            Debug.WriteLine(
                $"\nSeat {state.player.Seat}: Bot-suggested card of {botCard.rank} of {botCard.suit} mismatches the cloud-suggested card of {cloudCard.rank} of {cloudCard.suit} ({state.options.gameCode}).");

            SuggestCardState<OT> cloudState;
            try
            {
                cloudState = LoadState<SuggestCardState<OT>>(state.player.Seat);

                if (cloudState == default)
                {
                    Debug.WriteLine("Null cloud state returned.");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return;
            }

            //  the saved state doesn't have cloudCard. save it, set it to null, and restore it so we don't create unnecessary differences.
            state.cloudCard = null;
            var stateJson = JsonConvert.SerializeObject(state);
            state.cloudCard = cloudCard;

            var cloudStateJson = JsonConvert.SerializeObject(cloudState);

            if (stateJson != cloudStateJson)
            {
                Debug.WriteLine($"client-sent and cloud-saved states differ.\nclient: {stateJson}\n cloud: {cloudStateJson}");

                var card2 = bot.SuggestNextCard(cloudState);
                if (card2.suit == cloudCard.suit && card2.rank == cloudCard.rank)
                    Debug.WriteLine($"Using cloud-saved state, bot returns expected card of {card2.rank} of {card2.suit}.");
                else
                    Debug.WriteLine($"Even using cloud-saved state, bot returns wrong card of {card2.rank} of {card2.suit}!");

                CompareCardsPlayed(state, cloudState);
                CompareOptions(state.options, cloudState.options);
                ComparePlayer(state.player, cloudState.player);

                foreach (var playerSeat in state.players.Select(p => p.Seat).Where(seat => seat != state.player.Seat))
                    ComparePlayer(state.players.Single(p => p.Seat == playerSeat), cloudState.players.Single(p => p.Seat == playerSeat));
            }
            else
            {
                Debug.WriteLine("client-sent and cloud-saved states are identical!");
            }
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

        private static T LoadState<T>(int seat)
        {
            var savePath = string.Empty;

            try
            {
                var cc = HttpContext.Current;
                var apiPath = cc.Request.MapPath(".");
                //  will be path like GitHub/TricksterBot/WebApi/suggest/pinochle/card

                savePath = Path.GetFullPath($@"{apiPath}\..\..\..\..\state_seat{seat}.json");
                var savedJson = File.ReadAllText(savePath);
                var loadedState = JsonConvert.DeserializeObject<T>(savedJson);
                Debug.WriteLineIf(loadedState == null, $"Loaded null state from {savePath}!");
                return loadedState;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{ex.Message} attempting to load {savePath}");
                return default;
            }
        }

        private static void CompareDiscardResults<OT>(SuggestDiscardState<OT> state, List<Card> discard, object baseBot) where OT : GameOptions
        {
            var hand = new Hand(state.hand);
            Debug.Assert(discard.All(c => hand.ContainsCard(c)));

            var cloudDiscard = state.cloudDiscard;
            var discardJson = JsonConvert.SerializeObject(SuggestSorter.SortCardList(discard));
            var cloudDiscardJson = JsonConvert.SerializeObject(SuggestSorter.SortCardList(cloudDiscard));
            if (discardJson == cloudDiscardJson)
                return;

            Debug.WriteLine(
                $"\nSeat {state.player.Seat}: Bot-suggested discard of {discardJson} mismatches the cloud-suggested discard of {cloudDiscardJson} ({state.options.gameCode}).");

            SuggestDiscardState<OT> cloudState;
            try
            {
                cloudState = LoadState<SuggestDiscardState<OT>>(state.player.Seat);

                if (cloudState == default)
                {
                    Debug.WriteLine("Null cloud state returned.");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return;
            }

            //  the saved state doesn't have cloudDiscard. save it, set it to null, and restore it so we don't create unnecessary differences.
            state.cloudDiscard = null;
            var stateJson = JsonConvert.SerializeObject(state);
            state.cloudDiscard = cloudDiscard;

            var cloudStateJson = JsonConvert.SerializeObject(cloudState);

            if (stateJson != cloudStateJson)
            {
                Debug.WriteLine($"client-sent and cloud-saved states differ.\nclient: {stateJson}\n cloud: {cloudStateJson}");

                CompareOptions(state.options, cloudState.options);
            }
            else
            {
                Debug.WriteLine("client-sent and cloud-saved states are identical!");
            }
        }

        private static void ComparePassResults<OT>(SuggestPassState<OT> state, List<Card> pass, object baseBot) where OT : GameOptions
        {
            var hand = new Hand(state.hand);
            Debug.Assert(pass.Count == state.passCount && pass.All(c => hand.ContainsCard(c)));

            var cloudPass = state.cloudPass;
            var passJson = JsonConvert.SerializeObject(SuggestSorter.SortCardList(pass));
            var cloudPassJson = JsonConvert.SerializeObject(SuggestSorter.SortCardList(cloudPass));
            if (passJson == cloudPassJson)
                return;

            Debug.WriteLine(
                $"\nSeat {state.player.Seat}: Bot-suggested pass of {passJson} mismatches the cloud-suggested pass of {cloudPassJson} ({state.options.gameCode}).");

            SuggestPassState<OT> cloudState;
            try
            {
                cloudState = LoadState<SuggestPassState<OT>>(state.player.Seat);

                if (cloudState == default)
                {
                    Debug.WriteLine("Null cloud state returned.");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return;
            }

            //  the saved state doesn't have cloudPass. save it, set it to null, and restore it so we don't create unnecessary differences.
            state.cloudPass = null;
            var stateJson = JsonConvert.SerializeObject(state);
            state.cloudPass = cloudPass;

            var cloudStateJson = JsonConvert.SerializeObject(cloudState);

            if (stateJson != cloudStateJson)
            {
                Debug.WriteLine($"client-sent and cloud-saved states differ.\nclient: {stateJson}\n cloud: {cloudStateJson}");

                CompareOptions(state.options, cloudState.options);
            }
            else
            {
                Debug.WriteLine("client-sent and cloud-saved states are identical!");
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