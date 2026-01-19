using System.Net.Http.Json;
using CarLine.Common.Models;

namespace CarLine.Common.Services;

public sealed class MlInferenceClient(HttpClient httpClient) : IMlInferenceClient
{
    public async Task<PredictionResponse?> PredictAsync(CarPredictionRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("/api/CarPrediction/predict", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PredictionResponse>(cancellationToken);
    }

    public async Task<BatchPredictionResponse?> PredictBatchAsync(IReadOnlyCollection<CarPredictionRequest> requests,
        CancellationToken cancellationToken = default)
    {
        using var response =
            await httpClient.PostAsJsonAsync("/api/CarPrediction/predict/batch", requests, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BatchPredictionResponse>(cancellationToken);
    }

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("/api/CarPrediction/health", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}