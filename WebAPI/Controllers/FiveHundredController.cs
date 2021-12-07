using System.Web.Http;
using Trickster.cloud;

namespace Trickster.Bots.Controllers
{
    public class FiveHundredController : ApiController
    {
        [HttpPost]
        [Route("suggest/fivehundred/bid")]
        public string SuggestFiveHundredBid([FromBody] string postData)
        {
            return Suggester.SuggestBid<FiveHundredOptions>(postData, state => new FiveHundredBot(state.options, state.trumpSuit));
        }

        [HttpPost]
        [Route("suggest/fivehundred/card")]
        public string SuggestFiveHundredCard([FromBody] string postData)
        {
            return Suggester.SuggestNextCard<FiveHundredOptions>(postData, state => new FiveHundredBot(state.options, state.trumpSuit));
        }

        [HttpPost]
        [Route("suggest/fivehundred/discard")]
        public string SuggestFiveHundredDiscard([FromBody] string postData)
        {
            return Suggester.SuggestDiscard<FiveHundredOptions>(postData, state => new FiveHundredBot(state.options, state.trumpSuit));
        }
    }
}