using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeatherImageApp.Models;

namespace WeatherImageApp.Services;

public class QueueService : IQueueService
{
    private readonly QueueClient _imageQueueClient;
    private readonly ILogger<QueueService> _logger;
    private readonly IJobStatusService _jobStatusService;

    public QueueService(
        IConfiguration configuration, 
        ILogger<QueueService> logger,
        IJobStatusService jobStatusService)
    {
        _logger = logger;
        _jobStatusService = jobStatusService;
        var connectionString = configuration["StorageConnectionString"] 
                              ?? configuration["AzureWebJobsStorage"]
                              ?? throw new InvalidOperationException("Storage connection string not found");

        _imageQueueClient = new QueueClient(connectionString, "image-processing-queue");
    }

    public async Task EnqueueJobAsync(string jobId, List<WeatherStation> stations)
    {
        try
        {
            await _imageQueueClient.CreateIfNotExistsAsync();
            
            // Create job status
            await _jobStatusService.CreateJobAsync(jobId, stations.Count);

            // Enqueue a message for each station (fan-out pattern)
            for (int i = 0; i < stations.Count; i++)
            {
                var message = new ImageProcessingMessage
                {
                    JobId = jobId,
                    Station = stations[i],
                    StationIndex = i,
                    TotalStations = stations.Count
                };

                var messageJson = JsonSerializer.Serialize(message);
                // Encode as Base64 (required by Azure Storage Queues)
                var messageBytes = System.Text.Encoding.UTF8.GetBytes(messageJson);
                var messageBase64 = Convert.ToBase64String(messageBytes);
                await _imageQueueClient.SendMessageAsync(messageBase64);
            }

            _logger.LogInformation($"[JobId {jobId}] Enqueued {stations.Count} image processing messages");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error enqueuing job {jobId}");
            throw;
        }
    }

    public Task<JobStatus?> GetJobStatusAsync(string jobId)
    {
        return _jobStatusService.GetJobStatusAsync(jobId);
    }
}
