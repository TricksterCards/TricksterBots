using System.Web.Http;
using Trickster.cloud;

namespace Trickster.Bots.Controllers
{
    public class PinochleController : ApiController
    {
        [HttpPost]
        [Route("suggest/pinochle/bid")]
        public string SuggestPinochleBid([FromBody] string postData)
        {
            return Suggester.SuggestBid<PinochleOptions>(postData, state => new PinochleBot(state.options, Suit.Unknown));
        }

        [HttpPost]
        [Route("suggest/pinochle/card")]
        public string SuggestPinochleCard([FromBody] string postData)
        {
            return Suggester.SuggestNextCard<PinochleOptions>(postData, state => new PinochleBot(state.options, state.trumpSuit));
        }

        [HttpPost]
        [Route("suggest/pinochle/discard")]
        public string SuggestPinochleDiscard([FromBody] string postData)
        {
            return Suggester.SuggestDiscard<PinochleOptions>(postData, state => new PinochleBot(state.options, state.trumpSuit));
        }

        [HttpPost]
        [Route("suggest/pinochle/pass")]
        public string SuggestPinochlePass([FromBody] string postData)
        {
            return Suggester.SuggestPass<PinochleOptions>(postData, state => new PinochleBot(state.options, state.trumpSuit));
        }
    }
}