using MongoDB.Bson;
using MongoDB.Driver;

namespace CarLine.Crawler.Services;

public sealed class MongoCrawledCarsRepository(ILogger<MongoCrawledCarsRepository> logger, IMongoDatabase database)
    : ICrawledCarsRepository
{
    public async Task UpsertManyAsync(IEnumerable<ExternalCarListing> listings, string source, CancellationToken cancellationToken)
    {
        var collection = database.GetCollection<BsonDocument>("crawled_cars");
        var now = DateTime.UtcNow;

        var bulkOps = new List<WriteModel<BsonDocument>>();

        foreach (var listing in listings)
        {
            var doc = new BsonDocument
            {
                ["url"] = listing.Url,
                ["manufacturer"] = listing.Manufacturer,
                ["model"] = listing.Model,
                ["year"] = listing.Year,
                ["price"] = listing.Price,
                ["odometer"] = listing.Odometer,
                ["transmission"] = listing.Transmission,
                ["condition"] = listing.Condition,
                ["fuel"] = listing.Fuel,
                ["type"] = listing.Type,
                ["region"] = string.IsNullOrEmpty(listing.Region) ? BsonNull.Value : listing.Region,
                ["image_url"] = string.IsNullOrEmpty(listing.ImageUrl) ? BsonNull.Value : listing.ImageUrl,
                ["vin"] = string.IsNullOrEmpty(listing.Vin) ? BsonNull.Value : listing.Vin,
                ["paint_color"] = string.IsNullOrEmpty(listing.PaintColor) ? BsonNull.Value : listing.PaintColor,
                ["posting_date"] = listing.PostingDate,
                ["source"] = source,
                ["crawled_at"] = now
            };

            var filter = Builders<BsonDocument>.Filter.Eq("url", listing.Url);
            bulkOps.Add(new ReplaceOneModel<BsonDocument>(filter, doc) { IsUpsert = true });
        }

        if (bulkOps.Count == 0)
            return;

        try
        {
            await collection.BulkWriteAsync(
                bulkOps,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error storing car listings in MongoDB");
            throw;
        }
    }
}
