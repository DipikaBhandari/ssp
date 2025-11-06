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

            // Try multiple possible paths
            var possiblePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "test-interface.html"),
                Path.Combine(AppContext.BaseDirectory, "wwwroot", "test-interface.html"),
                Path.Combine(Environment.CurrentDirectory, "wwwroot", "test-interface.html"),
                "wwwroot/test-interface.html"
            };

            string? htmlContent = null;
            string? foundPath = null;

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    htmlContent = File.ReadAllText(path);
                    foundPath = path;
                    _logger.LogInformation($"Found HTML file at: {path}");
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
                var errorHtml = $@"
                    <h1>Test interface file not found</h1>
                    <p>Searched paths:</p>
                    <ul>
                        {string.Join("", possiblePaths.Select(p => $"<li>{p} - Exists: {File.Exists(p)}</li>"))}
                    </ul>
                    <p>Current Directory: {Directory.GetCurrentDirectory()}</p>
                    <p>Base Directory: {AppContext.BaseDirectory}</p>
                ";
                response.WriteString(errorHtml);
            }

            return response;
        }
    }
}
