using Microsoft.ML;
using Azure.Storage.Blobs;
using CarLine.Common;
using CarLine.Common.Models;

namespace CarLine.MLInterferenceService.Services;

public class CarPricePredictionService(
    MLContext mlContext,
    ILogger<CarPricePredictionService> logger,
    BlobServiceClient blobServiceClient)
{
    private readonly BlobContainerClient _blobClient = blobServiceClient.GetBlobContainerClient(StorageConstants.ModelsContainer);
    private ITransformer? _model;
    private PredictionEngine<CarTrainingModel, CarPricePrediction>? _predictionEngine;
    private readonly SemaphoreSlim _modelLock = new(1, 1);

    public async Task<CarPricePrediction> PredictPriceAsync(CarPredictionRequest request)
    {
        // Ensure model is loaded
        await EnsureModelLoadedAsync();

        if (_predictionEngine == null)
        {
            throw new InvalidOperationException("Model not loaded. No trained model available.");
        }

        // Apply same transformations as during training
        var input = new CarTrainingModel()
        {
            manufacturer = request.Manufacturer.Trim().ToLowerInvariant(),
            model = NormalizeModelName(request.Model), // Apply first-word normalization
            year = request.Year,
            odometer = (float)Math.Log10(request.Odometer + 1), // Apply log transformation
            condition = request.Condition?.Trim().ToLowerInvariant(),
            fuel = request.Fuel?.Trim().ToLowerInvariant(),
            transmission = request.Transmission?.Trim().ToLowerInvariant(),
            type = request.Type?.Trim().ToLowerInvariant(),
            region = request.Region?.Trim().ToLowerInvariant()
        };

        logger.LogInformation(
            "Predicting price for: {manufacturer} {model} {year}, odometer: {odometer} (log: {odoLog})",
            input.manufacturer, input.model, input.year, request.Odometer, input.odometer);

        var prediction = _predictionEngine.Predict(input);

        logger.LogInformation("Predicted price: ${price}", prediction.Score);

        return prediction;
    }

    private async Task EnsureModelLoadedAsync()
    {
        if (_predictionEngine != null)
            return;

        await _modelLock.WaitAsync();
        try
        {
            if (_predictionEngine != null)
                return;

            logger.LogInformation("Loading trained model from blob storage...");

            // Find the latest model in the models/ folder
            var blobs = _blobClient.GetBlobsAsync(prefix: "models/CarPriceModel_");
            string? latestBlobName = null;
            var latestDate = DateTimeOffset.MinValue;

            await foreach (var blob in blobs)
            {
                if (blob.Properties.CreatedOn > latestDate)
                {
                    latestDate = blob.Properties.CreatedOn ?? DateTimeOffset.MinValue;
                    latestBlobName = blob.Name;
                }
            }

            if (latestBlobName == null)
            {
                logger.LogWarning("No trained model found in blob storage.");
                throw new InvalidOperationException("No trained model available. Please train a model first.");
            }

            logger.LogInformation("Found latest model: {modelName}, created: {created}", latestBlobName, latestDate);

            // Download model to temp file
            var tempModelPath = Path.Combine(Path.GetTempPath(), $"CarPriceModel_{Guid.NewGuid()}.zip");
            var blobClient = _blobClient.GetBlobClient(latestBlobName);

            await using (var fileStream = File.Create(tempModelPath))
            {
                await blobClient.DownloadToAsync(fileStream);
            }

            // Load model
            _model = mlContext.Model.Load(tempModelPath, out var modelSchema);
            _predictionEngine = mlContext.Model.CreatePredictionEngine<CarTrainingModel, CarPricePrediction>(_model);

            logger.LogInformation("Model loaded successfully from {modelName}", latestBlobName);

            // Clean up temp file
            try
            {
                File.Delete(tempModelPath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete temp model file {path}", tempModelPath);
            }
        }
        finally
        {
            _modelLock.Release();
        }
    }

    // Apply same normalization as during training (first word only)
    private static string NormalizeModelName(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return string.Empty;

        model = model.Trim().ToLowerInvariant();
        var firstWord = model.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        return firstWord ?? string.Empty;
    }
}
