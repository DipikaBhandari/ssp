using WeatherImageApp.Models;

namespace WeatherImageApp.Services;

public interface ITableStorageService
{
    Task SaveJobStatusAsync(JobStatus jobStatus);
    Task<JobStatus?> GetJobStatusAsync(string jobId);
    Task UpdateJobProgressAsync(string jobId, int processedStations, int totalStations);
    Task CompleteJobAsync(string jobId);
}
