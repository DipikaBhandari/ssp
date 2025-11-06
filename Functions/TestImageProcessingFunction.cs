using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WeatherImageApp.Services;
using WeatherImageApp.Middleware;
using WeatherImageApp.Models;

namespace WeatherImageApp.Functions;

public class TestImageProcessingFunction
{
    private readonly ILogger<TestImageProcessingFunction> _logger;
    private readonly IWeatherService _weatherService;
    private readonly IImageService _imageService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IConfiguration _configuration;

    public TestImageProcessingFunction(
        ILogger<TestImageProcessingFunction> logger,
        IWeatherService weatherService,
        IImageService imageService,
        IBlobStorageService blobStorageService,
        IConfiguration configuration)
    {
        _logger = logger;
        _weatherService = weatherService;
        _imageService = imageService;
        _blobStorageService = blobStorageService;
        _configuration = configuration;
    }

    [Function("TestImageProcessing")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "test/image")] HttpRequestData req)
    {
        // Validate API key
        var authResponse = await ApiKeyAuthAttribute.ValidateApiKeyAsync(req, _configuration, _logger);
        if (authResponse != null)
        {
            return authResponse;
        }

        _logger.LogInformation("Testing image processing...");

        try
        {
            // Fetch one weather station
            _logger.LogInformation("Fetching weather station data...");
            var stations = await _weatherService.GetWeatherStationsAsync(1);
            
            if (stations.Count == 0)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
                await errorResponse.WriteAsJsonAsync(new { error = "No weather stations available" });
                return errorResponse;
            }

            var station = stations[0];
            _logger.LogInformation($"Processing station: {station.StationName}");

            // Fetch a random image
            _logger.LogInformation("Fetching random image from Unsplash...");
            var imageData = await _imageService.GetRandomImageAsync();
            _logger.LogInformation($"Downloaded image: {imageData.Length} bytes");

            // Add weather data to the image
            _logger.LogInformation("Adding weather overlay to image...");
            var processedImage = await _imageService.AddWeatherDataToImageAsync(imageData, station);
            _logger.LogInformation($"Processed image: {processedImage.Length} bytes");

            // Upload to blob storage with test job ID
            var testJobId = "test-" + Guid.NewGuid().ToString();
            _logger.LogInformation("Uploading to blob storage...");
            var imageUrl = await _blobStorageService.UploadImageAsync(testJobId, 0, processedImage);
            
            _logger.LogInformation($"Successfully created test image: {imageUrl}");

            // Return success with details
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = "Image processing test successful!",
                station = new
                {
                    name = station.StationName,
                    region = station.RegionName,
                    temperature = station.Temperature,
                    weatherDescription = station.WeatherDescription,
                    windSpeed = station.WindSpeed,
                    humidity = station.Humidity
                },
                imageUrl = imageUrl,
                imageSize = processedImage.Length
            });
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing image processing");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new 
            { 
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
            return errorResponse;
        }
    }
}
