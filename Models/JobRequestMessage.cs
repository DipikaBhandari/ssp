namespace WeatherImageApp.Models;

/// <summary>
/// Message for job-request-queue to initiate weather image job processing
/// </summary>
public class JobRequestMessage
{
    public string JobId { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public int MaxStations { get; set; } = 36;
}
