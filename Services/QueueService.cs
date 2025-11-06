using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeatherImageApp.Models;

namespace WeatherImageApp.Services;

public class QueueService : IQueueService
{
    private readonly QueueClient _jobRequestQueueClient;
    private readonly QueueClient _imageQueueClient;
    private readonly ILogger<QueueService> _logger;

    public QueueService(
        IConfiguration configuration, 
        ILogger<QueueService> logger)
    {
        _logger = logger;
        var connectionString = configuration["StorageConnectionString"] 
                              ?? configuration["AzureWebJobsStorage"]
                              ?? throw new InvalidOperationException("Storage connection string not found");

        // Queue 1: For starting jobs
        _jobRequestQueueClient = new QueueClient(connectionString, "job-request-queue");
        
        // Queue 2: For fetching and updating images
        _imageQueueClient = new QueueClient(connectionString, "image-processing-queue");
    }

    public async Task EnqueueJobRequestAsync(string jobId, int maxStations = 36)
    {
        try
        {
            await _jobRequestQueueClient.CreateIfNotExistsAsync();
            
            var message = new JobRequestMessage
            {
                JobId = jobId,
                RequestedAt = DateTime.UtcNow,
                MaxStations = maxStations
            };

            var messageJson = JsonSerializer.Serialize(message);
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(messageJson);
            var messageBase64 = Convert.ToBase64String(messageBytes);
            
            await _jobRequestQueueClient.SendMessageAsync(messageBase64);
            
            _logger.LogInformation($"[JobId {jobId}] Enqueued job request to job-request-queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error enqueuing job request {jobId}");
            throw;
        }
    }

    public async Task EnqueueImageProcessingJobsAsync(string jobId, List<WeatherStation> stations)
    {
        try
        {
            await _imageQueueClient.CreateIfNotExistsAsync();

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

            _logger.LogInformation($"[JobId {jobId}] Enqueued {stations.Count} image processing messages to image-processing-queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error enqueuing image processing jobs {jobId}");
            throw;
        }
    }

    [Obsolete("Use EnqueueImageProcessingJobsAsync instead")]
    public Task EnqueueJobAsync(string jobId, List<WeatherStation> stations)
    {
        return EnqueueImageProcessingJobsAsync(jobId, stations);
    }
}
