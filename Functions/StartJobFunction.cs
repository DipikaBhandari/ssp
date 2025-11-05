using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeatherImageApp.Services;
using WeatherImageApp.Models;
using WeatherImageApp.Middleware;
using System.Text.Json;

namespace WeatherImageApp.Functions;

public class StartJobFunction
{
    private readonly ILogger<StartJobFunction> _logger;
    private readonly IWeatherService _weatherService;
    private readonly IQueueService _queueService;
    private readonly IConfiguration _configuration;
    private readonly ITableStorageService _tableStorageService;

    public StartJobFunction(
        ILogger<StartJobFunction> logger,
        IWeatherService weatherService,
        IQueueService queueService,
        IConfiguration configuration,
        ITableStorageService tableStorageService)
    {
        _logger = logger;
        _weatherService = weatherService;
        _queueService = queueService;
        _configuration = configuration;
        _tableStorageService = tableStorageService;
    }

    [Function("StartJob")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "get", Route = "job/start")] HttpRequestData req)
    {
        // Validate API key
        var authResponse = await ApiKeyAuthAttribute.ValidateApiKeyAsync(req, _configuration, _logger);
        if (authResponse != null)
        {
            return authResponse;
        }

        _logger.LogInformation("Starting new weather image generation job");

        try
        {
            // Generate unique job ID
            var jobId = Guid.NewGuid().ToString();
            
            // Fetch weather stations
            _logger.LogInformation("Fetching weather station data...");
            var stations = await _weatherService.GetWeatherStationsAsync(36);
            
            if (stations.Count == 0)
            {
                _logger.LogWarning("No weather stations retrieved");
                var errorResponse = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
                await errorResponse.WriteAsJsonAsync(new { error = "Unable to fetch weather data" });
                return errorResponse;
            }

            // Enqueue processing jobs
            await _queueService.EnqueueJobAsync(jobId, stations);
            
            // Save initial job status to Table Storage
            var jobStatus = new JobStatus
            {
                JobId = jobId,
                Status = "processing",
                StartTime = DateTime.UtcNow,
                TotalStations = stations.Count,
                ProcessedStations = 0
            };
            await _tableStorageService.SaveJobStatusAsync(jobStatus);
            
            _logger.LogInformation($"Job {jobId} started with {stations.Count} stations");

            // Return job ID
            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new JobRequest
            {
                JobId = jobId,
                CreatedAt = DateTime.UtcNow
            });
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting job");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }
}
