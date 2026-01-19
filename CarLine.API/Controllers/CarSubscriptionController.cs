using CarLine.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarLine.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarSubscriptionController(IHttpClientFactory httpClientFactory, ILogger<CarSubscriptionController> logger)
    : ControllerBase
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("SubscriptionService");

    [HttpPost]
    public async Task<ActionResult<CarSubscriptionDto>> Create([FromBody] CreateCarSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("api/subscriptions", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("SubscriptionService returned {Status}: {Body}", response.StatusCode, body);
            return StatusCode((int)response.StatusCode, body);
        }

        var dto = await response.Content.ReadFromJsonAsync<CarSubscriptionDto>(cancellationToken);
        return Ok(dto);
    }

    [HttpGet]
    public async Task<ActionResult<List<CarSubscriptionDto>>> GetByEmail([FromQuery] string email,
        CancellationToken cancellationToken)
    {
        var response =
            await _httpClient.GetAsync($"api/subscriptions?email={Uri.EscapeDataString(email)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return StatusCode((int)response.StatusCode, body);
        }

        var list = await response.Content.ReadFromJsonAsync<List<CarSubscriptionDto>>(cancellationToken);
        return Ok(list ?? new List<CarSubscriptionDto>());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var response = await _httpClient.DeleteAsync($"api/subscriptions/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return StatusCode((int)response.StatusCode, body);
        }

        return NoContent();
    }
}