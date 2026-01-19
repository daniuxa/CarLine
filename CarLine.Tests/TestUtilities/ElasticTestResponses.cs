using Elastic.Clients.Elasticsearch.Aggregations;

namespace CarLine.Tests.TestUtilities;

internal static class ElasticTestResponses
{
    public static AggregateDictionary CreateAggregations(params (string name, IAggregate aggregate)[] items)
    {
        var dict = items.ToDictionary(x => x.name, x => x.aggregate);
        return new AggregateDictionary(dict);
    }
}
