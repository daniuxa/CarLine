namespace CarLine.Common.Models;

// Request model matching training features
public record CarPredictionRequest
{
    public string Manufacturer { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public float Year { get; init; }
    public float Odometer { get; init; }
    public string? Condition { get; init; }
    public string? Fuel { get; init; }
    public string? Transmission { get; init; }
    public string? Type { get; init; }
    public string? Region { get; init; }
}