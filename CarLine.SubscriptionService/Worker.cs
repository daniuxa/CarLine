using CarLine.SubscriptionService.Services;

namespace CarLine.SubscriptionService;

public sealed class Worker(ILogger<Worker> logger, IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once immediately, then every 24 hours
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<SubscriptionProcessingService>();

                await processor.ProcessAllAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Subscription worker run failed");
            }

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken).ConfigureAwait(false);
        }
    }
}