using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CarLine.Common.DependencyInjection;

public static class MongoServiceCollectionExtensions
{
    /// <summary>
    ///     Registers a shared MongoDB IMongoClient using common config/env conventions in this solution.
    /// </summary>
    public static IServiceCollection AddCarLineMongoClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string? configurationKey = null,
        string fallbackConnectionString = "mongodb://localhost:27017",
        bool allowLocalFallback = true)
    {
        services.AddSingleton<IMongoClient>(sp =>
        {
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("MongoClient");

            // Prefer explicit config key if provided (lets each service override its own settings section)
            var conn = !string.IsNullOrWhiteSpace(configurationKey) ? configuration[configurationKey] : null;

            // Common connection string sources across services
            conn ??= configuration.GetConnectionString("mongodb");
            conn ??= configuration.GetConnectionString("carsnosql");
            conn ??= configuration["MongoDB:ConnectionString"];

            // Per-service legacy key used in some projects
            conn ??= configuration["PriceClassificationService:MongoConnectionString"];

            // Aspire / environment variables
            conn ??= Environment.GetEnvironmentVariable("ConnectionStrings__mongodb");

            if (string.IsNullOrWhiteSpace(conn))
            {
                if (allowLocalFallback)
                {
                    conn = fallbackConnectionString;
                    logger.LogWarning(
                        "MongoDB connection string not found in configuration. Using fallback: {ConnectionString}",
                        conn);
                }
                else
                {
                    throw new InvalidOperationException(
                        "MongoDB connection string is not configured. Provide ConnectionStrings:mongodb/carsnosql or env var ConnectionStrings__mongodb.");
                }
            }
            else
            {
                logger.LogInformation("MongoDB connection string resolved: {ConnectionString}",
                    conn.Contains("@") ? "[REDACTED]" : conn);
            }

            return new MongoClient(conn);
        });

        return services;
    }

    /// <summary>
    ///     Registers an IMongoDatabase based on the provided database name.
    /// </summary>
    public static IServiceCollection AddCarLineMongoDatabase(this IServiceCollection services, string databaseName)
    {
        services.AddScoped(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(databaseName);
        });

        return services;
    }
}