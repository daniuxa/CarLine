using CarLine.SubscriptionService.Data;
using CarLine.SubscriptionService.Data.Entities;
using CarLine.SubscriptionService.Email;
using CarLine.SubscriptionService.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace CarLine.SubscriptionService.Services;

public sealed class SubscriptionProcessingService(
    SubscriptionDbContext db,
    MongoCarsRepository mongo,
    IEmailSender emailSender,
    ILogger<SubscriptionProcessingService> logger)
{
    public async Task ProcessAllAsync(CancellationToken cancellationToken)
    {
        var subs = await db.Subscriptions
            .AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var sub in subs)
        {
            try
            {
                await ProcessOneAsync(sub.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed processing subscription {SubscriptionId}", sub.Id);
            }
        }
    }

    public async Task ProcessOneAsync(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken);
        if (sub == null || !sub.IsActive) return;

        var sinceUtc = sub.SinceUtc;
        var nowUtc = DateTime.UtcNow;

        var cars = await mongo.FindNewCarsForSubscriptionAsync(
            sinceUtc,
            sub.Manufacturer,
            sub.Model,
            sub.YearFrom,
            sub.YearTo,
            sub.OdometerFrom,
            sub.OdometerTo,
            sub.Condition,
            sub.Fuel,
            sub.Transmission,
            sub.Type,
            sub.Region,
            cancellationToken);

        if (cars.Count == 0)
        {
            sub.SinceUtc = nowUtc;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        // Compute candidate ids
        var candidates = cars
            .Select(ExtractCarId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        // Filter out cars already notified for this subscription
        var already = await db.SubscriptionNotifications
            .AsNoTracking()
            .Where(n => n.SubscriptionId == sub.Id && candidates.Contains(n.CarId))
            .Select(n => n.CarId)
            .ToListAsync(cancellationToken);

        var alreadySet = already.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var detectedAt = nowUtc;
        var emailCars = new List<MatchedCarDto>();

        foreach (var doc in cars)
        {
            var carId = ExtractCarId(doc);
            if (alreadySet.Contains(carId))
                continue;

            var url = doc.TryGetValue("url", out var urlVal) ? urlVal.ToString() : null;
            var manu = doc.TryGetValue("manufacturer", out var m1) ? m1.ToString() : string.Empty;
            var model = doc.TryGetValue("model", out var m2) ? m2.ToString() : string.Empty;
            var year = doc.TryGetValue("year", out var y) && int.TryParse(y.ToString(), out var yi) ? yi : 0;
            var price = doc.TryGetValue("price", out var p) && decimal.TryParse(p.ToString(), out var pd) ? pd : (decimal?)null;
            var region = doc.TryGetValue("region", out var r) ? r.ToString() : null;

            db.SubscriptionNotifications.Add(new SubscriptionNotificationEntity
            {
                SubscriptionId = sub.Id,
                CarId = carId,
                CarUrl = url,
                DetectedAtUtc = detectedAt
            });

            emailCars.Add(new MatchedCarDto
            {
                CarId = carId,
                Url = url,
                Manufacturer = manu,
                Model = model,
                Year = year,
                Price = price,
                Region = region
            });
        }

        if (emailCars.Count == 0)
        {
            sub.SinceUtc = nowUtc;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        // Persist notification rows first
        await db.SaveChangesAsync(cancellationToken);

        // Send email; if SMTP fails, we keep SinceUtc unchanged so we can retry later,
        // but the unique constraint prevents duplicate notification rows.
        await emailSender.SendNewCarsDigestAsync(sub.Email, sub, emailCars, cancellationToken);

        sub.SinceUtc = nowUtc;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string ExtractCarId(BsonDocument doc)
    {
        if (doc.TryGetValue("_id", out var idVal) && idVal is { } value && !value.IsBsonNull)
        {
            var result = value.IsObjectId ? value.AsObjectId.ToString() : value.ToString();
            return result ?? string.Empty;
        }

        if (doc.TryGetValue("url", out var urlVal) && urlVal != null && !urlVal.IsBsonNull)
        {
            return urlVal.ToString() ?? string.Empty;
        }

        return Guid.NewGuid().ToString("N");
    }
}
