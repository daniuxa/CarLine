using CarLine.DataCleanUp.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarLine.DataCleanUp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CleanupController : ControllerBase
{
    private readonly ILogger<CleanupController> _logger;
    private readonly DataCleanupService _cleanupService;

    public CleanupController(ILogger<CleanupController> logger, DataCleanupService cleanupService)
    {
        _logger = logger;
        _cleanupService = cleanupService;
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunCleanup(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Manual cleanup endpoint called.");
        var result = await _cleanupService.RunCleanupAsync(cancellationToken).ConfigureAwait(false);
        if (result.Success)
            return Ok(result);
        return StatusCode(500, result);
    }
}

