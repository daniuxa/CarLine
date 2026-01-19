using CarLine.Common.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace CarLine.SubscriptionService.Services;

public sealed class MongoCarsRepository(IMongoClient mongoClient, IConfiguration configuration)
{
    private static readonly string[] AllowedPriceClassifications = new[]
    {
        PriceClassification.Low.ToStorageString(),
        PriceClassification.Normal.ToStorageString()
    };

    private readonly IMongoDatabase _db = mongoClient.GetDatabase(configuration["MongoDB:Database"]
                                                                  ?? configuration["CarsMongo:Database"]
                                                                  ?? "carsnosql");
    private readonly string _collectionName = configuration["MongoDB:CarsCollection"]
                                              ?? configuration["CarsMongo:CarsCollection"]
                                              ?? "cleaned_cars";

    public async Task<List<BsonDocument>> FindNewCarsForSubscriptionAsync(
        DateTime sinceUtc,
        string? manufacturer,
        string? model,
        int? yearFrom,
        int? yearTo,
        int? odometerFrom,
        int? odometerTo,
        string? condition,
        string? fuel,
        string? transmission,
        string? type,
        string? region,
        CancellationToken cancellationToken)
    {
        var collection = _db.GetCollection<BsonDocument>(_collectionName);

        var filter = Builders<BsonDocument>.Filter.Empty;

        // Newer than subscription.
        // NOTE: In this codebase, posting_date may be stored either as a BSON DateTime (from crawler)
        // or as an ISO-8601 string (from other pipelines / data sources). Mongo comparisons are type-sensitive,
        // so we query both representations.
        var sinceUtcNormalized = DateTime.SpecifyKind(sinceUtc, DateTimeKind.Utc);
        var sinceIso = sinceUtcNormalized.ToString("O");

        var postingDateFilter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Gt("posting_date", sinceUtcNormalized),
            Builders<BsonDocument>.Filter.Gt("posting_date", sinceIso));

        filter &= postingDateFilter;

        // Cleaned data may store many values as strings. Use case-insensitive exact-match regex for strings.
        if (!string.IsNullOrWhiteSpace(manufacturer))
            filter &= EqIgnoreCase("manufacturer", manufacturer);

        if (!string.IsNullOrWhiteSpace(model))
            filter &= EqIgnoreCase("model", model);

        // Numeric fields may be stored as numbers OR strings. Use $expr + $convert so both work.
        if (yearFrom.HasValue)
            filter &= ExprCompareInt("year", "$gte", yearFrom.Value);

        if (yearTo.HasValue)
            filter &= ExprCompareInt("year", "$lte", yearTo.Value);

        if (odometerFrom.HasValue)
            filter &= ExprCompareInt("odometer", "$gte", odometerFrom.Value);

        if (odometerTo.HasValue)
            filter &= ExprCompareInt("odometer", "$lte", odometerTo.Value);

        if (!string.IsNullOrWhiteSpace(condition))
            filter &= EqIgnoreCase("condition", condition);

        if (!string.IsNullOrWhiteSpace(fuel))
            filter &= EqIgnoreCase("fuel", fuel);

        if (!string.IsNullOrWhiteSpace(transmission))
            filter &= EqIgnoreCase("transmission", transmission);

        if (!string.IsNullOrWhiteSpace(type))
            filter &= EqIgnoreCase("type", type);

        if (!string.IsNullOrWhiteSpace(region))
            filter &= EqIgnoreCase("region", region);

        filter &= Builders<BsonDocument>.Filter.In("price_classification", AllowedPriceClassifications);

        // Safety: cap amount per subscription per run
        var docs = await collection.Find(filter)
            .Sort(Builders<BsonDocument>.Sort.Descending("posting_date"))
            .Limit(200)
            .ToListAsync(cancellationToken);

        return docs;
    }

    private static FilterDefinition<BsonDocument> EqIgnoreCase(string field, string value)
    {
        // Exact match ignoring case.
        // Escape the value so user input can't change the regex meaning.
        var pattern = $"^{Regex.Escape(value.Trim())}$";
        return Builders<BsonDocument>.Filter.Regex(field, new BsonRegularExpression(pattern, "i"));
    }

    private static FilterDefinition<BsonDocument> ExprCompareInt(string field, string op, int value)
    {
        // Builds: { $expr: { op: [ { $convert: { input: "$field", to: "int", onError: null, onNull: null } }, value ] } }
        // This allows comparisons when the field is stored as either number or numeric string.
        var converted = new BsonDocument("$convert", new BsonDocument
        {
            { "input", "$" + field },
            { "to", "int" },
            { "onError", BsonNull.Value },
            { "onNull", BsonNull.Value }
        });

        var expr = new BsonDocument(op, new BsonArray { converted, value });
        return new BsonDocument("$expr", expr);
    }
}
