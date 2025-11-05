using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using WeatherImageApp.Models;

namespace WeatherImageApp.Services;

public class ImageService : IImageService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageService> _logger;
    private readonly string? _unsplashAccessKey;

    public ImageService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ImageService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _unsplashAccessKey = configuration["UnsplashAccessKey"];
    }

    public async Task<byte[]> GetRandomImageAsync()
    {
        try
        {
            // Using Unsplash API for random nature/landscape images
            var url = $"https://source.unsplash.com/1600x900/?nature,landscape";
            
            _logger.LogInformation("Fetching random image from Unsplash");
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching random image");
            // Return a simple colored image as fallback
            return CreateFallbackImage();
        }
    }

    public async Task<byte[]> AddWeatherDataToImageAsync(byte[] imageData, WeatherStation station)
    {
        try
        {
            using var image = Image.Load<Rgba32>(imageData);
            
            // Create a semi-transparent overlay at the bottom
            var overlayHeight = 150;
            var overlayColor = Color.ParseHex("#000000CC"); // Black with transparency
            
            image.Mutate(ctx =>
            {
                // Draw overlay rectangle
                ctx.Fill(overlayColor, new Rectangle(0, image.Height - overlayHeight, image.Width, overlayHeight));
                
                // Load system font
                var fontFamily = SystemFonts.Collection.Families.First();
                var titleFont = fontFamily.CreateFont(32, FontStyle.Bold);
                var dataFont = fontFamily.CreateFont(24, FontStyle.Regular);
                
                var textColor = Color.White;
                var textOptions = new RichTextOptions(titleFont)
                {
                    Origin = new PointF(20, image.Height - overlayHeight + 10),
                    WrappingLength = image.Width - 40
                };
                
                // Draw station name
                ctx.DrawText(textOptions, station.StationName ?? "Unknown Station", textColor);
                
                // Draw weather data
                var weatherInfo = $"Temperature: {station.Temperature:F1}°C";
                if (station.WeatherDescription != null)
                    weatherInfo += $" | {station.WeatherDescription}";
                if (station.WindSpeed.HasValue)
                    weatherInfo += $" | Wind: {station.WindSpeed:F1} m/s {station.WindDirection}";
                if (station.Humidity.HasValue)
                    weatherInfo += $" | Humidity: {station.Humidity:F0}%";
                
                var dataTextOptions = new RichTextOptions(dataFont)
                {
                    Origin = new PointF(20, image.Height - overlayHeight + 60),
                    WrappingLength = image.Width - 40
                };
                ctx.DrawText(dataTextOptions, weatherInfo, textColor);
                
                // Draw region/location
                if (station.RegionName != null)
                {
                    var locationOptions = new RichTextOptions(dataFont)
                    {
                        Origin = new PointF(20, image.Height - overlayHeight + 100),
                        WrappingLength = image.Width - 40
                    };
                    ctx.DrawText(locationOptions, $"Region: {station.RegionName}", textColor);
                }
            });
            
            using var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding weather data to image");
            throw;
        }
    }

    private byte[] CreateFallbackImage()
    {
        using var image = new Image<Rgba32>(1600, 900);
        image.Mutate(ctx => ctx.Fill(Color.CornflowerBlue));
        
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms);
        return ms.ToArray();
    }
}
