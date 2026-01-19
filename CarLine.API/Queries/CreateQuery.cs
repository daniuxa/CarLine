using Elastic.Clients.Elasticsearch.QueryDsl;
using CarLine.Common.Models;
using Elastic.Clients.Elasticsearch;

namespace CarLine.API.Queries;

public static class CreateQuery
{
    public static Action<QueryDescriptor<CarDocument>> Build(
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
        string? region)
    {
        return qd =>
        {
            var mustQueries = new List<Action<QueryDescriptor<CarDocument>>>();

            if (!string.IsNullOrWhiteSpace(q))
            {
                mustQueries.Add(mq => mq.MultiMatch(mm => mm
                    .Fields(new[] { "manufacturer^2", "model^2", "region" })
                    .Query(q)
                    .Fuzziness(new Fuzziness("AUTO"))
                ));
            }

            if (!string.IsNullOrWhiteSpace(model))
            {
                mustQueries.Add(mq => mq.Term(t => t.Field("model.keyword").Value(model.ToLowerInvariant())));
            }

            if (string.IsNullOrWhiteSpace(model) && !string.IsNullOrWhiteSpace(q) && !q.Contains(" ") && !string.IsNullOrWhiteSpace(manufacturer))
            {
                mustQueries.Add(mq => mq.Term(t => t.Field("model.keyword").Value(q.ToLowerInvariant())));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                mustQueries.Add(mq => mq.Term(t => t.Field("status.keyword").Value(status)));
            }

            if (!string.IsNullOrWhiteSpace(priceClassification))
            {
                mustQueries.Add(mq => mq.Term(t => t.Field("price_classification.keyword").Value(priceClassification)));
            }

            if (!string.IsNullOrWhiteSpace(manufacturer))
            {
                mustQueries.Add(mq => mq.Term(t => t.Field("manufacturer.keyword").Value(manufacturer.ToLowerInvariant())));
            }

            if (!string.IsNullOrWhiteSpace(region))
            {
                mustQueries.Add(mq => mq.Term(t => t.Field("region.keyword").Value(region.ToLowerInvariant())));
            }

            if (!string.IsNullOrWhiteSpace(fuel))
            {
                mustQueries.Add(mq => mq.Term(t => t.Field("fuel.keyword").Value(fuel.ToLowerInvariant())));
            }

            if (!string.IsNullOrWhiteSpace(transmission))
            {
                mustQueries.Add(mq => mq.Term(t => t.Field("transmission.keyword").Value(transmission.ToLowerInvariant())));
            }

            if (!string.IsNullOrWhiteSpace(condition))
            {
                mustQueries.Add(mq => mq.Term(t => t.Field("condition.keyword").Value(condition.ToLowerInvariant())));
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                mustQueries.Add(mq => mq.Term(t => t.Field("type.keyword").Value(type.ToLowerInvariant())));
            }

            var effectiveMinPrice = minPrice ?? priceFrom;
            var effectiveMaxPrice = maxPrice ?? priceTo;
            if (effectiveMinPrice.HasValue || effectiveMaxPrice.HasValue)
            {
                mustQueries.Add(mq => mq.Range(r => r.Number(nr =>
                {
                    nr.Field(f => f.Price);
                    if (effectiveMinPrice.HasValue)
                    {
                        nr.Gte((double)effectiveMinPrice.Value);
                    }
                    if (effectiveMaxPrice.HasValue)
                    {
                        nr.Lte((double)effectiveMaxPrice.Value);
                    }
                })));
            }

            var effectiveMinYear = minYear ?? yearFrom;
            var effectiveMaxYear = maxYear ?? yearTo;
            if (effectiveMinYear.HasValue || effectiveMaxYear.HasValue)
            {
                mustQueries.Add(mq => mq.Range(r => r.Number(nr =>
                {
                    nr.Field(f => f.Year);
                    if (effectiveMinYear.HasValue)
                    {
                        nr.Gte(effectiveMinYear.Value);
                    }
                    if (effectiveMaxYear.HasValue)
                    {
                        nr.Lte(effectiveMaxYear.Value);
                    }
                })));
            }

            if (odometerFrom.HasValue || odometerTo.HasValue)
            {
                mustQueries.Add(mq => mq.Range(r => r.Number(nr =>
                {
                    nr.Field(f => f.Odometer);
                    if (odometerFrom.HasValue)
                    {
                        nr.Gte(odometerFrom.Value);
                    }
                    if (odometerTo.HasValue)
                    {
                        nr.Lte(odometerTo.Value);
                    }
                })));
            }

            if (mustQueries.Count == 0)
            {
                qd.MatchAll();
            }
            else
            {
                qd.Bool(b => b.Must(mustQueries.ToArray()));
            }
        };
    }
}

