using System.Web.Http;
using Trickster.cloud;

namespace Trickster.Bots.Controllers
{
    public class OhHellController : ApiController
    {
        [HttpPost]
        [Route("suggest/ohhell/bid")]
        public string SuggestOhHellBid([FromBody] string postData)
        {
            return Suggester.SuggestBid<OhHellOptions>(postData, state => new OhHellBot(state.options, state.trumpSuit));
        }

        [HttpPost]
        [Route("suggest/ohhell/card")]
        public string SuggestOhHellCard([FromBody] string postData)
        {
            return Suggester.SuggestNextCard<OhHellOptions>(postData, state => new OhHellBot(state.options, state.trumpSuit));
        }
    }
}