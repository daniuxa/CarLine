using CarLine.Common.Models;

namespace CarLine.API.Models;

public class CarSearchResponse
{
    public long Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    public List<CarDocument> Cars { get; set; } = new();

    // Facets values are object to allow nested structures (e.g. manufacturer_models = { manufacturer: { model: count } })
    public Dictionary<string, object>? Facets { get; set; }
}