using CarLine.ExternalCarSellerStub.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarLine.ExternalCarSellerStub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarsController(CarInventoryService inventory) : ControllerBase
{
    // GET /api/cars
    [HttpGet]
    public IActionResult GetCars(
        [FromQuery] string? manufacturer,
        [FromQuery] string? model,
        [FromQuery] int? minYear,
        [FromQuery] int? maxYear,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var cars = inventory.GetCars(manufacturer, model, minYear, maxYear, minPrice, maxPrice, page, pageSize);

        // Keep the exact response shape: { data, page, pageSize, total }
        return Ok(new
        {
            data = cars,
            page,
            pageSize,
            total = inventory.TotalCount
        });
    }

    // GET /api/cars/{id}
    [HttpGet("{id}")]
    public IActionResult GetCarById([FromRoute] string id)
    {
        var car = inventory.GetCarById(id);
        return car != null ? Ok(car) : NotFound();
    }

    // GET /api/cars/latest
    [HttpGet("latest")]
    public IActionResult GetLatestCars([FromQuery] int count = 10)
    {
        var cars = inventory.GetLatestCars(count);
        return Ok(cars);
    }
}