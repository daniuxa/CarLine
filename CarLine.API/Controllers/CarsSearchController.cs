using CarLine.API.Models;
using CarLine.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarLine.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarsSearchController(
    ICarsSearchService searchService,
    ILogger<CarsSearchController> logger)
    : ControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? q = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priceClassification = null,
        [FromQuery] string? manufacturer = null,
        [FromQuery] string? model = null,
        [FromQuery] string? fuel = null,
        [FromQuery] string? transmission = null,
        [FromQuery] string? condition = null,
        [FromQuery] string? type = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] decimal? priceFrom = null,
        [FromQuery] decimal? priceTo = null,
        [FromQuery] int? minYear = null,
        [FromQuery] int? maxYear = null,
        [FromQuery] int? yearFrom = null,
        [FromQuery] int? yearTo = null,
        [FromQuery] int? odometerFrom = null,
        [FromQuery] int? odometerTo = null,
        [FromQuery] string? region = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool facets = false)
    {
        try
        {
            var result = await searchService.SearchAsync(
                q,
                status,
                priceClassification,
                manufacturer,
                model,
                fuel,
                transmission,
                condition,
                type,
                minPrice,
                maxPrice,
                priceFrom,
                priceTo,
                minYear,
                maxYear,
                yearFrom,
                yearTo,
                odometerFrom,
                odometerTo,
                region,
                page,
                pageSize,
                facets);

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error performing car search");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}
