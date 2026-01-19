using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;

namespace CarLine.API.Services;

/// <summary>
/// Minimal search response shape needed by CarLine services.
/// </summary>
public sealed record CarLineSearchResponse<TDocument>(
    bool IsValid,
    long Total,
    IReadOnlyCollection<TDocument> Documents,
    AggregateDictionary? Aggregations);

/// <summary>
/// Small abstraction over Elastic.Clients.Elasticsearch.ElasticsearchClient to keep application services unit-testable.
/// </summary>
public interface ICarLineElasticsearchClient
{
    Task<CarLineSearchResponse<TDocument>> SearchAsync<TDocument>(
        Action<SearchRequestDescriptor<TDocument>> configureRequest,
        CancellationToken cancellationToken = default)
        where TDocument : class;
}
