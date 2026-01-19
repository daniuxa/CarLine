using CarLine.Common.Services;
using CarLine.PriceClassificationService.Models;
using Elastic.Clients.Elasticsearch;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CarLine.PriceClassificationService.Services;

public class PriceClassificationService
{
    private const int BatchSize = 1000;
    private readonly BatchProcessor _batchProcessor;
    private readonly IMongoCollection<BsonDocument> _carsCollection;
    private readonly ILogger<PriceClassificationService> _logger;

    public PriceClassificationService(
        IMongoClient mongoClient,
        IMlInferenceClient mlClient,
        ILogger<PriceClassificationService> logger,
        IConfiguration configuration,
        ElasticsearchClient elasticsearchClient)
    {
        _logger = logger;

        var dbName = configuration["PriceClassificationService:DatabaseName"] ?? "carsnosql";
        var collectionName = configuration["PriceClassificationService:CollectionName"] ?? "cleaned_cars";

        var database = mongoClient.GetDatabase(dbName);
        _carsCollection = database.GetCollection<BsonDocument>(collectionName);

        // Create batch processor (uses moved logic)
        _batchProcessor = new BatchProcessor(mlClient, _carsCollection, elasticsearchClient, _logger);
    }

    public async Task ClassifyAllCarsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting price classification for all cars...");
        var startTime = DateTime.UtcNow;

        int processed = 0, classified = 0, errors = 0;

        try
        {
            _logger.LogInformation("Attempting to connect to MongoDB collection: {collection}",
                _carsCollection.CollectionNamespace.FullName);

            // Find all cars without price classification or that need reclassification
            var filter = FilterDefinition<BsonDocument>.Empty;
            var totalCars = await _carsCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

            _logger.LogInformation("Found {total} cars to process", totalCars);

            if (totalCars == 0)
            {
                _logger.LogWarning("No cars found in cleaned_cars collection. Make sure DataCleanUp has run first.");
                return;
            }

            var options = new FindOptions<BsonDocument>
            {
                BatchSize = BatchSize
            };

            using var cursor = await _carsCollection.FindAsync(filter, options, cancellationToken);

            var carBatch = new List<(BsonDocument doc, CarPredictionRequestData data)>();

            while (await cursor.MoveNextAsync(cancellationToken))
                foreach (var car in cursor.Current)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Extract car data
                    if (TryExtractCarData(car, out var manufacturer, out var model, out var year,
                            out var odometer, out var transmission, out var condition, out var fuel,
                            out var type, out var region, out var actualPrice, out var status) && status == "ACTIVE")
                    {
                        carBatch.Add((car, new CarPredictionRequestData
                        {
                            Manufacturer = manufacturer,
                            Model = model,
                            Year = year,
                            Odometer = odometer,
                            Transmission = transmission,
                            Condition = condition,
                            Fuel = fuel,
                            Type = type,
                            Region = region ?? "",
                            ActualPrice = actualPrice
                        }));

                        // Process batch when full
                        if (carBatch.Count >= BatchSize)
                        {
                            var (batchClassified, batchErrors, batchProcessed) = await ProcessAndLogBatchAsync(carBatch,
                                totalCars, processed, classified, errors, cancellationToken);
                            classified += batchClassified;
                            errors += batchErrors;
                            processed += batchProcessed;

                            carBatch.Clear();
                        }
                    }
                    else
                    {
                        errors++;
                        processed++;
                    }
                }

            // Process remaining cars in final batch
            if (carBatch.Count > 0)
            {
                var (batchClassified, batchErrors, batchProcessed) = await ProcessAndLogBatchAsync(carBatch, totalCars,
                    processed, classified, errors, cancellationToken);
                classified += batchClassified;
                errors += batchErrors;
                processed += batchProcessed;

                _logger.LogInformation(
                    "Final batch: {processed}/{total} cars processed, {classified} classified, {errors} errors",
                    processed, totalCars, classified, errors);
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Price classification complete. Processed: {processed}, Classified: {classified}, Errors: {errors}, Duration: {duration}",
                processed, classified, errors, duration);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex,
                "MongoDB connection timeout. Please check: 1) MongoDB is running, 2) Connection string is correct, 3) Network connectivity");
            throw;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB error during price classification. Error code: {code}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during price classification");
            throw;
        }
    }

    private static bool TryExtractCarData(BsonDocument car,
        out string manufacturer, out string model, out int year, out double odometer,
        out string transmission, out string condition, out string fuel, out string type,
        out string? region, out decimal actualPrice, out string status)
    {
        manufacturer = model = transmission = condition = fuel = type = string.Empty;
        region = null;
        year = 0;
        odometer = 0;
        actualPrice = 0;
        status = string.Empty;

        try
        {
            // Required fields
            if (!car.Contains("manufacturer") || !car.Contains("model") || !car.Contains("year") ||
                !car.Contains("odometer") || !car.Contains("price") ||
                !car.Contains("transmission") || !car.Contains("condition") ||
                !car.Contains("fuel") || !car.Contains("type") || !car.Contains("status"))
                return false;

            manufacturer = car["manufacturer"].AsString;
            model = car["model"].AsString;
            year = car["year"].ToInt32();
            status = car["status"].AsString;

            // Odometer might be stored as string or number, handle both
            var odoValue = car["odometer"];
            if (odoValue.IsString)
            {
                if (!double.TryParse(odoValue.AsString, out odometer))
                    return false;
            }
            else if (odoValue.IsNumeric)
            {
                odometer = odoValue.ToDouble();
            }
            else
            {
                return false;
            }

            // Price
            var priceValue = car["price"];
            if (priceValue.IsString)
            {
                if (!decimal.TryParse(priceValue.AsString, out actualPrice))
                    return false;
            }
            else if (priceValue.IsNumeric)
            {
                actualPrice = priceValue.ToDecimal();
            }
            else
            {
                return false;
            }

            transmission = car["transmission"].AsString;
            condition = car["condition"].AsString;
            fuel = car["fuel"].AsString;
            type = car["type"].AsString;

            // Optional field
            if (car.Contains("region") && !car["region"].IsBsonNull) region = car["region"].AsString;

            return !string.IsNullOrWhiteSpace(manufacturer) &&
                   !string.IsNullOrWhiteSpace(model) &&
                   year > 0 &&
                   actualPrice > 0;
        }
        catch
        {
            return false;
        }
    }

    // New helper appended at end of class — uses BatchProcessor
    private async Task<(int classified, int errors, int processed)> ProcessAndLogBatchAsync(
        List<(BsonDocument doc, CarPredictionRequestData data)> carBatch,
        long totalCars,
        int currentProcessed,
        int currentClassified,
        int currentErrors,
        CancellationToken cancellationToken)
    {
        var (batchClassified, batchErrors) = await _batchProcessor.ProcessBatchAsync(carBatch, cancellationToken);

        var batchProcessed = carBatch.Count;

        var newProcessed = currentProcessed + batchProcessed;
        var newClassified = currentClassified + batchClassified;
        var newErrors = currentErrors + batchErrors;

        _logger.LogInformation("Progress: {processed}/{total} cars processed, {classified} classified, {errors} errors",
            newProcessed, totalCars, newClassified, newErrors);

        // clear the caller's batch (we also cleared earlier in caller; keep for safety)
        carBatch.Clear();

        return (batchClassified, batchErrors, batchProcessed);
    }
}