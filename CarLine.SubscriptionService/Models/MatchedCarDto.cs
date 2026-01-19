namespace CarLine.SubscriptionService.Models;

public sealed class MatchedCarDto
{
    public string CarId { get; init; } = string.Empty;
    public string? Url { get; init; }
    public string Manufacturer { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public decimal? Price { get; init; }
    public string? Region { get; init; }
}