using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarLine.Common.DependencyInjection;

public static class SubscriptionServiceClientServiceCollectionExtensions
{
    private const string DefaultBaseUrl = "http://localhost:5025";

    public static IServiceCollection AddSubscriptionServiceClient(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient("SubscriptionService", (sp, httpClient) =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SubscriptionServiceClient");

            var baseUrl =
                Environment.GetEnvironmentVariable("SUBSCRIPTIONSERVICE_HTTP")
                ?? Environment.GetEnvironmentVariable("SUBSCRIPTIONSERVICE__HTTP")
                ?? configuration.GetConnectionString("CarLine.SubscriptionService")
                ?? configuration["services:CarLine.SubscriptionService:http:0"]
                ?? configuration["SubscriptionService:Http:0"]
                ?? configuration["SubscriptionService:BaseUrl"]
                ?? DefaultBaseUrl;

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            {
                logger.LogWarning(
                    "Subscription service URL '{value}' is not a valid absolute URI; falling back to {fallback}",
                    baseUrl, DefaultBaseUrl);
                baseUri = new Uri(DefaultBaseUrl);
            }

            httpClient.BaseAddress = baseUri;
            logger.LogInformation("Subscription service base URL: {baseUrl}", baseUri);
        });

        return services;
    }
}