using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeatherImageApp.Services;
using WeatherImageApp.Middleware;

namespace WeatherImageApp.Functions;

public class GetJobStatusFunction
{
    private readonly ILogger<GetJobStatusFunction> _logger;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IConfiguration _configuration;
    private readonly ITableStorageService _tableStorageService;

    public GetJobStatusFunction(
        ILogger<GetJobStatusFunction> logger,
        IBlobStorageService blobStorageService,
        IConfiguration configuration,
        ITableStorageService tableStorageService)
    {
        _logger = logger;
        _blobStorageService = blobStorageService;
        _configuration = configuration;
        _tableStorageService = tableStorageService;
    }

    [Function("GetJobStatus")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "job/{jobId}")] HttpRequestData req,
        string jobId)
    {
        // Validate API key
        var authResponse = await ApiKeyAuthAttribute.ValidateApiKeyAsync(req, _configuration, _logger);
        if (authResponse != null)
        {
            return authResponse;
        }

        _logger.LogInformation($"Retrieving status for job: {jobId}");

        try
        {
            // Get job status from Table Storage
            var jobStatus = await _tableStorageService.GetJobStatusAsync(jobId);
            
            if (jobStatus == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { error = "Job not found" });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(jobStatus);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting job status for {jobId}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = ex.Message });
            return errorResponse;
        }
    }
}
