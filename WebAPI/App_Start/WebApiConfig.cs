using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Cors;

namespace WebBotsDotNet
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            var cors = new EnableCorsAttribute("https://www.trickstercards.com,https://tricksterwest.azurewebsites.net,http://localhost:63677", "*", "*");
            config.EnableCors(cors);

            // Web API routes
            config.MapHttpAttributeRoutes();

            //  this causes us to response to a request asking for text/html with json data (useful for debugging with Chrome)
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
        }
    }
}