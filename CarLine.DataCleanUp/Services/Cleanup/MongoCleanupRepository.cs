using MongoDB.Bson;
using MongoDB.Driver;

namespace CarLine.DataCleanUp.Services.Cleanup;

internal sealed class MongoCleanupRepository
{
    public MongoCleanupRepository(IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase("carsnosql");
        Source = database.GetCollection<BsonDocument>("crawled_cars");
        Cleaned = database.GetCollection<BsonDocument>("cleaned_cars");
    }

    public IMongoCollection<BsonDocument> Source { get; }

    public IMongoCollection<BsonDocument> Cleaned { get; }

    public async Task<BsonDocument?> FindFirstSourceAsync(CancellationToken cancellationToken)
    {
        return await Source.Find(FilterDefinition<BsonDocument>.Empty)
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task EnsureUniqueUrlIndexAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending("url");
            var indexOptions = new CreateIndexOptions { Unique = true, Sparse = true, Name = "idx_url_unique" };

            await Cleaned.Indexes.CreateOneAsync(
                new CreateIndexModel<BsonDocument>(indexKeys, indexOptions),
                cancellationToken: cancellationToken
            );

            logger.LogInformation("Ensured unique index on 'url' field");
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" ||
                                               ex.CodeName == "IndexKeySpecsConflict")
        {
            logger.LogInformation("Index on 'url' already exists");
        }
    }

    public async Task<BulkWriteResult<BsonDocument>?> BulkInsertIgnoreDuplicatesAsync(
        IReadOnlyCollection<WriteModel<BsonDocument>> batch,
        CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
            return null;

        try
        {
            // With a unique index on url, duplicates will error with E11000 but inserts continue because IsOrdered=false.
            return await Cleaned.BulkWriteAsync(
                batch,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken
            );
        }
        catch (MongoBulkWriteException<BsonDocument> ex)
        {
            // Duplicate key errors are expected when running insert-only behavior.
            var duplicateErrors = ex.WriteErrors.Count(e => e.Category == ServerErrorCategory.DuplicateKey);
            if (duplicateErrors == ex.WriteErrors.Count)
                return ex.Result;

            throw;
        }
    }
}