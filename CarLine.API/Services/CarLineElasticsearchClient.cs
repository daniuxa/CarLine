using Elastic.Clients.Elasticsearch;

namespace CarLine.API.Services;

public sealed class CarLineElasticsearchClient(ElasticsearchClient inner) : ICarLineElasticsearchClient
{
    public async Task<CarLineSearchResponse<TDocument>> SearchAsync<TDocument>(
        Action<SearchRequestDescriptor<TDocument>> configureRequest,
        CancellationToken cancellationToken = default)
        where TDocument : class
    {
        var response = await inner.SearchAsync(configureRequest, cancellationToken);

        return new CarLineSearchResponse<TDocument>(
            IsValid: response.IsValidResponse,
            Total: response.Total,
            Documents: response.Documents,
            Aggregations: response.Aggregations);
    }
}
