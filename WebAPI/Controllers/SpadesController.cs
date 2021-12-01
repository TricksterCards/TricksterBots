using System.Web.Http;
using Trickster.cloud;

namespace Trickster.Bots.Controllers
{
    public class SpadesController : ApiController
    {
        [HttpPost]
        [Route("suggest/spades/bid")]
        public string SuggestSpadesBid([FromBody] string postData)
        {
            return Suggester.SuggestBid<SpadesOptions>(postData, state => new SpadesBot(state.options, Suit.Unknown));
        }

        [HttpPost]
        [Route("suggest/spades/card")]
        public string SuggestSpadesCard([FromBody] string postData)
        {
            return Suggester.SuggestNextCard<SpadesOptions>(postData, state => new SpadesBot(state.options, state.trumpSuit));
        }
    }
}