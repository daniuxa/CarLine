using CarLine.SubscriptionService.Data.Entities;
using CarLine.SubscriptionService.Models;

namespace CarLine.SubscriptionService.Email;

public sealed class EmailDigestTemplateBuilder : ISubscriptionDigestTemplateBuilder
{
    public IReadOnlyList<string> BuildBodyLines(
        SubscriptionEntity subscription,
        IReadOnlyList<MatchedCarDto> cars)
    {
        var lines = new List<string>
        {
            "Hello,",
            "",
            $"We found {cars.Count} new car(s) that match your subscription.",
            "",
            "Filters:",
            $"- Manufacturer: {subscription.Manufacturer ?? "(any)"}",
            $"- Model: {subscription.Model ?? "(any)"}",
            $"- Year: {subscription.YearFrom?.ToString() ?? "(any)"} .. {subscription.YearTo?.ToString() ?? "(any)"}",
            $"- Odometer: {subscription.OdometerFrom?.ToString() ?? "(any)"} .. {subscription.OdometerTo?.ToString() ?? "(any)"}",
            $"- Fuel: {subscription.Fuel ?? "(any)"}",
            $"- Transmission: {subscription.Transmission ?? "(any)"}",
            $"- Condition: {subscription.Condition ?? "(any)"}",
            $"- Type: {subscription.Type ?? "(any)"}",
            $"- Region: {subscription.Region ?? "(any)"}",
            "",
            "New cars:"
        };

        foreach (var car in cars.Take(50))
        {
            var price = car.Price.HasValue ? $"{car.Price.Value:C}" : "(price n/a)";
            lines.Add($"- {car.Manufacturer} {car.Model} {car.Year} | {price} | {car.Region ?? "(region n/a)"}");
            if (!string.IsNullOrWhiteSpace(car.Url)) lines.Add($"  {car.Url}");
        }

        if (cars.Count > 50) lines.Add($"...and {cars.Count - 50} more");

        lines.Add(string.Empty);
        lines.Add("Thanks,");
        lines.Add("CarLine");

        return lines;
    }
}