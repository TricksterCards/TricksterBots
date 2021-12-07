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
            return Suggester.SuggestBid<OhHellOptions>(postData, state =>
            {
                System.Diagnostics.Debug.WriteLineIf(state.trumpSuit != state.upCardSuit,
                    $"state.trumpSuit is {state.trumpSuit} while state.upCardSuit is {state.upCardSuit}");

                return new OhHellBot(state.options, state.upCardSuit);
            });
        }

        [HttpPost]
        [Route("suggest/ohhell/card")]
        public string SuggestOhHellCard([FromBody] string postData)
        {
            return Suggester.SuggestNextCard<OhHellOptions>(postData, state => new OhHellBot(state.options, state.trumpSuit));
        }
    }
}