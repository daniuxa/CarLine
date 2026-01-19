using Azure.Storage.Blobs;
using CarLine.Common;
using CarLine.Common.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Trainers.LightGbm;

namespace CarLine.TrainingFunction;

public class TrainModelFunction(ILogger<TrainModelFunction> logger, BlobContainerClient container)
{
    private readonly MLContext _ml = new();

    [Function(nameof(TrainModelFunction))]
    public async Task Run(
        [BlobTrigger(StorageConstants.ModelsContainer + "/cleaned/{name}")]
        Stream csvStream,
        string name,
        FunctionContext context)
    {
        if (!name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Skipping non-CSV file: {name}", name);
            return;
        }

        var startTime = DateTime.UtcNow;
        logger.LogInformation("Blob trigger activated for CSV: {name}. Starting training...", name);

        var tempCsvPath = Path.Combine(Path.GetTempPath(), $"cleaned_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        var tempModelPath = Path.Combine(Path.GetTempPath(), $"CarPriceModel_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip");

        try
        {
            // Save stream to temp file
            logger.LogInformation("Saving CSV to {path}", tempCsvPath);
            Directory.CreateDirectory(Path.GetDirectoryName(tempCsvPath) ?? Path.GetTempPath());
            await using (var fileStream = File.Create(tempCsvPath))
            {
                await csvStream.CopyToAsync(fileStream);
            }

            // Load data
            logger.LogInformation("Loading data and preparing pipeline...");
            var data = _ml.Data.LoadFromTextFile<CarTrainingModel>(tempCsvPath, hasHeader: true, separatorChar: ',');
            var cache = _ml.Data.Cache(data);

            // Build pipeline with safer handling for high-cardinality columns and odometer log transform
            var catSteps = new List<IEstimator<ITransformer>>
            {
                // For manufacturer/model (potentially very high cardinality) use hashed one-hot to cap dimensionality
                _ml.Transforms.Categorical.OneHotHashEncoding("manufacturer_encoded", "manufacturer",
                    numberOfBits: 13), // 8192 buckets
                _ml.Transforms.Categorical.OneHotHashEncoding("model_encoded", "model", numberOfBits: 13),

                // For lower-cardinality categoricals we keep OneHotEncoding to produce a dense float vector
                _ml.Transforms.Categorical.OneHotEncoding("fuel_encoded", "fuel"),
                _ml.Transforms.Categorical.OneHotEncoding("transmission_encoded", "transmission"),
                _ml.Transforms.Categorical.OneHotEncoding("condition_encoded", "condition"),
                _ml.Transforms.Categorical.OneHotEncoding("type_encoded", "type"),
                _ml.Transforms.Categorical.OneHotEncoding("region_encoded", "region")
            };

            // Chain all transforms
            var pipeline = catSteps.Any()
                ? catSteps.Aggregate((a, b) => a.Append(b))
                : _ml.Transforms.CopyColumns("Features", "year"); // fallback

            pipeline = pipeline.Append(_ml.Transforms.Concatenate(
                "Features",
                "manufacturer_encoded",
                "model_encoded",
                "fuel_encoded",
                "transmission_encoded",
                "condition_encoded",
                "type_encoded",
                "region_encoded",
                "year",
                "odometer"));

            // Configure LightGBM trainer with conservative options to avoid overflow/memory issues
            var lgbOptions = new LightGbmRegressionTrainer.Options
            {
                LabelColumnName = "price",
                FeatureColumnName = "Features",

                // Core complexity
                NumberOfLeaves = 128,
                MinimumExampleCountPerLeaf = 50,

                // Learning
                LearningRate = 0.05,
                NumberOfIterations = 300,

                // Regularization
                L2CategoricalRegularization = 1.0,

                // Memory / speed
                MaximumBinCountPerFeature = 255,

                // Parallelism
                NumberOfThreads = Environment.ProcessorCount,

                // Metrics
                EvaluationMetric = LightGbmRegressionTrainer.Options.EvaluateMetricType.RootMeanSquaredError
            };

            pipeline = pipeline.Append(_ml.Regression.Trainers.LightGbm(lgbOptions))
                .AppendCacheCheckpoint(_ml);

            logger.LogInformation("Pipeline constructed. Beginning training...");
            logger.LogInformation("Training model...");
            var model = pipeline.Fit(cache);

            logger.LogInformation("Saving model to {path}", tempModelPath);
            await using (var fs = File.Create(tempModelPath))
            {
                _ml.Model.Save(model, cache.Schema, fs);
            }

            // Upload trained model
            var modelBlobName = $"models/{Path.GetFileName(tempModelPath)}";
            var modelBlobClient = container.GetBlobClient(modelBlobName);
            logger.LogInformation("Uploading model to blob {blobName}", modelBlobName);
            await using (var modelStream = File.OpenRead(tempModelPath))
            {
                await modelBlobClient.UploadAsync(modelStream, true);
            }

            var duration = DateTime.UtcNow - startTime;
            logger.LogInformation(
                "Training and upload complete in {duration} for CSV {name}. Model saved to: {modelBlobName}",
                duration, name, modelBlobName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Training failed for CSV {name}", name);
            throw;
        }
        finally
        {
            TryDeleteTempFile(tempCsvPath);
            TryDeleteTempFile(tempModelPath);
        }
    }

    private void TryDeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to delete temp file {path}", path);
        }
    }
}