namespace CarLine.Common.Models;

public enum PriceClassification
{
    Unknown,
    Low,
    Normal,
    High
}

public static class PriceClassificationExtensions
{
    private static readonly Dictionary<PriceClassification, string> _storageNames = new()
    {
        [PriceClassification.Unknown] = "unknown",
        [PriceClassification.Low] = "low",
        [PriceClassification.Normal] = "normal",
        [PriceClassification.High] = "high"
    };

    public static string ToStorageString(this PriceClassification classification)
    {
        return _storageNames[classification].ToLower();
    }

    public static PriceClassification FromStorageString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return PriceClassification.Unknown;

        var key = value.Trim();
        foreach (var pair in _storageNames)
            if (string.Equals(pair.Value, key, StringComparison.OrdinalIgnoreCase))
                return pair.Key;

        return PriceClassification.Unknown;
    }

    public static bool IsAffordable(this PriceClassification classification)
    {
        return classification == PriceClassification.Low || classification == PriceClassification.Normal;
    }
}