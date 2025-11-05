using WeatherImageApp.Models;

namespace WeatherImageApp.Services;

public interface IQueueService
{
    Task EnqueueJobAsync(string jobId, List<WeatherStation> stations);
    Task<JobStatus?> GetJobStatusAsync(string jobId);
}
