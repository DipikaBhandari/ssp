using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WeatherImageApp.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly string _containerName;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration["StorageConnectionString"] 
                              ?? configuration["AzureWebJobsStorage"]
                              ?? throw new InvalidOperationException("Storage connection string not found");
        
        _containerName = configuration["BlobContainerName"] ?? "weather-images";
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadImageAsync(string jobId, int stationIndex, byte[] imageData)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            // Create container without public access
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobName = $"{jobId}/station-{stationIndex:D3}.jpg";
            var blobClient = containerClient.GetBlobClient(blobName);

            using var ms = new MemoryStream(imageData);
            await blobClient.UploadAsync(ms, overwrite: true);

            // Generate SAS token URL with 1 hour expiry
            var sasUrl = GenerateSasUrl(blobClient);
            _logger.LogInformation($"Uploaded image to blob storage: {blobName}");
            
            return sasUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading image for job {jobId}, station {stationIndex}");
            throw;
        }
    }

    public async Task<List<string>> GetJobImagesAsync(string jobId)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            
            // Check if container exists first
            var exists = await containerClient.ExistsAsync();
            if (!exists)
            {
                _logger.LogInformation($"Container {_containerName} does not exist yet");
                return new List<string>();
            }

            var imageUrls = new List<string>();

            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: $"{jobId}/"))
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                // Generate SAS URL for each blob
                var sasUrl = GenerateSasUrl(blobClient);
                imageUrls.Add(sasUrl);
            }

            return imageUrls.OrderBy(url => url).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving images for job {jobId}");
            return new List<string>(); // Return empty list instead of throwing
        }
    }

    private string GenerateSasUrl(BlobClient blobClient)
    {
        // Check if we can generate SAS token (requires account key)
        if (!blobClient.CanGenerateSasUri)
        {
            _logger.LogWarning("Cannot generate SAS token. Returning blob URI without SAS.");
            return blobClient.Uri.ToString();
        }

        // Create SAS token with read permissions valid for 1 hour
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b", // b = blob
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Start 5 minutes ago to account for clock skew
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };

        // Grant read permissions
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        // Generate the SAS URI
        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        return sasUri.ToString();
    }
}
