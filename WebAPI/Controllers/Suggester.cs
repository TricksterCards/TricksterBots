using System;
using System.CodeDom;
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
            var state = JsonSerializer.Deserialize<SuggestBidState<OT>>(postData);

            if (state == null)
                return null;

#if DEBUG
            CompareOptions(state.options, state.player.Seat);
#endif

            state.SortCardMembers();
            var bot = getBot(state);

            BidBase bid;
            try
            {
                bid = bot.SuggestBid(state);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            Debug.Assert(state.legalBids.Any(lb => lb.value == bid.value));

            if (bid?.value != state.cloudBid.value)
                Debug.WriteLine($"Bot-suggested bid of {bid?.value.ToString() ?? "null"} mismatches the cloud-suggested bid of {state.cloudBid.value}.");

            return JsonSerializer.Serialize(bid);
        }

        public static string SuggestDiscard<OT>(string postData, Func<SuggestDiscardState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestDiscardState<OT>>(postData);

            if (state == null)
                return null;

#if DEBUG
            CompareOptions(state.options, state.player.Seat);
#endif

            state.SortCardMembers();
            var bot = getBot(state);

            List<Card> discard;
            try
            {
                discard = bot.SuggestDiscard(state);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            return JsonSerializer.Serialize(discard.Select(SuitRank.FromCard));
        }

        public static string SuggestNextCard<OT>(string postData, Func<SuggestCardState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestCardState<OT>>(postData);

            if (state == null || state.legalCards.Count == 0)
                return null;

            //  if there's only one card, play it
            if (state.legalCards.Count == 1)
                return JsonSerializer.Serialize(SuitRank.FromCard(state.legalCards[0]));

#if DEBUG
            CompareOptions(state.options, state.player.Seat);
#endif

            state.SortCardMembers();
            var bot = getBot(state);

            Card card;
            try
            {
                card = bot.SuggestNextCard(state);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            Debug.Assert(state.legalCards.Any(lc => lc.SameAs(card)));

            var cloudCard = state.cloudCard;
            if (card?.suit != cloudCard.suit || card.rank != cloudCard.rank)
                Debug.WriteLine(
                    $"\nBot-suggested card of {card?.rank.ToString() ?? "null"} of {card?.suit.ToString() ?? "null"} mismatches the cloud-suggested card of {cloudCard.rank} of {cloudCard.suit}.");

            return JsonSerializer.Serialize(card != null ? SuitRank.FromCard(card) : null);
        }

        public static string SuggestPass<OT>(string postData, Func<SuggestPassState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestPassState<OT>>(postData);

            if (state == null)
                return null;

#if DEBUG
            CompareOptions(state.options, state.player.Seat);
#endif

            state.SortCardMembers();
            var bot = getBot(state);

            List<Card> pass;
            try
            {
                pass = bot.SuggestPass(state);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }

            return JsonSerializer.Serialize(pass.Select(SuitRank.FromCard));
        }

#if DEBUG
        private static readonly GameCode[] careAbout = { GameCode.FiveHundred, GameCode.Pitch, GameCode.Whist };

        private static void CompareOptions<OT>(OT options, int seat) where OT : GameOptions
        {
            if (!careAbout.Contains(options.gameCode))
                return;

            var cc = HttpContext.Current;
            var controllersPath = cc.Request.MapPath(".");
            var savePath = Path.GetFullPath($@"{controllersPath}\..\..\..\..\options_seat{seat}.json");
            Debug.WriteLine($"Reading game options from '{savePath}'");
            var savedJson = File.ReadAllText(savePath);
            var savedOptions = JsonConvert.DeserializeObject<OT>(savedJson);

            if (typeof(OT) == typeof(FiveHundredOptions))
            {
                Debug.WriteLine("OT is FiveHundredOptions!");
            }

            switch (options.gameCode)
            {
                case GameCode.FiveHundred:
                    if ((options as FiveHundredOptions)._jokerSuit != (savedOptions as FiveHundredOptions)._jokerSuit)
                        Debug.WriteLine(
                            $"options._jokerSuit != savedOptions._jokerSuit ({(options as FiveHundredOptions)._jokerSuit} != {(savedOptions as FiveHundredOptions)._jokerSuit})");

                    break;

                case GameCode.Pitch:
                    if ((options as PitchOptions)._callPartnerSeat != (savedOptions as PitchOptions)._callPartnerSeat)
                        Debug.WriteLine(
                            $"options._callPartnerSeat != savedOptions._callPartnerSeat ({(options as PitchOptions)._callPartnerSeat} != {(savedOptions as PitchOptions)._callPartnerSeat})");

                    break;

                case GameCode.Whist:
                    if ((options as WhistOptions)._highBidderSeat != (savedOptions as WhistOptions)._highBidderSeat)
                        Debug.WriteLine(
                            $"options._highBidderSeat != savedOptions._highBidderSeat ({(options as WhistOptions)._highBidderSeat?.ToString() ?? "null"} != {(savedOptions as WhistOptions)._highBidderSeat?.ToString() ?? "null"})");

                    if ((options as WhistOptions)._lowIsHigh != (savedOptions as WhistOptions)._lowIsHigh)
                        Debug.WriteLine(
                            $"options._lowIsHigh != savedOptions._lowIsHigh ({(options as WhistOptions)._lowIsHigh} != {(savedOptions as WhistOptions)._lowIsHigh})");

                    break;
            }
        }
#endif
    }
}