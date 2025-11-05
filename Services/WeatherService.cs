using System.Text.Json;
using Microsoft.Extensions.Logging;
using WeatherImageApp.Models;

namespace WeatherImageApp.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private const string BuienradarApiUrl = "https://data.buienradar.nl/2.0/feed/json";

    public WeatherService(IHttpClientFactory httpClientFactory, ILogger<WeatherService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    public async Task<List<WeatherStation>> GetWeatherStationsAsync(int count = 50)
    {
        try
        {
            _logger.LogInformation("Fetching weather data from Buienradar API");
            
            var response = await _httpClient.GetAsync(BuienradarApiUrl);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var buienradarData = JsonSerializer.Deserialize<BuienradarResponse>(content, options);
            
            if (buienradarData?.Actual?.Stationmeasurements == null)
            {
                _logger.LogWarning("No station measurements found in Buienradar response");
                return new List<WeatherStation>();
            }

            var stations = buienradarData.Actual.Stationmeasurements
                .Where(s => s.Temperature.HasValue) // Only include stations with temperature data
                .Take(count)
                .Select(s => new WeatherStation
                {
                    StationId = s.StationId,
                    StationName = s.Stationname,
                    Lat = s.Lat,
                    Lon = s.Lon,
                    RegionName = s.Regio,
                    Temperature = s.Temperature,
                    WeatherDescription = s.Weatherdescription,
                    IconUrl = s.Iconurl,
                    WindSpeed = s.Windspeed,
                    WindDirection = s.Winddirection,
                    Humidity = s.Humidity
                })
                .ToList();

            _logger.LogInformation($"Successfully fetched {stations.Count} weather stations");
            return stations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather data from Buienradar");
            throw;
        }
    }
}
