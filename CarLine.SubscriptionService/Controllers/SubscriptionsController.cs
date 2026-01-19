using CarLine.Common.Models;
using CarLine.SubscriptionService.Data;
using CarLine.SubscriptionService.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarLine.SubscriptionService.Controllers;

[ApiController]
[Route("api/subscriptions")]
public sealed class SubscriptionsController(SubscriptionDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CarSubscriptionDto>> Create([FromBody] CreateCarSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        var entity = new SubscriptionEntity
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Manufacturer = request.Manufacturer,
            Model = request.Model,
            YearFrom = request.YearFrom,
            YearTo = request.YearTo,
            OdometerFrom = request.OdometerFrom,
            OdometerTo = request.OdometerTo,
            Condition = request.Condition,
            Fuel = request.Fuel,
            Transmission = request.Transmission,
            Type = request.Type,
            Region = request.Region,
            CreatedAtUtc = nowUtc,
            SinceUtc = nowUtc,
            IsActive = true
        };

        db.Subscriptions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(entity));
    }

    [HttpGet]
    public async Task<ActionResult<List<CarSubscriptionDto>>> GetByEmail([FromQuery] string email,
        CancellationToken cancellationToken)
    {
        var list = await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.Email == email && s.IsActive)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(list.Select(ToDto).ToList());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity == null) return NotFound();

        entity.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static CarSubscriptionDto ToDto(SubscriptionEntity entity) => new()
    {
        Id = entity.Id,
        Email = entity.Email,
        Manufacturer = entity.Manufacturer,
        Model = entity.Model,
        YearFrom = entity.YearFrom,
        YearTo = entity.YearTo,
        OdometerFrom = entity.OdometerFrom,
        OdometerTo = entity.OdometerTo,
        Condition = entity.Condition,
        Fuel = entity.Fuel,
        Transmission = entity.Transmission,
        Type = entity.Type,
        Region = entity.Region,
        CreatedAtUtc = entity.CreatedAtUtc,
        SinceUtc = entity.SinceUtc,
        IsActive = entity.IsActive
    };
}
