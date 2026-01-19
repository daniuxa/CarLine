using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CarLine.Crawler.Services;

public sealed class CarCrawlerService(
    ILogger<CarCrawlerService> logger,
    IHttpClientFactory httpClientFactory,
    ICrawledCarsRepository repository,
    IOptions<CrawlerSettings> settings) : ICarCrawlerService
{
    private readonly CrawlerSettings _settings = settings.Value;

    public async Task FetchFromExternalApisAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting fetch from external APIs at: {Time}", DateTime.UtcNow);

        var enabledApis = _settings.ExternalApis.Where(a => a.Enabled).ToList();

        if (!enabledApis.Any())
        {
            logger.LogWarning("No enabled external APIs configured.");
            return;
        }

        foreach (var apiConfig in enabledApis)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await FetchFromApiAsync(apiConfig, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching from API: {ApiName}", apiConfig.Name);
            }
        }

        logger.LogInformation("Completed fetch from external APIs at: {Time}", DateTime.UtcNow);
    }

    private async Task FetchFromApiAsync(ExternalApiConfig apiConfig, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching data from {ApiName} ({BaseUrl})", apiConfig.Name, apiConfig.BaseUrl);

        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(apiConfig.BaseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        var totalFetched = 0;
        var page = 1;
        var pageSize = Math.Min(50, _settings.MaxCarsPerFetch);

        while (totalFetched < _settings.MaxCarsPerFetch && !cancellationToken.IsCancellationRequested)
        {
            var url = $"/api/cars?page={page}&pageSize={pageSize}";
            logger.LogInformation("Requesting: {Url}", url);

            HttpResponseMessage response;
            try
            {
                response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "HTTP error fetching page {Page} from {ApiName}", page, apiConfig.Name);
                break;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            ExternalApiResponse? apiResponse;
            try
            {
                apiResponse = JsonSerializer.Deserialize<ExternalApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Invalid JSON from {ApiName} (page {Page})", apiConfig.Name, page);
                break;
            }

            if (apiResponse?.Data == null || apiResponse.Data.Count == 0)
            {
                logger.LogInformation("No more data available from {ApiName}", apiConfig.Name);
                break;
            }

            await repository.UpsertManyAsync(apiResponse.Data, apiConfig.Name, cancellationToken);

            totalFetched += apiResponse.Data.Count;
            logger.LogInformation("Fetched {Count} cars from {ApiName} (page {Page}). Total: {Total}",
                apiResponse.Data.Count, apiConfig.Name, page, totalFetched);

            if (totalFetched >= _settings.MaxCarsPerFetch || apiResponse.Data.Count < pageSize)
                break;

            page++;

            await Task.Delay(500, cancellationToken);
        }

        logger.LogInformation("Completed fetch from {ApiName}. Total cars fetched: {Total}", apiConfig.Name, totalFetched);
    }
}
