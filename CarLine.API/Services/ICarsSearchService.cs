using CarLine.API.Models;
using CarLine.Common.Models;

namespace CarLine.API.Services;

public interface ICarsSearchService
{
    Task<CarSearchResponse> SearchAsync(
        string? q,
        string? status,
        string? priceClassification,
        string? manufacturer,
        string? model,
        string? fuel,
        string? transmission,
        string? condition,
        string? type,
        decimal? minPrice,
        decimal? maxPrice,
        decimal? priceFrom,
        decimal? priceTo,
        int? minYear,
        int? maxYear,
        int? yearFrom,
        int? yearTo,
        int? odometerFrom,
        int? odometerTo,
        string? region,
        int page,
        int pageSize,
        bool facets);
}

