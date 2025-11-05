namespace WeatherImageApp.Models;

public class BuienradarResponse
{
    public Actual? Actual { get; set; }
}

public class Actual
{
    public List<StationMeasurement>? Stationmeasurements { get; set; }
}

public class StationMeasurement
{
    public int StationId { get; set; }
    public string? Stationname { get; set; }
    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public string? Regio { get; set; }
    public double? Temperature { get; set; }
    public string? Weatherdescription { get; set; }
    public string? Iconurl { get; set; }
    public double? Windspeed { get; set; }
    public string? Winddirection { get; set; }
    public double? Humidity { get; set; }
}
