using WeatherImageApp.Models;

namespace WeatherImageApp.Services;

public interface IImageService
{
    Task<byte[]> GetRandomImageAsync();
    Task<byte[]> AddWeatherDataToImageAsync(byte[] imageData, WeatherStation station);
}
