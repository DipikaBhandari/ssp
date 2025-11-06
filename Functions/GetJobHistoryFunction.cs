using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using WeatherImageApp.Services;

namespace WeatherImageApp.Functions
{
    public class GetJobHistory
    {
        private readonly ILogger<GetJobHistory> _logger;
        private readonly ITableStorageService _tableStorageService;

        public GetJobHistory(ILogger<GetJobHistory> logger, ITableStorageService tableStorageService)
        {
            _logger = logger;
            _tableStorageService = tableStorageService;
        }

        [Function("GetJobHistory")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "jobs/history")] HttpRequestData req)
        {
            _logger.LogInformation("Getting job history");

            try
            {
                var jobs = await _tableStorageService.GetAllJobsAsync();
                
                // Sort by StartTime descending (most recent first)
                var sortedJobs = jobs.OrderByDescending(j => j.StartTime).ToList();
                
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(sortedJobs);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching job history");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new { error = ex.Message });
                return response;
            }
        }
    }
}
