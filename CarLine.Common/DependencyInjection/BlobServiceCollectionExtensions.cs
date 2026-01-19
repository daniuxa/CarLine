using Azure.Storage.Blobs;
using CarLine.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarLine.Common.DependencyInjection;

public sealed record BlobStorageOptions
{
    public IReadOnlyList<string> ConfigurationKeys { get; init; } = new[] { "ConnectionStrings:azureblobstorage" };
    public IReadOnlyList<string> EnvironmentVariables { get; init; } = new[]
    {
        "ConnectionStrings__azureblobstorage",
        StorageConstants.BlobStorage,
        StorageConstants.ModelsContainer,
        "modelscontainer",
        "ConnectionStrings__modelscontainer"
    };
    public string? FallbackConnectionString { get; init; }
    public string ContainerName { get; init; } = StorageConstants.ModelsContainer;
    public bool EnsureContainerExists { get; init; } = true;
}

public static class BlobServiceCollectionExtensions
{
    public static IServiceCollection AddCarLineBlobStorage(this IServiceCollection services, IConfiguration configuration, BlobStorageOptions? options = null)
    {
        var opts = options ?? new();

        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<BlobServiceClient>>();
            var (connectionString, source) = ResolveConnectionString(configuration, opts);
            logger.LogInformation("Blob storage connection string resolved from {source}", source);
            return new BlobServiceClient(connectionString);
        });

        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<BlobContainerClient>>();
            var client = sp.GetRequiredService<BlobServiceClient>();
            var containerClient = client.GetBlobContainerClient(opts.ContainerName);

            if (opts.EnsureContainerExists)
            {
                try
                {
                    var created = containerClient.CreateIfNotExists();
                    logger.LogInformation(created != null
                        ? "Created blob container '{container}' at startup."
                        : "Blob container '{container}' already exists.",
                        opts.ContainerName);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to ensure blob container '{container}' at startup.", opts.ContainerName);
                }
            }

            return containerClient;
        });

        return services;
    }

    private static (string ConnectionString, string Source) ResolveConnectionString(IConfiguration configuration, BlobStorageOptions options)
    {
        foreach (var key in options.ConfigurationKeys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return (value, $"config:{key}");
            }
        }

        foreach (var env in options.EnvironmentVariables)
        {
            if (string.IsNullOrWhiteSpace(env))
            {
                continue;
            }

            var value = Environment.GetEnvironmentVariable(env);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return (value, $"env:{env}");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.FallbackConnectionString))
        {
            return (options.FallbackConnectionString!, "fallback");
        }

        throw new InvalidOperationException(
            "Blob storage connection string was not found. Provide a configuration key or environment variable listed in BlobStorageOptions.");
    }
}
