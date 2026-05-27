namespace CarLine.DataCleanUp.FSharp

open System
open System.Threading
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type Worker(logger: ILogger<Worker>, serviceProvider: IServiceProvider) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        task {
            while not ct.IsCancellationRequested do
                try
                    use scope   = serviceProvider.CreateScope()
                    let svc     = scope.ServiceProvider.GetRequiredService<DataCleanupService>()
                    let! result = svc.RunCleanupAsync(ct)

                    if result.Success then
                        logger.LogInformation(
                            "Run complete — Written:{w} Dropped:{d} Errors:{e} Duration:{dur} Blob:{blob}",
                            result.RowsWritten, result.RowsDropped, result.Errors, result.Duration, result.BlobName)
                    else
                        logger.LogWarning("Run failed: {msg}", result.Message)

                    do! Threading.Tasks.Task.Delay(TimeSpan.FromHours(24), ct)
                with
                | :? OperationCanceledException -> ()
                | ex -> logger.LogError(ex, "Unhandled error in Worker")
        } :> Threading.Tasks.Task

