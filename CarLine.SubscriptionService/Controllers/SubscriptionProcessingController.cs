using CarLine.SubscriptionService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarLine.SubscriptionService.Controllers;

[ApiController]
[Route("api/subscriptions-processing")]
public sealed class SubscriptionProcessingController(SubscriptionProcessingService processor) : ControllerBase
{
    // Manual trigger (useful in dev/demo instead of waiting for the daily worker)
    [HttpPost("run")]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        await processor.ProcessAllAsync(cancellationToken);
        return Ok(new { ok = true });
    }

    [HttpPost("run/{subscriptionId:guid}")]
    public async Task<IActionResult> RunOne([FromRoute] Guid subscriptionId, CancellationToken cancellationToken)
    {
        await processor.ProcessOneAsync(subscriptionId, cancellationToken);
        return Ok(new { ok = true });
    }
}