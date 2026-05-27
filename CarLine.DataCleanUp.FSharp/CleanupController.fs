namespace CarLine.DataCleanUp.FSharp

open System.Threading
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging

[<ApiController>]
[<Route("api/[controller]")>]
type CleanupController(logger: ILogger<CleanupController>, cleanupService: DataCleanupService) =
    inherit ControllerBase()

    [<HttpPost("run")>]
    member this.RunCleanup(ct: CancellationToken) = task {
        logger.LogInformation("Manual cleanup triggered via API.")
        let! result = cleanupService.RunCleanupAsync(ct)
        return
            if result.Success then this.Ok(result)          :> IActionResult
            else               this.StatusCode(500, result) :> IActionResult
    }
