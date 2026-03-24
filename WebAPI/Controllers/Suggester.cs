using System.Diagnostics;
using System.Text.Json;
using Trickster.cloud;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Trickster.Bots.Controllers
{
    public class Suggester
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { IncludeFields = true };

        public static string? SuggestBid<OT>(string postData, Func<SuggestBidState<OT>, BaseBot<OT>> getBot)
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

            return JsonSerializer.Serialize(bid);
        }

        public static string? SuggestDiscard<OT>(string postData, Func<SuggestDiscardState<OT>, BaseBot<OT>> getBot)
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

            return JsonSerializer.Serialize(discard.Select(SuitRank.FromCard));
        }

        public static string? SuggestNextCard<OT>(string postData, Func<SuggestCardState<OT>, BaseBot<OT>> getBot)
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

            return JsonSerializer.Serialize(SuitRank.FromCard(card));
        }

        public static string? SuggestPass<OT>(string postData, Func<SuggestPassState<OT>, BaseBot<OT>> getBot)
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

            return JsonSerializer.Serialize(pass.Select(SuitRank.FromCard));
        }

        private static string FixPostedJson(string postData)
        {
            return postData.Replace("\"r\":", "\"rank\":").Replace("\"s\":", "\"suit\":");
        }
    }
}