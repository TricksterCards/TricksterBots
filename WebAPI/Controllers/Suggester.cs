using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Trickster.cloud;

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

            state.SortCardMembers();
            var bot = getBot(state);
            var bid = bot.SuggestBid(state);
            var returnBid = state.legalBids.SingleOrDefault(lb => lb.value == bid.value);

            if (returnBid?.value != state.cloudBid.value)
            {
                Debug.WriteLine($"Bot-suggested bid of {bid?.value.ToString() ?? "null"} mismatches the cloud-suggested bid of {state.cloudBid.value}.");

                try
                {
                    var lastCloudState = File.ReadAllText($@"C:\Users\tedjo\LastBidState_{state.player.Seat}.json");
                    state.cloudBid = null;
                    state.options = null;
                    Debug.WriteLine($"Last used cloud state:\n{lastCloudState}\nCalled state:\n{JsonSerializer.Serialize(state)}\n");
                }
                catch
                {
                    //  ignore
                }
            }

            return JsonSerializer.Serialize(returnBid);
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

            state.SortCardMembers();
            var bot = getBot(state);
            var card = bot.SuggestNextCard(state);

            Debug.Assert(state.legalCards.Any(lc => lc.SameAs(card)));

            var cloudCard = state.cloudCard;
            if (card?.suit != cloudCard.suit || card.rank != cloudCard.rank)
            {
                Debug.WriteLine($"\nBot-suggested card of {card?.rank.ToString() ?? "null"} of {card?.suit.ToString() ?? "null"} mismatches the cloud-suggested card of {cloudCard.rank} of {cloudCard.suit}.");

                try
                {
                    var lastCloudStateJson = File.ReadAllText($@"C:\Users\tedjo\LastCardState_{state.player.Seat}.json");
                    var cloudState = JsonSerializer.Deserialize<SuggestCardState<OT>>(lastCloudStateJson);
                    Debug.Assert(cloudState != null, nameof(cloudState) + " != null");

                    if (state.trumpSuit != cloudState.trumpSuit)
                        Debug.WriteLine($"Client-sent state has trumpSuit of {state.trumpSuit} whereas cloud state has trumpSuit of {cloudState.trumpSuit}.");

                    //  this isn't sent by the cloud
                    state.cloudCard = null;

                    Debug.WriteLine($"Last used cloud state:\n{lastCloudStateJson}\nCalled state:\n{JsonSerializer.Serialize(state)}");

                    var bot2 = getBot(cloudState);
                    var card2 = bot2.SuggestNextCard(cloudState);
                    Debug.WriteLine($"Invoking SuggestNextCard using last used cloud state returned {card2.rank} of {card2.suit}, "
                                    + $"which is {(card2.suit == cloudCard.suit && card2.rank == cloudCard.rank ? "correct." : "WRONG!")}");

                    if (state.trumpSuit != cloudState.trumpSuit)
                    {
                        state.trumpSuit = cloudState.trumpSuit;
                        var bot3 = getBot(state);
                        var card3 = bot3.SuggestNextCard(state);
                        Debug.WriteLine($"Invoking SuggestNextCard using client-sent state with corrected trumpSuit returned {card3.rank} of {card3.suit}, "
                                        + $"which is {(card3.suit == cloudCard.suit && card3.rank == cloudCard.rank ? "correct." : "WRONG!")}");
                    }

                    Debug.WriteLine($"Returning expected card {cloudCard.rank} of {cloudCard.suit}.");
                    return JsonSerializer.Serialize(cloudCard);
                }
                catch
                {
                    //  ignore
                }
            }

            return JsonSerializer.Serialize(card != null ? SuitRank.FromCard(card) : null);
        }
        
        public static string SuggestPass<OT>(string postData, Func<SuggestPassState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestPassState<OT>>(postData);

            if (state == null)
                return null;

            state.SortCardMembers();
            var bot = getBot(state);
            var pass = bot.SuggestPass(state);
            return JsonSerializer.Serialize(pass.Select(SuitRank.FromCard));
        }
        
        public static string SuggestDiscard<OT>(string postData, Func<SuggestDiscardState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestDiscardState<OT>>(postData);

            if (state == null)
                return null;

            state.SortCardMembers();
            var bot = getBot(state);
            var discard = bot.SuggestDiscard(state);
            return JsonSerializer.Serialize(discard.Select(SuitRank.FromCard));
        }
    }
}