using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeatherImageApp.Models;
using WeatherImageApp.Services;

namespace WeatherImageApp.Functions;

/// <summary>
/// Queue-triggered function that starts a weather image job
/// Triggered by messages in job-request-queue
/// </summary>
public class StartJobQueueFunction
{
    private readonly ILogger<StartJobQueueFunction> _logger;
    private readonly IWeatherService _weatherService;
    private readonly IQueueService _queueService;
    private readonly ITableStorageService _tableStorageService;

    public StartJobQueueFunction(
        ILogger<StartJobQueueFunction> logger,
        IWeatherService weatherService,
        IQueueService queueService,
        ITableStorageService tableStorageService)
    {
        _logger = logger;
        _weatherService = weatherService;
        _queueService = queueService;
        _tableStorageService = tableStorageService;
    }

    [Function("StartJobQueue")]
    public async Task Run(
        [QueueTrigger("job-request-queue", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        _logger.LogInformation($"Processing job request from queue: {queueMessage.Substring(0, Math.Min(100, queueMessage.Length))}...");

        try
        {
            var message = JsonSerializer.Deserialize<JobRequestMessage>(queueMessage);
            if (message == null || string.IsNullOrEmpty(message.JobId))
            {
                _logger.LogWarning("Invalid job request message format");
                return;
            }

            var jobId = message.JobId;
            _logger.LogInformation($"[JobId {jobId}] Starting weather image generation job");

            // Fetch weather stations
            _logger.LogInformation($"[JobId {jobId}] Fetching weather station data...");
            var stations = await _weatherService.GetWeatherStationsAsync(message.MaxStations);
            
            if (stations.Count == 0)
            {
                _logger.LogWarning($"[JobId {jobId}] No weather stations retrieved");
                
                // Update job status to failed
                var failedJobStatus = new JobStatus
                {
                    JobId = jobId,
                    Status = "failed",
                    StartTime = DateTime.UtcNow,
                    CompletedTime = DateTime.UtcNow,
                    TotalStations = 0,
                    ProcessedStations = 0
                };
                await _tableStorageService.SaveJobStatusAsync(failedJobStatus);
                return;
            }

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

            // Enqueue image processing jobs to image-processing-queue (fan-out pattern)
            await _queueService.EnqueueImageProcessingJobsAsync(jobId, stations);
            
            _logger.LogInformation($"[JobId {jobId}] Started with {stations.Count} stations, enqueued to image-processing-queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job request from queue");
            throw; // Will trigger retry mechanism
        }
    }
}
