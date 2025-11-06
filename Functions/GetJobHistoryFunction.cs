using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using System.Net;
using System.Text.Json;

namespace WeatherImageApp.Functions
{
    public class GetJobHistory
    {
        private readonly ILogger<GetJobHistory> _logger;
        private readonly TableClient _tableClient;

        public GetJobHistory(ILogger<GetJobHistory> logger, TableServiceClient tableServiceClient)
        {
            _logger = logger;
            _tableClient = tableServiceClient.GetTableClient("jobstatus");
        }

        [Function("GetJobHistory")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "jobs/history")] HttpRequestData req)
        {
            _logger.LogInformation("Getting job history");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            try
            {
                var jobs = new List<object>();
                
                // Query all jobs, ordered by timestamp descending
                await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
                    maxPerPage: 50))
                {
                    jobs.Add(new
                    {
                        JobId = entity.RowKey,
                        Status = entity.GetString("Status"),
                        StartTime = entity.GetDateTimeOffset("StartTime")?.DateTime,
                        CompletedTime = entity.GetDateTimeOffset("CompletedTime")?.DateTime,
                        TotalStations = entity.GetInt32("TotalStations"),
                        ProcessedStations = entity.GetInt32("ProcessedStations"),
                        ErrorMessage = entity.GetString("ErrorMessage")
                    });
                }

                // Sort by StartTime descending
                var sortedJobs = jobs.OrderByDescending(j => 
                    ((dynamic)j).StartTime ?? DateTime.MinValue).ToList();

                await response.WriteStringAsync(JsonSerializer.Serialize(sortedJobs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching job history");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            }

            return response;
        }
    }
}
