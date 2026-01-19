namespace CarLine.API.Models;

public class CarPriceEstimateRequest
{
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Odometer { get; set; }
    public string Transmission { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Fuel { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Region { get; set; }
}