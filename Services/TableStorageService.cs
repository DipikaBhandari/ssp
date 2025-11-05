using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeatherImageApp.Models;

namespace WeatherImageApp.Services;

public class TableStorageService : ITableStorageService
{
    private readonly TableClient _tableClient;
    private readonly ILogger<TableStorageService> _logger;
    private const string TableName = "jobstatus";
    private const string PartitionKey = "WeatherJob";

    public TableStorageService(IConfiguration configuration, ILogger<TableStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration["AzureWebJobsStorage"] 
            ?? configuration["StorageConnectionString"] 
            ?? throw new InvalidOperationException("Storage connection string not configured");

        var serviceClient = new TableServiceClient(connectionString);
        _tableClient = serviceClient.GetTableClient(TableName);
        
        // Create table if it doesn't exist
        try
        {
            _tableClient.CreateIfNotExists();
            _logger.LogInformation($"Table Storage initialized: {TableName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Table Storage");
            throw;
        }
    }

    public async Task SaveJobStatusAsync(JobStatus jobStatus)
    {
        try
        {
            var entity = new JobStatusEntity
            {
                PartitionKey = PartitionKey,
                RowKey = jobStatus.JobId,
                Status = jobStatus.Status,
                ProcessedStations = jobStatus.ProcessedStations,
                TotalStations = jobStatus.TotalStations,
                StartTime = jobStatus.StartTime,
                CompletedTime = jobStatus.CompletedTime,
                ImagesJson = jobStatus.Images != null ? System.Text.Json.JsonSerializer.Serialize(jobStatus.Images) : null
            };

            await _tableClient.UpsertEntityAsync(entity);
            _logger.LogInformation($"Saved job status to Table Storage: {jobStatus.JobId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving job status to Table Storage: {jobStatus.JobId}");
            throw;
        }
    }

    public async Task<JobStatus?> GetJobStatusAsync(string jobId)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<JobStatusEntity>(PartitionKey, jobId);
            var entity = response.Value;

            var jobStatus = new JobStatus
            {
                JobId = entity.RowKey,
                Status = entity.Status,
                ProcessedStations = entity.ProcessedStations,
                TotalStations = entity.TotalStations,
                StartTime = entity.StartTime,
                CompletedTime = entity.CompletedTime
            };

            if (!string.IsNullOrEmpty(entity.ImagesJson))
            {
                jobStatus.Images = System.Text.Json.JsonSerializer.Deserialize<List<WeatherImageInfo>>(entity.ImagesJson);
            }

            return jobStatus;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning($"Job status not found in Table Storage: {jobId}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving job status from Table Storage: {jobId}");
            throw;
        }
    }

    public async Task UpdateJobProgressAsync(string jobId, int processedStations, int totalStations)
    {
        try
        {
            var entity = await _tableClient.GetEntityAsync<JobStatusEntity>(PartitionKey, jobId);
            var jobEntity = entity.Value;

            jobEntity.ProcessedStations = processedStations;
            jobEntity.TotalStations = totalStations;
            jobEntity.Status = "processing";

            await _tableClient.UpdateEntityAsync(jobEntity, ETag.All);
            _logger.LogInformation($"Updated job progress in Table Storage: {jobId} - {processedStations}/{totalStations}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating job progress in Table Storage: {jobId}");
            // Don't throw - this is not critical
        }
    }

    public async Task CompleteJobAsync(string jobId)
    {
        try
        {
            var entity = await _tableClient.GetEntityAsync<JobStatusEntity>(PartitionKey, jobId);
            var jobEntity = entity.Value;

            jobEntity.Status = "completed";
            jobEntity.CompletedTime = DateTime.UtcNow;

            await _tableClient.UpdateEntityAsync(jobEntity, ETag.All);
            _logger.LogInformation($"Marked job as completed in Table Storage: {jobId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error completing job in Table Storage: {jobId}");
            // Don't throw - this is not critical
        }
    }
}

// Table entity class for Azure Table Storage
public class JobStatusEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Status { get; set; } = string.Empty;
    public int ProcessedStations { get; set; }
    public int TotalStations { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public string? ImagesJson { get; set; }
}
