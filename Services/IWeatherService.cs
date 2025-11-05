using WeatherImageApp.Models;

namespace WeatherImageApp.Services;

public interface IWeatherService
{
    Task<List<WeatherStation>> GetWeatherStationsAsync(int count = 50);
}
