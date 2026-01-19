namespace CarLine.DataCleanUp.Models;

public static class DataCleanupConstants
{
    // Columns to remove from the CSV training dataset only (MongoDB will keep these for web display)
    public static readonly HashSet<string> ColumnsToRemoveFromTraining = new(StringComparer.OrdinalIgnoreCase)
    {
        "_id", "url", "region_url", "title_status", "vin", "image_url", "state",
        "lat", "long", "posting_date", "paint_color", "size", "county", "cylinders", "drive"
    };

    // Fields to keep in MongoDB for web display (in addition to the core fields)
    public static readonly HashSet<string> WebDisplayFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "url", "posting_date", "image_url", "region_url", "title_status", "vin", "state",
        "lat", "long", "paint_color", "size", "county", "cylinders", "drive",
        // Core fields also needed for web display
        "manufacturer", "model", "year", "price", "odometer", "region", "transmission", "condition", "fuel", "type"
    };

    // Standardize manufacturer names (only handle variations/typos)
    public static readonly Dictionary<string, string> ManufacturerMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "chevy", "chevrolet" },
        { "mercedes benz", "mercedes-benz" },
        { "mercedes", "mercedes-benz" },
        { "benz", "mercedes-benz" },
        { "vw", "volkswagen" },
        { "land rover", "land-rover" },
        { "landrover", "land-rover" },
        { "rover", "land-rover" },
        { "alfa romeo", "alfa-romeo" },
        { "alfaromeo", "alfa-romeo" },
        { "harley davidson", "harley-davidson" },
        { "harley", "harley-davidson" },
        { "aston martin", "aston-martin" },
        { "astonmartin", "aston-martin" },
        { "datsun", "nissan" }
    };

    // Valid transmission values - skip record if not found
    public static readonly HashSet<string> ValidTransmissions = new(StringComparer.OrdinalIgnoreCase)
    {
        "automatic", "manual", "other"
    };

    // Valid condition values - skip record if not found
    public static readonly HashSet<string> ValidConditions = new(StringComparer.OrdinalIgnoreCase)
    {
        "excellent", "good", "fair", "like new"
    };

    // Valid fuel values - skip record if not found
    public static readonly HashSet<string> ValidFuels = new(StringComparer.OrdinalIgnoreCase)
    {
        "gas", "diesel", "electric", "hybrid", "other"
    };

    // Valid type values - skip record if not found
    public static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "sedan", "suv", "truck", "coupe", "van", "wagon", "convertible", "hatchback", "pickup", "other"
    };
}