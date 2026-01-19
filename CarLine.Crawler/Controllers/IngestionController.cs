using System.Globalization;
using System.Diagnostics;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CarLine.Crawler.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly ILogger<IngestionController> _logger;
    private readonly IMongoCollection<BsonDocument> _carsCollection;

    public IngestionController(ILogger<IngestionController> logger, IMongoClient mongoClient)
    {
        _logger = logger;
        var db = mongoClient.GetDatabase("carsnosql");
        _carsCollection = db.GetCollection<BsonDocument>("crawled_cars");
    }

    [HttpPost("upload")]
    [RequestFormLimits(MultipartBodyLengthLimit = 314_572_800)] // 300 MB
    [RequestSizeLimit(314_572_800)] // 300 MB limit
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        // If client used a different form field name, try to pick the first file from the parsed form.
        try
        {
            var filesCount = Request?.Form?.Files?.Count ?? 0;
            _logger.LogInformation("Upload request received. Provided IFormFile param null? {IsNull}. Request.Form.Files.Count={Count}", file == null, filesCount);

            if ((file == null || file.Length == 0) && filesCount > 0)
            {
                file = Request.Form.Files[0];
                _logger.LogInformation("Fallback: using first file from Request.Form.Files. FileName={FileName}, Length={Length}", file?.FileName, file?.Length);
            }
        }
        catch (Exception ex) when (ex is InvalidDataException || ex is BadHttpRequestException)
        {
            // Common when multipart body size is exceeded or request is malformed.
            _logger.LogWarning(ex, "Failed to read multipart form (likely size limit or malformed body)");
            return BadRequest(new { message = "Failed to read the request form. The multipart body may be too large or malformed." });
        }

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        _logger.LogInformation("Starting CSV ingestion upload. FileName={FileName}, Length={Length} bytes", file.FileName, file.Length);
        var swTotal = Stopwatch.StartNew();

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            BadDataFound = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim
        };

        int processed = 0, inserted = 0, updated = 0, errors = 0, dropped = 0;
        var toInsert = new List<WriteModel<BsonDocument>>();

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, csvConfig);

        // Read header
        await csv.ReadAsync();
        csv.ReadHeader();
        var header = csv.HeaderRecord ?? Array.Empty<string>();

        _logger.LogInformation("CSV header read. Columns={Count}", header.Length);
        if (header.Length > 0)
            _logger.LogDebug("CSV header sample: {HeaderSample}", string.Join(", ", header.Take(20)));
        else
        {
            _logger.LogWarning("Uploaded CSV contains no header columns.");
        }

        var swBatch = new Stopwatch();
        var lastProgressLog = 0;
        string lastProcessedId = "(none)";

        while (await csv.ReadAsync())
        {
            try
            {
                var doc = new BsonDocument();
                foreach (var h in header)
                {
                    var value = csv.GetField(h);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        // try to parse numeric fields
                        if (long.TryParse(value, out var li))
                            doc[h] = li;
                        else if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                            doc[h] = d;
                        else
                            doc[h] = value;
                    }
                    else
                    {
                        doc[h] = BsonNull.Value;
                    }
                }

                // Use existing id or vin as upsert key
                BsonValue idVal = BsonNull.Value;
                if (doc.Contains("id") && !doc["id"].IsBsonNull)
                    idVal = doc["id"];
                else if (doc.Contains("vin") && !doc["vin"].IsBsonNull)
                    idVal = doc["vin"];

                if (!idVal.IsBsonNull)
                {
                    lastProcessedId = idVal.ToString()!;
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", idVal);
                    var update = new BsonDocument("$set", doc);
                    var result = await _carsCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });

                    if (result.UpsertedId != null)
                    {
                        updated++;
                        _logger.LogDebug("Upserted document with id {UpsertedId}", result.UpsertedId);
                    }
                    else if (result.ModifiedCount > 0)
                    {
                        updated++;
                        _logger.LogDebug("Updated document with id {Id}", idVal);
                    }
                    else
                    {
                        // No change (maybe identical)
                        _logger.LogTrace("No changes for document id {Id}", idVal);
                    }
                }
                else
                {
                    toInsert.Add(new InsertOneModel<BsonDocument>(doc));
                }

                processed++;

                // Periodic progress logging
                if (processed - lastProgressLog >= 1000)
                {
                    lastProgressLog = processed;
                    _logger.LogInformation("Ingestion progress: processed={Processed}, inserted={Inserted}, updated={Updated}, errors={Errors}, lastId={LastId}", processed, inserted, updated, errors, lastProcessedId);
                }

                // Flush batch inserts periodically
                if (toInsert.Count >= 1000)
                {
                    _logger.LogInformation("Flushing batch of {BatchSize} documents to MongoDB...", toInsert.Count);
                    swBatch.Restart();
                    try
                    {
                        await _carsCollection.BulkWriteAsync(toInsert);
                        swBatch.Stop();
                        inserted += toInsert.Count;
                        _logger.LogInformation("Batch flushed: {BatchSize} docs in {ElapsedMs}ms", toInsert.Count, swBatch.ElapsedMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        swBatch.Stop();
                        _logger.LogError(ex, "Batch insert failed for {BatchSize} docs. LastId={LastId}", toInsert.Count, lastProcessedId);
                    }
                    finally
                    {
                        toInsert.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                errors++;
                // Try to capture a small sample of the record for debugging
                string sample = "(no sample)";
                try
                {
                    var rec = csv.Context.Parser.Record;
                    if (rec != null)
                        sample = string.Join(",", rec.Take(10).ToArray());
                }
                catch { /* ignore sample capture failures */ }

                _logger.LogError(ex, "Failed to process CSV row {Row}. Sample={Sample}", processed + 1, sample);
            }
        }

        // Final flush
        if (toInsert.Count > 0)
        {
            _logger.LogInformation("Flushing final batch of {BatchSize} documents to MongoDB...", toInsert.Count);
            swBatch.Restart();
            try
            {
                await _carsCollection.BulkWriteAsync(toInsert);
                swBatch.Stop();
                inserted += toInsert.Count;
                _logger.LogInformation("Final batch flushed: {BatchSize} docs in {ElapsedMs}ms", toInsert.Count, swBatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                swBatch.Stop();
                _logger.LogError(ex, "Final batch insert failed for {BatchSize} docs. LastId={LastId}", toInsert.Count, lastProcessedId);
            }
            finally
            {
                toInsert.Clear();
            }
        }

        swTotal.Stop();
        // Log final memory and last processed id for diagnostics
        var mem = GC.GetTotalMemory(false);
        _logger.LogInformation("CSV ingestion completed in {Elapsed}. processed={Processed}, inserted={Inserted}, updated={Updated}, errors={Errors}, dropped={Dropped}, lastId={LastId}, memoryBytes={Memory}", swTotal.Elapsed, processed, inserted, updated, errors, dropped, lastProcessedId, mem);

        var summary = new
        {
            processed,
            inserted,
            updated,
            errors,
            dropped
        };

        return Ok(summary);
    }

    [HttpPost("upload-json")]
    public async Task<IActionResult> UploadJson([FromBody] IEnumerable<ExternalCarListing>? listings)
    {
        if (listings == null)
            return BadRequest(new { message = "No car listings provided" });

        var input = listings as IList<ExternalCarListing> ?? listings.ToList();
        if (input.Count == 0)
            return BadRequest(new { message = "No car listings provided" });

        var sw = Stopwatch.StartNew();
        var batch = new List<WriteModel<BsonDocument>>();
        var processed = 0;
        var flushed = 0;
        var lastUrl = "(none)";

        foreach (var listing in input)
        {
            processed++;
            lastUrl = listing.Url;

            var doc = BuildCarDocument(listing, "json-upload");
            var filter = !string.IsNullOrWhiteSpace(listing.Url)
                ? Builders<BsonDocument>.Filter.Eq("url", listing.Url)
                : Builders<BsonDocument>.Filter.Eq("_id", listing.Id);

            batch.Add(new ReplaceOneModel<BsonDocument>(filter, doc) { IsUpsert = true });

            if (batch.Count >= 500)
            {
                await FlushJsonBatchAsync(batch, lastUrl, processed);
                flushed += batch.Count;
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await FlushJsonBatchAsync(batch, lastUrl, processed);
            flushed += batch.Count;
            batch.Clear();
        }

        sw.Stop();
        _logger.LogInformation("JSON ingestion processed {Processed} listings in {Elapsed}. Flushed {Flushed} ops. LastUrl={LastUrl}",
            processed, sw.Elapsed, flushed, lastUrl);

        return Ok(new { processed, flushed, lastUrl, elapsed = sw.Elapsed });
    }

    private async Task FlushJsonBatchAsync(List<WriteModel<BsonDocument>> batch, string lastUrl, int processed)
    {
        if (batch.Count == 0)
            return;

        var sw = Stopwatch.StartNew();
        try
        {
            await _carsCollection.BulkWriteAsync(batch, new BulkWriteOptions { IsOrdered = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON batch write failed for {BatchSize} docs at {LastUrl}. Processed={Processed}",
                batch.Count, lastUrl, processed);
            throw;
        }
        finally
        {
            sw.Stop();
        }

        _logger.LogInformation("Flushed JSON batch of {BatchSize} docs in {ElapsedMs}ms (processed={Processed}, lastUrl={LastUrl})",
            batch.Count, sw.ElapsedMilliseconds, processed, lastUrl);
    }

    private static BsonDocument BuildCarDocument(ExternalCarListing listing, string source)
    {
        var now = DateTime.UtcNow;
        return new BsonDocument
        {
            ["url"] = ValueOrNull(listing.Url),
            ["manufacturer"] = ValueOrNull(listing.Manufacturer),
            ["model"] = ValueOrNull(listing.Model),
            ["year"] = listing.Year,
            ["price"] = listing.Price,
            ["odometer"] = listing.Odometer,
            ["transmission"] = ValueOrNull(listing.Transmission),
            ["condition"] = ValueOrNull(listing.Condition),
            ["fuel"] = ValueOrNull(listing.Fuel),
            ["type"] = ValueOrNull(listing.Type),
            ["region"] = ValueOrNull(listing.Region),
            ["image_url"] = ValueOrNull(listing.ImageUrl),
            ["vin"] = ValueOrNull(listing.Vin),
            ["paint_color"] = ValueOrNull(listing.PaintColor),
            ["posting_date"] = listing.PostingDate,
            ["source"] = source,
            ["crawled_at"] = now
        };
    }

    private static BsonValue ValueOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? BsonNull.Value : value;
}
