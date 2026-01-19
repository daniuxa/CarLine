using System.ComponentModel.DataAnnotations;

namespace CarLine.SubscriptionService.Data.Entities;

public sealed class SubscriptionNotificationEntity
{
    [Key] public long Id { get; set; }

    public Guid SubscriptionId { get; set; }
    public SubscriptionEntity? Subscription { get; set; }

    [MaxLength(256)] public string CarId { get; set; } = string.Empty;

    [MaxLength(2048)] public string? CarUrl { get; set; }

    public DateTime DetectedAtUtc { get; set; }
}