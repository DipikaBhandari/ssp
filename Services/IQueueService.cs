using WeatherImageApp.Models;

namespace WeatherImageApp.Services;

public interface IQueueService
{
    /// <summary>
    /// Enqueue a job request to job-request-queue
    /// </summary>
    Task EnqueueJobRequestAsync(string jobId, int maxStations = 36);
    
    /// <summary>
    /// Enqueue image processing messages to image-processing-queue (fan-out pattern)
    /// </summary>
    Task EnqueueImageProcessingJobsAsync(string jobId, List<WeatherStation> stations);
    
    /// <summary>
    /// Legacy method - now calls EnqueueImageProcessingJobsAsync
    /// </summary>
    [Obsolete("Use EnqueueImageProcessingJobsAsync instead")]
    Task EnqueueJobAsync(string jobId, List<WeatherStation> stations);
}
