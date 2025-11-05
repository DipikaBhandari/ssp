using System.Text.Json;
using Microsoft.Extensions.Logging;
using WeatherImageApp.Models;

namespace WeatherImageApp.Services;

public interface IJobStatusService
{
    Task<JobStatus?> GetJobStatusAsync(string jobId);
    Task CreateJobAsync(string jobId, int totalStations);
    Task UpdateJobProgressAsync(string jobId, string imageUrl);
    Task<List<JobStatus>> GetAllJobsAsync();
}

public class JobStatusService : IJobStatusService
{
    private readonly ILogger<JobStatusService> _logger;
    private readonly string _statusFilePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public JobStatusService(ILogger<JobStatusService> logger)
    {
        _logger = logger;
        _statusFilePath = Path.Combine(Directory.GetCurrentDirectory(), "job-status.json");
        
        // Initialize file if it doesn't exist
        if (!File.Exists(_statusFilePath))
        {
            File.WriteAllText(_statusFilePath, "{}");
        }
    }

    public async Task<JobStatus?> GetJobStatusAsync(string jobId)
    {
        await _fileLock.WaitAsync();
        try
        {
            var allJobs = await ReadJobsFromFileAsync();
            return allJobs.TryGetValue(jobId, out var status) ? status : null;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task CreateJobAsync(string jobId, int totalStations)
    {
        await _fileLock.WaitAsync();
        try
        {
            var allJobs = await ReadJobsFromFileAsync();
            
            var newJob = new JobStatus
            {
                JobId = jobId,
                Status = "processing",
                StartTime = DateTime.UtcNow,
                TotalStations = totalStations,
                ProcessedStations = 0,
                Images = new List<WeatherImageInfo>()
            };

            allJobs[jobId] = newJob;
            await WriteJobsToFileAsync(allJobs);
            
            _logger.LogInformation($"Created job {jobId} with {totalStations} stations");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task UpdateJobProgressAsync(string jobId, string imageUrl)
    {
        await _fileLock.WaitAsync();
        try
        {
            var allJobs = await ReadJobsFromFileAsync();
            
            if (allJobs.TryGetValue(jobId, out var job))
            {
                job.ProcessedStations++;
                job.Images ??= new List<WeatherImageInfo>();
                // Store just the URL for now
                
                if (job.ProcessedStations >= job.TotalStations)
                {
                    job.Status = "completed";
                    job.CompletedTime = DateTime.UtcNow;
                }

                allJobs[jobId] = job;
                await WriteJobsToFileAsync(allJobs);
                
                _logger.LogInformation(
                    $"[JobId {jobId}] Station {job.ProcessedStations}/{job.TotalStations} processed → {imageUrl}");
            }
            else
            {
                _logger.LogWarning($"Job {jobId} not found for status update");
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<List<JobStatus>> GetAllJobsAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var allJobs = await ReadJobsFromFileAsync();
            return allJobs.Values.OrderByDescending(j => j.StartTime).ToList();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<Dictionary<string, JobStatus>> ReadJobsFromFileAsync()
    {
        try
        {
            var json = await File.ReadAllTextAsync(_statusFilePath);
            var jobs = JsonSerializer.Deserialize<Dictionary<string, JobStatus>>(json);
            return jobs ?? new Dictionary<string, JobStatus>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading job status file");
            return new Dictionary<string, JobStatus>();
        }
    }

    private async Task WriteJobsToFileAsync(Dictionary<string, JobStatus> jobs)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(jobs, options);
            await File.WriteAllTextAsync(_statusFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing job status file");
            throw;
        }
    }
}
