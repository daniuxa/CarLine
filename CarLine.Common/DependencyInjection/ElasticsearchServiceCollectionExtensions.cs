using Elastic.Clients.Elasticsearch;
using CarLine.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarLine.Common.DependencyInjection;

public static class ElasticsearchServiceCollectionExtensions
{
    /// <summary>
    /// Registers an ElasticsearchClient using the existing config/env conventions in this solution.
    /// </summary>
    public static IServiceCollection AddCarLineElasticsearch(this IServiceCollection services, IConfiguration configuration,
        string? connectionStringName = "elasticsearch",
        string fallbackConnectionString = "http://localhost:9200",
        bool disableDirectStreaming = false,
        string defaultIndex = ElasticsearchHelper.CarsIndexName)
    {
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("ElasticsearchClient");

            string? conn = null;

            if (!string.IsNullOrWhiteSpace(connectionStringName))
            {
                conn = configuration.GetConnectionString(connectionStringName);
            }

            conn ??= Environment.GetEnvironmentVariable("ConnectionStrings__elasticsearch");
            conn ??= fallbackConnectionString;

            logger.LogInformation("Elasticsearch connection string resolved: {connectionString}", conn);

            var settings = new ElasticsearchClientSettings(new Uri(conn))
                .DefaultIndex(defaultIndex);

            if (disableDirectStreaming)
            {
                settings = settings.DisableDirectStreaming();
            }

            return new ElasticsearchClient(settings);
        });

        return services;
    }
}

