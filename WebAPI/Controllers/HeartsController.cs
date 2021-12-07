using System.Web.Http;
using Trickster.cloud;

namespace Trickster.Bots.Controllers
{
    public class HeartsController : ApiController
    {
        [HttpPost]
        [Route("suggest/hearts/card")]
        public string SuggestHeartsCard([FromBody] string postData)
        {
            return Suggester.SuggestNextCard<HeartsOptions>(postData, state => new HeartsBot(state.options, state.trumpSuit));
        }

        [HttpPost]
        [Route("suggest/hearts/pass")]
        public string SuggestHeartsPass([FromBody] string postData)
        {
            return Suggester.SuggestPass<HeartsOptions>(postData, state => new HeartsBot(state.options, Suit.Unknown));
        }
    }
}