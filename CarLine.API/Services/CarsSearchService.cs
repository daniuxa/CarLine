using CarLine.API.Models;
using CarLine.Common.Models;
using CarLine.API.Queries;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;

namespace CarLine.API.Services;

public class CarsSearchService(ICarLineElasticsearchClient elasticsearchClient, ILogger<CarsSearchService> logger)
    : ICarsSearchService
{
    public async Task<CarSearchResponse> SearchAsync(
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
        bool facets)
    {
        var from = (page - 1) * pageSize;
        var buildQuery = CreateQuery.Build(
            q,
            status,
            priceClassification,
            manufacturer,
            model,
            fuel,
            transmission,
            condition,
            type,
            minPrice,
            maxPrice,
            priceFrom,
            priceTo,
            minYear,
            maxYear,
            yearFrom,
            yearTo,
            odometerFrom,
            odometerTo,
            region);

        var response = await elasticsearchClient.SearchAsync<CarDocument>(s => s
            .Indices(ElasticsearchHelper.CarsIndexName)
            .From(from)
            .Size(pageSize)
            .Query(buildQuery)
            .TrackTotalHits(true)
            .Aggregations(ad =>
            {
                if (!facets) return;

                ad.Add("status_facet", a => a.Terms(t => t.Field("status.keyword").Size(20)));
                ad.Add("manufacturer_facet", a => a.Terms(t => t.Field("manufacturer.keyword").Size(50))
                    .Aggregations(aa => aa
                        .Add("top_models", m => m.Terms(mt => mt.Field("model.keyword").Size(50)))
                    )
                );
                ad.Add("fuel_facet", a => a.Terms(t => t.Field("fuel.keyword").Size(10)));
                ad.Add("transmission_facet", a => a.Terms(t => t.Field("transmission.keyword").Size(10)));
                ad.Add("condition_facet", a => a.Terms(t => t.Field("condition.keyword").Size(10)));
                ad.Add("price_classification_facet", a => a.Terms(t => t.Field("price_classification.keyword").Size(10)));
                ad.Add("type_facet", a => a.Terms(t => t.Field("type.keyword").Size(10)));
                ad.Add("region_facet", a => a.Terms(t => t.Field("region.keyword").Size(30)));
            })
            .Sort(sd => sd.Field(f => f.LastSeen, SortOrder.Desc)));

        if (response == null || !response.IsValid)
        {
            logger.LogError("Elasticsearch search failed");
            throw new InvalidOperationException("Search failed");
        }

        var result = new CarSearchResponse
        {
            Total = response.Total,
            Page = page,
            PageSize = pageSize,
            Cars = response.Documents.ToList()
        };

        if (facets && response.Aggregations != null)
        {
            result.Facets = new Dictionary<string, object>();

            if (response.Aggregations.TryGetValue("status_facet", out var statusAgg))
            {
                var statusTerms = statusAgg as StringTermsAggregate;
                if (statusTerms?.Buckets != null)
                {
                    result.Facets["status"] = statusTerms.Buckets
                        .ToDictionary(b => b.Key.ToString(), b => (long)b.DocCount);
                }
            }

            if (response.Aggregations.TryGetValue("manufacturer_facet", out var manuAgg))
            {
                var manuTerms = manuAgg as StringTermsAggregate;
                if (manuTerms?.Buckets != null)
                {
                    result.Facets["manufacturer"] = manuTerms.Buckets
                        .ToDictionary(b => b.Key.ToString(), b => (long)b.DocCount);

                    var manufacturerModels = new Dictionary<string, Dictionary<string, long>>();
                    foreach (var b in manuTerms.Buckets)
                    {
                        var manuKey = b.Key.ToString();
                        var modelsDict = new Dictionary<string, long>();

                        if (b.Aggregations != null && b.Aggregations.TryGetValue("top_models", out var topModelsAgg))
                        {
                            var topModels = topModelsAgg as StringTermsAggregate;
                            if (topModels?.Buckets != null)
                            {
                                foreach (var mb in topModels.Buckets)
                                {
                                    modelsDict[mb.Key.ToString()] = (long)mb.DocCount;
                                }
                            }
                        }

                        manufacturerModels[manuKey] = modelsDict;
                    }

                    result.Facets["manufacturer_models"] = manufacturerModels;
                }
            }

            if (response.Aggregations.TryGetValue("fuel_facet", out var fuelAgg))
            {
                var fuelTerms = fuelAgg as StringTermsAggregate;
                if (fuelTerms?.Buckets != null)
                {
                    result.Facets["fuel"] = fuelTerms.Buckets
                        .ToDictionary(b => b.Key.ToString(), b => (long)b.DocCount);
                }
            }

            if (response.Aggregations.TryGetValue("transmission_facet", out var transAgg))
            {
                var transTerms = transAgg as StringTermsAggregate;
                if (transTerms?.Buckets != null)
                {
                    result.Facets["transmission"] = transTerms.Buckets
                        .ToDictionary(b => b.Key.ToString(), b => (long)b.DocCount);
                }
            }

            if (response.Aggregations.TryGetValue("condition_facet", out var condAgg))
            {
                var condTerms = condAgg as StringTermsAggregate;
                if (condTerms?.Buckets != null)
                {
                    result.Facets["condition"] = condTerms.Buckets
                        .ToDictionary(b => b.Key.ToString(), b => (long)b.DocCount);
                }
            }

            if (response.Aggregations.TryGetValue("price_classification_facet", out var priceClassAgg))
            {
                var priceClassTerms = priceClassAgg as StringTermsAggregate;
                if (priceClassTerms?.Buckets != null)
                {
                    result.Facets["price_classification"] = priceClassTerms.Buckets
                        .ToDictionary(b => b.Key.ToString(), b => (long)b.DocCount);
                }
            }

            if (response.Aggregations.TryGetValue("model_facet", out var modelAgg))
            {
                var modelTerms = modelAgg as StringTermsAggregate;
                if (modelTerms?.Buckets != null)
                {
                    result.Facets["model"] = modelTerms.Buckets
                        .ToDictionary(b => b.Key.ToString(), b => (long)b.DocCount);
                }
            }

            if (response.Aggregations.TryGetValue("type_facet", out var typeAgg))
            {
                var typeTerms = typeAgg as StringTermsAggregate;
                if (typeTerms?.Buckets != null)
                {
                    result.Facets["type"] = typeTerms.Buckets
                        .ToDictionary(b => b.Key.ToString(), b => (long)b.DocCount);
                }
            }

            if (response.Aggregations.TryGetValue("region_facet", out var regionAgg))
            {
                var regionTerms = regionAgg as StringTermsAggregate;
                if (regionTerms?.Buckets != null)
                {
                    result.Facets["region"] = regionTerms.Buckets
                        .ToDictionary(b => b.Key.ToString(), b => (long)b.DocCount);
                }
            }
        }

        return result;
    }
}
