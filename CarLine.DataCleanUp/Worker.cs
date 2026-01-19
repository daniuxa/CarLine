using CarLine.DataCleanUp.Services;

namespace CarLine.DataCleanUp;

public class Worker(ILogger<Worker> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once immediately, then every 24 hours
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<DataCleanupService>();

                var result = await cleanupService.RunCleanupAsync(stoppingToken);

                if (result.Success)
                    _logger.LogInformation(
                        "Cleanup completed: {Written} rows written, {Dropped} dropped, {Errors} errors in {Duration}. Blob: {Blob}",
                        result.RowsWritten, result.RowsDropped, result.Errors, result.Duration, result.BlobName);
                else
                    _logger.LogWarning("Cleanup failed: {Message}", result.Message);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data cleanup run failed");
            }

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken).ConfigureAwait(false);
        }
    }
}