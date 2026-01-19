using CarLine.API.Models;
using CarLine.Common.Models;
using CarLine.Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarLine.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarPricePredictionController(IMlInferenceClient mlClient, ILogger<CarPricePredictionController> logger)
    : ControllerBase
{
    private readonly ILogger<CarPricePredictionController> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    ///     Estimate car price using the ML model
    /// </summary>
    [HttpPost("estimate")]
    public async Task<IActionResult> EstimatePrice([FromBody] CarPriceEstimateRequest request)
    {
        try
        {
            // Call the ML prediction endpoint
            var mlRequest = new CarPredictionRequest
            {
                Manufacturer = request.Manufacturer,
                Model = request.Model,
                Year = request.Year,
                Odometer = request.Odometer,
                Transmission = request.Transmission,
                Condition = request.Condition,
                Fuel = request.Fuel,
                Type = request.Type,
                Region = request.Region ?? ""
            };

            var result = await mlClient.PredictAsync(mlRequest, HttpContext.RequestAborted);

            if (result?.PredictedPrice == null)
                return BadRequest(new { error = "Invalid prediction response from ML service" });

            return Ok(new CarPriceEstimateResponse
            {
                EstimatedPrice = Math.Round(result.PredictedPrice.Value, 2),
                Manufacturer = request.Manufacturer,
                Model = request.Model,
                Year = request.Year,
                Odometer = request.Odometer,
                Transmission = request.Transmission,
                Condition = request.Condition,
                Fuel = request.Fuel,
                Type = request.Type,
                Region = request.Region
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to ML service");
            return StatusCode(503, new { error = "ML service unavailable", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estimating car price");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}