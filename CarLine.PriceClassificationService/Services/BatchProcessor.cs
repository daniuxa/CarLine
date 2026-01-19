using CarLine.Common.Models;
using CarLine.Common.Services;
using CarLine.PriceClassificationService.Models;
using Elastic.Clients.Elasticsearch;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CarLine.PriceClassificationService.Services;

internal sealed class BatchProcessor(
    IMlInferenceClient mlClient,
    IMongoCollection<BsonDocument> carsCollection,
    ElasticsearchClient elasticsearchClient,
    ILogger logger)
{
    public async Task<(int classified, int errors)> ProcessBatchAsync(
        List<(BsonDocument doc, CarPredictionRequestData data)> batch,
        CancellationToken cancellationToken)
    {
        var classified = 0;
        var errors = 0;

        try
        {
            // Prepare batch request
            var requests = batch.Select(b => new CarPredictionRequest
            {
                Manufacturer = b.data.Manufacturer,
                Model = b.data.Model,
                Year = b.data.Year,
                Odometer = (float)b.data.Odometer,
                Transmission = b.data.Transmission,
                Condition = b.data.Condition,
                Fuel = b.data.Fuel,
                Type = b.data.Type,
                Region = b.data.Region
            }).ToList();

            BatchPredictionResponse? result;
            try
            {
                result = await mlClient.PredictBatchAsync(requests, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Cannot connect to ML service for batch prediction");
                return (0, batch.Count);
            }

            if (result?.Predictions == null)
            {
                logger.LogError("ML service returned null predictions for batch");
                return (0, batch.Count);
            }

            // Update MongoDB with predictions
            var bulkUpdates = new List<WriteModel<BsonDocument>>();
            var esUpdates =
                new List<(string id, string classification, decimal predictedPrice, decimal priceDiff, DateTime
                    classDate)>();

            for (var i = 0; i < result.Predictions.Count && i < batch.Count; i++)
                try
                {
                    var prediction = result.Predictions[i];
                    var (doc, data) = batch[i];

                    if (prediction.PredictedPrice == null)
                    {
                        errors++;
                        continue;
                    }

                    var predictedPrice = prediction.PredictedPrice.Value;
                    var actualPrice = data.ActualPrice;

                    // Calculate price classification - use decimal arithmetic
                    var priceDifference = (actualPrice - predictedPrice) / predictedPrice * 100m;
                    var priceClassification = ClassifyPrice(priceDifference);
                    var classificationDate = DateTime.UtcNow;
                    var classificationString = priceClassification.ToStorageString();

                    // Build MongoDB update
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]);
                    var update = Builders<BsonDocument>.Update
                        .Set("price_classification", classificationString)
                        .Set("predicted_price", Math.Round(predictedPrice, 2))
                        .Set("price_difference_percent", Math.Round(priceDifference, 2))
                        .Set("classification_date", classificationDate);

                    bulkUpdates.Add(new UpdateOneModel<BsonDocument>(filter, update));

                    // Track Elasticsearch update - decide ES id carefully
                    string? esId = null;
                    if (doc.Contains("id") && !doc["id"].IsBsonNull)
                    {
                        var idVal = doc["id"].ToString();
                        if (!string.IsNullOrWhiteSpace(idVal))
                            esId = idVal;
                    }

                    if (esId == null && doc.Contains("url") && !doc["url"].IsBsonNull)
                    {
                        var urlVal = doc["url"].ToString();
                        if (!string.IsNullOrWhiteSpace(urlVal))
                            esId = urlVal;
                    }

                    if (!string.IsNullOrWhiteSpace(esId))
                        esUpdates.Add((esId, classificationString,
                            Math.Round(predictedPrice, 2), Math.Round(priceDifference, 2), classificationDate));
                    else
                        logger.LogWarning(
                            "Skipping Elasticsearch update for MongoDB document {mongoId} because no URL-based id available",
                            doc.GetValue("_id").ToString());

                    classified++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error processing prediction result for car in batch");
                    errors++;
                }

            // Execute MongoDB bulk update
            if (bulkUpdates.Count > 0)
                await carsCollection.BulkWriteAsync(bulkUpdates, cancellationToken: cancellationToken);

            // Execute Elasticsearch bulk update
            if (esUpdates.Count > 0)
                try
                {
                    await UpdateElasticsearchBulkAsync(esUpdates, cancellationToken);
                    logger.LogInformation("Updated {count} documents in Elasticsearch with price classifications",
                        esUpdates.Count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to update Elasticsearch with price classifications");
                }

            // Account for any errors from ML service
            errors += result.Errors?.Count ?? 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing batch");
            errors += batch.Count;
        }

        return (classified, errors);
    }

    private async Task UpdateElasticsearchBulkAsync(
        List<(string id, string classification, decimal predictedPrice, decimal priceDiff, DateTime classDate)> updates,
        CancellationToken cancellationToken)
    {
        var bulkResponse = await elasticsearchClient.BulkAsync(b =>
        {
            foreach (var (id, classification, predictedPrice, priceDiff, classDate) in updates)
            {
                var partialDoc = new
                {
                    price_classification = classification,
                    predicted_price = predictedPrice,
                    price_difference_percent = priceDiff,
                    classification_date = classDate
                };

                b.Update<CarDocument, object>(u => u
                    .Index(ElasticsearchHelper.CarsIndexName)
                    .Id(id)
                    .Doc(partialDoc));
            }
        }, cancellationToken);

        if (!bulkResponse.IsValidResponse)
        {
            logger.LogError("Elasticsearch bulk update failed: {error}", bulkResponse.DebugInformation);
            return;
        }

        if (bulkResponse.Errors)
        {
            var alreadyExists = 0;
            var realErrors = 0;
            foreach (var item in bulkResponse.ItemsWithErrors)
            {
                if (item.Status == 409)
                    alreadyExists++;
                else
                    realErrors++;

                var errorType = item.Error?.Type ?? "unknown_type";
                var errorReason = item.Error?.Reason ?? "Unknown error";
                var causedBy = item.Error?.CausedBy != null
                    ? $" | Caused by: {item.Error.CausedBy.Type}: {item.Error.CausedBy.Reason}"
                    : string.Empty;

                logger.LogError(
                    "Bulk item error for document {id}: Status={status}, Type={type}, Reason={reason}{causedBy}",
                    item.Id, item.Status, errorType, errorReason, causedBy);
            }

            logger.LogError(
                "Elasticsearch bulk update completed with {realErrors} real errors and {exists} conflicts (already existed)",
                realErrors, alreadyExists);
            return;
        }

        logger.LogInformation("Successfully updated {count} documents in Elasticsearch", updates.Count);
    }

    private static PriceClassification ClassifyPrice(decimal priceDifference)
    {
        if (priceDifference < -10m)
            return PriceClassification.Low;

        if (priceDifference > 10m)
            return PriceClassification.High;

        return PriceClassification.Normal;
    }
}