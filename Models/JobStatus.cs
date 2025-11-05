namespace WeatherImageApp.Models;

public class JobStatus
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedTime { get; set; }
    public int TotalStations { get; set; }
    public int ProcessedStations { get; set; }
    public List<WeatherImageInfo>? Images { get; set; }
    public string? ErrorMessage { get; set; }
}

public class WeatherImageInfo
{
    public string StationName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public string WeatherDescription { get; set; } = string.Empty;
}
