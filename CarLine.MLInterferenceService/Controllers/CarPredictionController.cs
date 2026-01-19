using CarLine.Common.Models;
using CarLine.MLInterferenceService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarLine.MLInterferenceService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarPredictionController(
    CarPricePredictionService predictionService,
    ILogger<CarPredictionController> logger)
    : ControllerBase
{
    [HttpPost("predict")]
    public async Task<IActionResult> Predict([FromBody] CarPredictionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Manufacturer) || string.IsNullOrWhiteSpace(request.Model) ||
            request.Year <= 0 || request.Odometer < 0)
            return BadRequest(new { error = "Invalid input. Manufacturer, Model, Year, and Odometer are required." });

        try
        {
            var prediction = await predictionService.PredictPriceAsync(request);
            return Ok(new
                { predictedPrice = Math.Round(prediction.Score, 2), input = request, timestamp = DateTime.UtcNow });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Model not available");
            return Problem(ex.Message, statusCode: 503, title: "Model Not Available");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Prediction failed");
            return Problem(ex.Message, statusCode: 500, title: "Prediction Failed");
        }
    }

    [HttpPost("predict/batch")]
    public async Task<IActionResult> PredictBatch([FromBody] List<CarPredictionRequest> requests)
    {
        if (requests.Count == 0) return BadRequest(new { error = "Request batch cannot be empty." });

        if (requests.Count > 1000) return BadRequest(new { error = "Batch size cannot exceed 1000 items." });

        try
        {
            var results = new List<object>();
            var errors = new List<object>();

            foreach (var request in requests)
                try
                {
                    if (string.IsNullOrWhiteSpace(request.Manufacturer) || string.IsNullOrWhiteSpace(request.Model) ||
                        request.Year <= 0 || request.Odometer < 0)
                    {
                        errors.Add(new
                        {
                            input = request,
                            error = "Invalid input. Manufacturer, Model, Year, and Odometer are required."
                        });
                        continue;
                    }

                    var prediction = await predictionService.PredictPriceAsync(request);
                    results.Add(new
                    {
                        predictedPrice = Math.Round(prediction.Score, 2),
                        input = request
                    });
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Prediction failed for individual car in batch");
                    errors.Add(new { input = request, error = ex.Message });
                }

            return Ok(new
            {
                predictions = results,
                errors,
                totalRequested = requests.Count,
                totalSuccessful = results.Count,
                totalFailed = errors.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Model not available");
            return Problem(ex.Message, statusCode: 503, title: "Model Not Available");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Batch prediction failed");
            return Problem(ex.Message, statusCode: 500, title: "Batch Prediction Failed");
        }
    }
}