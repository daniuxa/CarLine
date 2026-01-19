namespace CarLine.PriceClassificationService;

public class Worker(
    ILogger<Worker> logger,
    Services.PriceClassificationService classificationService,
    IConfiguration configuration)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Price Classification Worker started");

        var runIntervalHours = int.Parse(configuration["PriceClassificationService:RunIntervalHours"] ?? "24");
        var runInterval = TimeSpan.FromHours(runIntervalHours);

        await RunClassificationAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                logger.LogInformation("Next classification run scheduled in {hours} hours", runIntervalHours);
                await Task.Delay(runInterval, stoppingToken);

                if (!stoppingToken.IsCancellationRequested) await RunClassificationAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("Price Classification Worker shutting down");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in classification worker loop");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
    }

    private async Task RunClassificationAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting price classification run at: {time}", DateTimeOffset.UtcNow);
            await classificationService.ClassifyAllCarsAsync(stoppingToken);
            logger.LogInformation("Price classification run completed at: {time}", DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Price classification run failed");
        }
    }
}