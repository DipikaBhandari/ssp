namespace WeatherImageApp.Models;

public class JobRequest
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
