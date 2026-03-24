using Microsoft.AspNetCore.Mvc;
using Trickster.cloud;

namespace Trickster.Bots.Controllers
{
    [ApiController]
    public class WhistController : ControllerBase
    {
        [HttpPost]
        [Route("suggest/whist/bid")]
        public string? SuggestWhistBid([FromBody] string postData)
        {
            return Suggester.SuggestBid<WhistOptions>(postData, state => new WhistBot(state.options, Suit.Unknown));
        }

        [HttpPost]
        [Route("suggest/whist/card")]
        public string? SuggestWhistCard([FromBody] string postData)
        {
            return Suggester.SuggestNextCard<WhistOptions>(postData, state => new WhistBot(state.options, state.trumpSuit));
        }

        [HttpPost]
        [Route("suggest/whist/discard")]
        public string? SuggestWhistDiscard([FromBody] string postData)
        {
            return Suggester.SuggestDiscard<WhistOptions>(postData, state => new WhistBot(state.options, state.trumpSuit));
        }
    }
}