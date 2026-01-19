using System.ComponentModel.DataAnnotations;

namespace CarLine.SubscriptionService.Data.Entities;

public sealed class SubscriptionEntity
{
    [Key] public Guid Id { get; set; }

    [MaxLength(320)] public string Email { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }
    public string? Model { get; set; }

    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }

    public int? OdometerFrom { get; set; }
    public int? OdometerTo { get; set; }

    public string? Condition { get; set; }
    public string? Fuel { get; set; }
    public string? Transmission { get; set; }
    public string? Type { get; set; }
    public string? Region { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime SinceUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public List<SubscriptionNotificationEntity> Notifications { get; set; } = new();
}