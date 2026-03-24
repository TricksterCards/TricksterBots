using Microsoft.AspNetCore.Mvc;
using Trickster.cloud;

namespace Trickster.Bots.Controllers
{
    [ApiController]
    public class BridgeController : ControllerBase
    {
        [HttpPost]
        [Route("suggest/bridge/bid")]
        public string? SuggestBridgeBid([FromBody] string postData)
        {
            return Suggester.SuggestBid<BridgeOptions>(postData, state => new BridgeBot(state.options, Suit.Unknown));
        }

        [HttpPost]
        [Route("suggest/bridge/card")]
        public string? SuggestBridgeCard([FromBody] string postData)
        {
            return Suggester.SuggestNextCard<BridgeOptions>(postData, state => new BridgeBot(state.options, state.trumpSuit));
        }
    }
}