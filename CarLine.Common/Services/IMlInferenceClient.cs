using CarLine.Common.Models;

namespace CarLine.Common.Services;

public interface IMlInferenceClient
{
    Task<PredictionResponse?> PredictAsync(CarPredictionRequest request, CancellationToken cancellationToken = default);
    Task<BatchPredictionResponse?> PredictBatchAsync(IReadOnlyCollection<CarPredictionRequest> requests, CancellationToken cancellationToken = default);
    Task<bool> HealthAsync(CancellationToken cancellationToken = default);
}
