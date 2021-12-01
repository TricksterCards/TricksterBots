using System.Web.Http;
using Trickster.cloud;

namespace Trickster.Bots.Controllers
{
    public class EuchreController : ApiController
    {
        [HttpPost]
        [Route("suggest/euchre/card")]
        public string SuggestEuchreCard([FromBody] string postData)
        {
            return Suggester.SuggestNextCard<EuchreOptions>(postData, state => new EuchreBot(state.options, state.trumpSuit));
        }

        [HttpPost]
        [Route("suggest/euchre/bid")]
        public string SuggestEuchreBid([FromBody] string postData)
        {
            return Suggester.SuggestBid<EuchreOptions>(postData, state => new EuchreBot(state.options, Suit.Unknown));
        }

        [HttpPost]
        [Route("suggest/euchre/discard")]
        public string SuggestEuchreDiscard([FromBody] string postData)
        {
            return Suggester.SuggestDiscard<EuchreOptions>(postData, state => new EuchreBot(state.options, state.trumpSuit));
        }
    }
}