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
    private readonly IQueueService _queueService;
    private readonly IConfiguration _configuration;

    public StartJobFunction(
        ILogger<StartJobFunction> logger,
        IQueueService queueService,
        IConfiguration configuration)
    {
        _logger = logger;
        _queueService = queueService;
        _configuration = configuration;
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

        _logger.LogInformation("Received request to start new weather image generation job");

        try
        {
            // Generate unique job ID
            var jobId = Guid.NewGuid().ToString();
            
            _logger.LogInformation($"[JobId {jobId}] Enqueueing job request to job-request-queue");
            
            // Enqueue job request to job-request-queue
            // The StartJobQueueFunction will handle fetching stations and processing
            await _queueService.EnqueueJobRequestAsync(jobId, maxStations: 36);
            
            _logger.LogInformation($"[JobId {jobId}] Job request enqueued successfully");

            // Return job ID immediately (async pattern)
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
