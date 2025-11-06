using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace WeatherImageApp.Functions
{
    public class ServeHistory
    {
        private readonly ILogger<ServeHistory> _logger;

        public ServeHistory(ILogger<ServeHistory> logger)
        {
            _logger = logger;
        }

        [Function("ServeHistory")]
        public HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "history")] HttpRequestData req)
        {
            _logger.LogInformation("Serving history page");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/html; charset=utf-8");

            var possiblePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "history.html"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "history.html"),
                Path.Combine(AppContext.BaseDirectory, "history.html"),
                Path.Combine(AppContext.BaseDirectory, "wwwroot", "history.html"),
                Path.Combine(Environment.CurrentDirectory, "history.html"),
                Path.Combine(Environment.CurrentDirectory, "wwwroot", "history.html"),
                "wwwroot/history.html"
            };

            string? htmlContent = null;

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    htmlContent = File.ReadAllText(path);
                    _logger.LogInformation($"Found history HTML file at: {path}");
                    break;
                }
            }

            if (htmlContent != null)
            {
                response.WriteString(htmlContent);
            }
            else
            {
                response.StatusCode = HttpStatusCode.NotFound;
                response.WriteString("<h1>History page not found</h1>");
            }

            return response;
        }
    }
}
