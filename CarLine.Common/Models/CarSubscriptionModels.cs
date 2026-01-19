using System.ComponentModel.DataAnnotations;

namespace CarLine.Common.Models;

public sealed class CreateCarSubscriptionRequest
{
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;

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
}

public sealed class CarSubscriptionDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;

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
    public bool IsActive { get; set; }
}