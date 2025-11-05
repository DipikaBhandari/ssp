namespace WeatherImageApp.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(string jobId, int stationIndex, byte[] imageData);
    Task<List<string>> GetJobImagesAsync(string jobId);
}
