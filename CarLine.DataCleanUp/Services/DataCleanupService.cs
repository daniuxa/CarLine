using Azure.Storage.Blobs;
using CarLine.Common.Models;
using CarLine.DataCleanUp.Models;
using CarLine.DataCleanUp.Services.Cleanup;
using Elastic.Clients.Elasticsearch;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CarLine.DataCleanUp.Services;

public class DataCleanupService(
    ILogger<DataCleanupService> logger,
    BlobContainerClient container,
    IMongoClient mongoClient,
    ElasticsearchClient elasticsearchClient)
{
    private const int BatchSize = 5000;

    public async Task<CleanupResult> RunCleanupAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting data cleanup run...");
        var startTime = DateTime.UtcNow;

        var mongo = new MongoCleanupRepository(mongoClient);
        var elasticSearch = new ElasticsearchBatchIndexer(logger, elasticsearchClient);

        // Ensure Elasticsearch index exists (best-effort)
        try
        {
            await elasticSearch.EnsureIndexExistsAsync(cancellationToken);
            logger.LogInformation("Elasticsearch index ensured");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure Elasticsearch index. Continuing without ES indexing.");
        }

        await mongo.EnsureUniqueUrlIndexAsync(logger, cancellationToken);

        var firstDoc = await mongo.FindFirstSourceAsync(cancellationToken);
        if (firstDoc == null)
        {
            logger.LogInformation("No documents found in collection");
            return new CleanupResult
            {
                Success = false,
                Message = "No documents found",
                Duration = DateTime.UtcNow - startTime
            };
        }

        // Build CSV header (excluding web-only fields)
        var csvHeader = firstDoc.Names
            .Where(n => !DataCleanupConstants.ColumnsToRemoveFromTraining.Contains(n))
            .OrderBy(n => n)
            .ToList();

        logger.LogInformation("Training CSV Header: {header}", string.Join(", ", csvHeader));

        var tempCsvPath = Path.Combine(Path.GetTempPath(), $"cleaned_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");

        long rowsRead = 0;
        int rowsWritten = 0, rowsDropped = 0, errors = 0, updated = 0, inserted = 0;

        var distinctValues = new DistinctValueTracker(csvHeader);
        var mongoBatch = new MongoUpsertBatch();
        var esBatch = new List<CarDocument>();

        // Write CSV inside a scope so the underlying file handle is guaranteed to be closed
        // before we reopen the file for blob upload (important on Windows).
        await using (var csvWriter = new TrainingCsvWriter(tempCsvPath))
        {
            await csvWriter.WriteHeaderAsync(csvHeader);

            using var cursor =
                await mongo.Source.FindAsync(FilterDefinition<BsonDocument>.Empty,
                    cancellationToken: cancellationToken);
            while (await cursor.MoveNextAsync(cancellationToken))
                foreach (var doc in cursor.Current)
                {
                    rowsRead++;

                    try
                    {
                        var fullRecord = RecordExtractor.ExtractAllFields(doc);
                        var csvRecord = RecordExtractor.ExtractCsvFields(doc, csvHeader);

                        if (!RecordCleaner.CleanAndValidate(csvRecord, fullRecord))
                        {
                            rowsDropped++;
                            continue;
                        }

                        distinctValues.TrackRow(csvRecord);

                        csvWriter.WriteRow(csvHeader, csvRecord);

                        // Build cleaned BSON doc for ES from the full record (same selection as before)
                        var cleanedDoc = new BsonDocument();
                        foreach (var key in DataCleanupConstants.WebDisplayFields)
                            if (fullRecord.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                                cleanedDoc[key] = val;

                        if (mongoBatch.TryAddFromFullRecord(fullRecord, out var url) && url != null)
                        {
                            // Best-effort: add ES doc
                            try
                            {
                                esBatch.Add(FromCleanedBson(cleanedDoc, url));
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "Failed to build Elasticsearch document for url={url}", url);
                            }

                            if (mongoBatch.Count >= BatchSize)
                            {
                                var batchInserted = await ProcessBatchAsync(mongo, mongoBatch, esBatch, elasticSearch,
                                    cancellationToken);
                                inserted += batchInserted;
                            }
                        }

                        rowsWritten++;
                        if (rowsWritten % BatchSize == 0)
                            logger.LogInformation("Processed {count} rows...", rowsWritten);
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        logger.LogError(ex, "Error processing row {row}", rowsRead);
                    }
                }

            if (mongoBatch.Count > 0)
            {
                var finalInserted =
                    await ProcessBatchAsync(mongo, mongoBatch, esBatch, elasticSearch, cancellationToken);
                if (finalInserted > 0) inserted += finalInserted;
            }

            await csvWriter.FlushAsync();
        }

        logger.LogInformation(
            "Cleanup complete - Rows: {rows}, Written: {written}, Dropped: {dropped}, Errors: {errors}",
            rowsRead, rowsWritten, rowsDropped, errors);

        logger.LogInformation("MongoDB summary - New inserted: {new}, Updated: {updated}",
            inserted, updated);

        logger.LogInformation("Stored {count} cleaned documents in MongoDB collection 'cleaned_cars'", rowsWritten);

        logger.LogInformation("Distinct values per column:");
        foreach (var (column, count) in distinctValues.GetCountsOrderedByColumn())
            logger.LogInformation("  {column}: {count} distinct values", column, count);

        // Upload to blob (CSV writer is disposed at this point)
        var blobName = $"cleaned/{Path.GetFileName(tempCsvPath)}";
        var blobClient = container.GetBlobClient(blobName);

        await using (var fileStream = await OpenReadWithRetryAsync(tempCsvPath, logger, cancellationToken))
        {
            await blobClient.UploadAsync(fileStream, true, cancellationToken);
        }

        logger.LogInformation("Uploaded to blob: {blob}", blobName);

        try
        {
            return new CleanupResult
            {
                Success = true,
                Message = "Cleanup completed successfully",
                RowsRead = rowsRead,
                RowsWritten = rowsWritten,
                RowsDropped = rowsDropped,
                Errors = errors,
                BlobName = blobName,
                Duration = DateTime.UtcNow - startTime
            };
        }
        finally
        {
            if (File.Exists(tempCsvPath))
                File.Delete(tempCsvPath);
        }
    }

    private async Task<int> ProcessBatchAsync(
        MongoCleanupRepository mongo,
        MongoUpsertBatch mongoBatch,
        List<CarDocument> esBatch,
        ElasticsearchBatchIndexer elasticSearch,
        CancellationToken cancellationToken)
    {
        if (mongoBatch.Count == 0)
            return 0;

        var result = await mongo.BulkInsertIgnoreDuplicatesAsync(mongoBatch.Items, cancellationToken);

        var insertedCount = 0;
        if (result != null) insertedCount = (int)result.InsertedCount;

        mongoBatch.Clear();

        logger.LogInformation("MongoDB batch insert-only: {new} inserted (duplicates ignored)",
            result?.InsertedCount ?? 0);

        if (esBatch.Count > 0)
        {
            try
            {
                await elasticSearch.CreateOnlyAsync(esBatch, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create batch in Elasticsearch");
            }

            esBatch.Clear();
        }

        return insertedCount;
    }

    private static CarDocument FromCleanedBson(BsonDocument cleanedDoc, string url)
    {
        return new CarDocument
        {
            Id = url,
            Manufacturer = cleanedDoc.Contains("manufacturer")
                ? BsonValueConverters.ToStringValue(cleanedDoc["manufacturer"])
                : string.Empty,
            Model =
                cleanedDoc.Contains("model") ? BsonValueConverters.ToStringValue(cleanedDoc["model"]) : string.Empty,
            Year = cleanedDoc.Contains("year") &&
                   int.TryParse(BsonValueConverters.ToStringValue(cleanedDoc["year"]), out var y)
                ? y
                : 0,
            Status = "ACTIVE",
            Price = cleanedDoc.Contains("price") &&
                    decimal.TryParse(BsonValueConverters.ToStringValue(cleanedDoc["price"]), out var p)
                ? p
                : 0m,
            Odometer = cleanedDoc.Contains("odometer") &&
                       int.TryParse(BsonValueConverters.ToStringValue(cleanedDoc["odometer"]), out var o)
                ? o
                : 0,
            Transmission = cleanedDoc.Contains("transmission")
                ? BsonValueConverters.ToStringValue(cleanedDoc["transmission"])
                : string.Empty,
            Condition = cleanedDoc.Contains("condition")
                ? BsonValueConverters.ToStringValue(cleanedDoc["condition"])
                : string.Empty,
            Fuel = cleanedDoc.Contains("fuel") ? BsonValueConverters.ToStringValue(cleanedDoc["fuel"]) : string.Empty,
            Type = cleanedDoc.Contains("type") ? BsonValueConverters.ToStringValue(cleanedDoc["type"]) : string.Empty,
            Region = cleanedDoc.Contains("region") ? BsonValueConverters.ToStringValue(cleanedDoc["region"]) : null,
            Url = url,
            ImageUrl = cleanedDoc.Contains("image_url")
                ? BsonValueConverters.ToStringValue(cleanedDoc["image_url"])
                : null,
            Vin = cleanedDoc.Contains("vin") ? BsonValueConverters.ToStringValue(cleanedDoc["vin"]) : null,
            PaintColor = cleanedDoc.Contains("paint_color")
                ? BsonValueConverters.ToStringValue(cleanedDoc["paint_color"])
                : null,
            PostingDate =
                cleanedDoc.Contains("posting_date") &&
                DateTime.TryParse(BsonValueConverters.ToStringValue(cleanedDoc["posting_date"]), out var pd)
                    ? pd
                    : null,
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        };
    }

    private static async Task<FileStream> OpenReadWithRetryAsync(
        string path,
        ILogger logger,
        CancellationToken cancellationToken,
        int maxAttempts = 6,
        int initialDelayMs = 100)
    {
        // We explicitly allow other handles to keep the file open (ReadWrite/Delete) to reduce
        // issues with AV scanners or delayed handle release on Windows.
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
            }
            catch (IOException ex) when (attempt < maxAttempts)
            {
                var delay = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                logger.LogWarning(ex,
                    "Failed to open '{path}' for upload (attempt {attempt}/{maxAttempts}). Retrying in {delay}ms...",
                    path, attempt, maxAttempts, delay);
                await Task.Delay(delay, cancellationToken);
            }
        }

        // final attempt throws
        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
    }
}