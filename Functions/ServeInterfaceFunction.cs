using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace WeatherImageApp.Functions
{
    public class ServeInterface
    {
        private readonly ILogger<ServeInterface> _logger;

        public ServeInterface(ILogger<ServeInterface> logger)
        {
            _logger = logger;
        }

        [Function("ServeInterface")]
        public HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "interface")] HttpRequestData req)
        {
            _logger.LogInformation("Serving test interface");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/html; charset=utf-8");

            var htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "test-interface.html");
            
            if (File.Exists(htmlPath))
            {
                var html = File.ReadAllText(htmlPath);
                response.WriteString(html);
            }
            else
            {
                response.StatusCode = HttpStatusCode.NotFound;
                response.WriteString("<h1>Test interface file not found</h1>");
            }

            return response;
        }
    }
}
