using System;
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

            var bot = getBot(state);
            var bid = bot.SuggestBid(state);
            var returnBid = state.legalBids.SingleOrDefault(lb => lb.value == bid.value);

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

            var bot = getBot(state);
            var card = bot.SuggestNextCard(state);
            var returnCard = state.legalCards.SingleOrDefault(lc => lc.SameAs(card));

            return JsonSerializer.Serialize(returnCard != null ? SuitRank.FromCard(card) : null);
        }
        
        public static string SuggestPass<OT>(string postData, Func<SuggestPassState<OT>, BaseBot<OT>> getBot)
            where OT : GameOptions
        {
            var state = JsonSerializer.Deserialize<SuggestPassState<OT>>(postData);

            if (state == null)
                return null;

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

            var bot = getBot(state);
            var discard = bot.SuggestDiscard(state);
            return JsonSerializer.Serialize(discard.Select(SuitRank.FromCard));
        }
    }
}