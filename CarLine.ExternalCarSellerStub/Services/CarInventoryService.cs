using CarLine.ExternalCarSellerStub.Models;

namespace CarLine.ExternalCarSellerStub.Services;

// In-memory car inventory service
public class CarInventoryService
{
    private readonly List<ExternalCarListing> _cars = new();
    private int _idCounter = 1;

    public int TotalCount => _cars.Count;

    public CarInventoryService()
    {
        // Seed with sample data
        SeedData();
    }

    private void SeedData()
    {
        var random = new Random(42); // Fixed seed for consistency
        var baseDate = DateTime.UtcNow.AddDays(-30);

        // Sample manufacturers and their models
        var vehicleData = new Dictionary<string, (string[] models, string[] types)>
        {
            ["toyota"] = (["camry", "corolla", "rav4", "highlander", "tacoma", "tundra"], ["sedan", "suv", "pickup"]),
            ["ford"] = (["f-150", "mustang", "explorer", "escape", "fusion"], ["truck", "sedan", "suv"]),
            ["chevrolet"] = (["silverado", "equinox", "malibu", "traverse", "camaro"], ["truck", "suv", "sedan"]),
            ["honda"] = (["civic", "accord", "cr-v", "pilot"], ["sedan", "suv"]),
            ["nissan"] = (["altima", "sentra", "rogue", "frontier"], ["sedan", "suv", "pickup"]),
            ["bmw"] = (["3-series", "5-series", "x3", "x5"], ["sedan", "suv"]),
            ["mercedes-benz"] = (["c-class", "e-class", "glc", "gle"], ["sedan", "suv"]),
            ["volkswagen"] = (["jetta", "passat", "tiguan", "atlas"], ["sedan", "suv"]),
            ["jeep"] = (["wrangler", "cherokee", "grand cherokee", "gladiator"], ["suv", "pickup"]),
            ["ram"] = (["1500", "2500", "3500"], ["truck", "pickup"])
        };

        var transmissions = new[] { "automatic", "manual", "other" };
        var conditions = new[] { "excellent", "good", "fair", "like new" };
        var fuels = new[] { "gas", "diesel", "electric", "hybrid" };
        var regions = new[]
            { "auburn", "atlanta", "boston", "chicago", "dallas", "denver", "houston", "miami", "seattle", "portland" };
        var paintColors = new[] { "white", "black", "silver", "gray", "red", "blue", "green", "brown" };

        // Generate 150 sample cars
        foreach (var (manufacturer, (models, types)) in vehicleData)
        {
            for (int i = 0; i < 15; i++)
            {
                var model = models[random.Next(models.Length)];
                var type = types[random.Next(types.Length)];
                var year = random.Next(2010, 2025);
                var price = random.Next(5000, 75000);
                var odometer = random.Next(5000, 150000);
                var postingDate = baseDate.AddDays(random.Next(0, 30));

                var carId = $"{_idCounter:D6}";
                var url = $"https://external-seller.example.com/listing/{manufacturer}-{model}-{year}-{carId}";

                _cars.Add(new ExternalCarListing
                {
                    Id = url,
                    Url = url,
                    Manufacturer = manufacturer,
                    Model = model,
                    Year = year,
                    Price = price,
                    Odometer = odometer,
                    Transmission = transmissions[random.Next(transmissions.Length)],
                    Condition = conditions[random.Next(conditions.Length)],
                    Fuel = fuels[random.Next(fuels.Length)],
                    Type = type,
                    Region = regions[random.Next(regions.Length)],
                    ImageUrl = $"https://images.example.com/car-{carId}.jpg",
                    Vin = GenerateVin(random),
                    PaintColor = paintColors[random.Next(paintColors.Length)],
                    PostingDate = postingDate
                });

                _idCounter++;
            }
        }
    }

    private static string GenerateVin(Random random)
    {
        const string chars = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 17)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }

    public List<ExternalCarListing> GetCars(
        string? manufacturer,
        string? model,
        int? minYear,
        int? maxYear,
        decimal? minPrice,
        decimal? maxPrice,
        int page,
        int pageSize)
    {
        var query = _cars.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(manufacturer))
            query = query.Where(c => c.Manufacturer.Equals(manufacturer, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(model))
            query = query.Where(c => c.Model.Contains(model, StringComparison.OrdinalIgnoreCase));

        if (minYear.HasValue)
            query = query.Where(c => c.Year >= minYear.Value);

        if (maxYear.HasValue)
            query = query.Where(c => c.Year <= maxYear.Value);

        if (minPrice.HasValue)
            query = query.Where(c => c.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(c => c.Price <= maxPrice.Value);

        return query
            .OrderByDescending(c => c.PostingDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public ExternalCarListing? GetCarById(string id)
    {
        return _cars.FirstOrDefault(c => c.Url.Contains(id, StringComparison.OrdinalIgnoreCase));
    }

    public List<ExternalCarListing> GetLatestCars(int count)
    {
        return _cars
            .OrderByDescending(c => c.PostingDate)
            .Take(count)
            .ToList();
    }
}