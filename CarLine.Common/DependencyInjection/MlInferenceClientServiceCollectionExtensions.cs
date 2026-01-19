using CarLine.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarLine.Common.DependencyInjection;

public static class MlInferenceClientServiceCollectionExtensions
{
    public static IServiceCollection AddMlInferenceClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IMlInferenceClient, MlInferenceClient>((sp, httpClient) =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("MlInferenceClient");

            var mlServiceUrl =
                Environment.GetEnvironmentVariable("CARLINEMLINFERENCESERVICE_HTTP")
                ?? Environment.GetEnvironmentVariable("CARLINEMLINFERENCESERVICE__HTTP")
                ?? configuration.GetConnectionString("CarLine.MLInterferenceService")
                ?? configuration["services:CarLine.MLInterferenceService:http:0"]
                ?? configuration["CarPricePrediction:MLServiceUrl"]
                ?? configuration["PriceClassificationService:MLServiceUrl"]
                ?? Environment.GetEnvironmentVariable("services__CarLine.MLInterferenceService__http__0")
                ?? "http://localhost:5000";

            if (!Uri.TryCreate(mlServiceUrl, UriKind.Absolute, out var baseUri))
            {
                logger.LogWarning("ML service URL '{value}' is not a valid absolute URI; falling back to http://localhost:5000", mlServiceUrl);
                baseUri = new Uri("http://localhost:5000");
            }

            httpClient.BaseAddress = baseUri;
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            logger.LogInformation("ML inference service base URL: {baseUrl}", baseUri);
        });

        return services;
    }
}

