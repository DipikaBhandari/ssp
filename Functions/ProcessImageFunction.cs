using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WeatherImageApp.Models;
using WeatherImageApp.Services;

namespace WeatherImageApp.Functions;

public class ProcessImageFunction
{
    private readonly ILogger<ProcessImageFunction> _logger;
    private readonly IImageService _imageService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ITableStorageService _tableStorageService;

    public ProcessImageFunction(
        ILogger<ProcessImageFunction> logger,
        IImageService imageService,
        IBlobStorageService blobStorageService,
        ITableStorageService tableStorageService)
    {
        _logger = logger;
        _imageService = imageService;
        _blobStorageService = blobStorageService;
        _tableStorageService = tableStorageService;
    }

    [Function("ProcessImage")]
    public async Task Run(
        [QueueTrigger("image-processing-queue", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        _logger.LogInformation($"Processing image from queue: {queueMessage.Substring(0, Math.Min(100, queueMessage.Length))}...");

        try
        {
            var message = JsonSerializer.Deserialize<ImageProcessingMessage>(queueMessage);
            if (message?.Station == null)
            {
                _logger.LogWarning("Invalid message format");
                return;
            }

            _logger.LogInformation(
                $"[JobId {message.JobId}] Processing station {message.StationIndex + 1}/{message.TotalStations}: {message.Station.StationName}");

            // Fetch a random image
            var imageData = await _imageService.GetRandomImageAsync();
            
            // Add weather data to the image
            var processedImage = await _imageService.AddWeatherDataToImageAsync(imageData, message.Station);
            
            // Upload to blob storage
            var imageUrl = await _blobStorageService.UploadImageAsync(
                message.JobId, 
                message.StationIndex, 
                processedImage);
            
            // Update progress in Table Storage
            var currentStatus = await _tableStorageService.GetJobStatusAsync(message.JobId);
            if (currentStatus != null)
            {
                var newProcessedCount = currentStatus.ProcessedStations + 1;
                
                // Add image info to the list
                currentStatus.Images ??= new List<WeatherImageInfo>();
                currentStatus.Images.Add(new WeatherImageInfo
                {
                    StationName = message.Station.StationName ?? "Unknown",
                    ImageUrl = imageUrl,
                    Temperature = message.Station.Temperature ?? 0.0,
                    WeatherDescription = message.Station.WeatherDescription ?? "Unknown"
                });
                
                currentStatus.ProcessedStations = newProcessedCount;
                
                // If all stations processed, mark as complete
                if (newProcessedCount >= message.TotalStations)
                {
                    currentStatus.Status = "completed";
                    currentStatus.CompletedTime = DateTime.UtcNow;
                }
                
                // Save updated status back to Table Storage
                await _tableStorageService.SaveJobStatusAsync(currentStatus);
            }
            
            _logger.LogInformation(
                $"[JobId {message.JobId}] Station {message.StationIndex + 1}/{message.TotalStations} processed → {imageUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image");
            throw; // Will trigger retry mechanism
        }
    }
}
