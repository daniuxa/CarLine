using CarLine.Crawler.Services;
using Microsoft.Extensions.Options;

namespace CarLine.Crawler;

public class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory,
    IOptions<CrawlerSettings> settings)
    : BackgroundService
{
    private readonly CrawlerSettings _settings = settings.Value;
    private DateTime _lastFetchTime = DateTime.MinValue;

    private async Task RunFetchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var crawlerService = scope.ServiceProvider.GetRequiredService<ICarCrawlerService>();
        await crawlerService.FetchFromExternalApisAsync(stoppingToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Crawler Worker started. Fetch interval: {Hours} hours, Max cars per fetch: {Max}",
            _settings.FetchIntervalHours, _settings.MaxCarsPerFetch);

        // Run immediately on startup, then on schedule
        await RunFetchAsync(stoppingToken);
        _lastFetchTime = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            var timeSinceLastFetch = DateTime.UtcNow - _lastFetchTime;
            var intervalTimeSpan = TimeSpan.FromHours(_settings.FetchIntervalHours);

            if (timeSinceLastFetch >= intervalTimeSpan)
            {
                await RunFetchAsync(stoppingToken);
                _lastFetchTime = DateTime.UtcNow;
            }
            else
            {
                var nextFetch = _lastFetchTime.Add(intervalTimeSpan);
                var timeUntilNextFetch = nextFetch - DateTime.UtcNow;
                logger.LogInformation("Next fetch scheduled at: {NextFetch} (in {Minutes} minutes)",
                    nextFetch, timeUntilNextFetch.TotalMinutes);
            }

            // Check every minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}