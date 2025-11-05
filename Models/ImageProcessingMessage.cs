namespace WeatherImageApp.Models;

public class ImageProcessingMessage
{
    public string JobId { get; set; } = string.Empty;
    public WeatherStation? Station { get; set; }
    public int StationIndex { get; set; }
    public int TotalStations { get; set; }
}
