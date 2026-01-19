using CarLine.API.Services;
using CarLine.Common.Models;
using CarLine.Tests.TestUtilities;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CarLine.Tests.CarLine.API;

[TestFixture]
public class CarsSearchServiceTests
{
    private Mock<ICarLineElasticsearchClient> _es = null!;
    private Mock<ILogger<CarsSearchService>> _logger = null!;
    private CarsSearchService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _es = new Mock<ICarLineElasticsearchClient>();
        _logger = new Mock<ILogger<CarsSearchService>>();
        _sut = new CarsSearchService(_es.Object, _logger.Object);
    }

    [Test]
    public async Task SearchAsync_facetsFalse_returnsCarsAndPaging_withoutFacets()
    {
        var docs = new List<CarDocument>
        {
            new() { Manufacturer = "Toyota", Model = "Corolla", Year = 2018, Status = "ACTIVE", Price = 12000, LastSeen = DateTime.UtcNow },
            new() { Manufacturer = "Honda", Model = "Civic", Year = 2019, Status = "ACTIVE", Price = 13000, LastSeen = DateTime.UtcNow }
        };

        var response = new CarLineSearchResponse<CarDocument>(
            IsValid: true,
            Total: 123,
            Documents: docs,
            Aggregations: null);

        _es
            .Setup(x => x.SearchAsync<CarDocument>(It.IsAny<Action<SearchRequestDescriptor<CarDocument>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.SearchAsync(
            q: null,
            status: null,
            priceClassification: null,
            manufacturer: null,
            model: null,
            fuel: null,
            transmission: null,
            condition: null,
            type: null,
            minPrice: null,
            maxPrice: null,
            priceFrom: null,
            priceTo: null,
            minYear: null,
            maxYear: null,
            yearFrom: null,
            yearTo: null,
            odometerFrom: null,
            odometerTo: null,
            region: null,
            page: 2,
            pageSize: 10,
            facets: false);

        Assert.That(result.Total, Is.EqualTo(123));
        Assert.That(result.Page, Is.EqualTo(2));
        Assert.That(result.PageSize, Is.EqualTo(10));
        Assert.That(result.Cars, Has.Count.EqualTo(2));
        Assert.That(result.Facets, Is.Null);

        _es.Verify(
            x => x.SearchAsync<CarDocument>(It.IsAny<Action<SearchRequestDescriptor<CarDocument>>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SearchAsync_facetsTrue_withAggregations_buildsFacetsDictionary()
    {
        var docs = new List<CarDocument>
        {
            new() { Manufacturer = "Toyota", Model = "Corolla", Year = 2018, Status = "ACTIVE", Price = 12000, LastSeen = DateTime.UtcNow }
        };

        var aggregations = ElasticTestResponses.CreateAggregations(
            ("status_facet", TermsAgg("ACTIVE", 10, "INACTIVE", 4)),
            ("fuel_facet", TermsAgg("gas", 7, "diesel", 2)),
            ("transmission_facet", TermsAgg("automatic", 9)),
            ("condition_facet", TermsAgg("used", 11)),
            ("price_classification_facet", TermsAgg("normal", 8, "high", 1)),
            ("type_facet", TermsAgg("sedan", 3, "suv", 5)),
            ("region_facet", TermsAgg("CA", 6))
        );

        var response = new CarLineSearchResponse<CarDocument>(
            IsValid: true,
            Total: 1,
            Documents: docs,
            Aggregations: aggregations);

        _es
            .Setup(x => x.SearchAsync<CarDocument>(It.IsAny<Action<SearchRequestDescriptor<CarDocument>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.SearchAsync(
            q: null,
            status: null,
            priceClassification: null,
            manufacturer: null,
            model: null,
            fuel: null,
            transmission: null,
            condition: null,
            type: null,
            minPrice: null,
            maxPrice: null,
            priceFrom: null,
            priceTo: null,
            minYear: null,
            maxYear: null,
            yearFrom: null,
            yearTo: null,
            odometerFrom: null,
            odometerTo: null,
            region: null,
            page: 1,
            pageSize: 10,
            facets: true);

        Assert.That(result.Facets, Is.Not.Null);

        var facets = result.Facets!;
        Assert.That(((Dictionary<string, long>)facets["status"])["ACTIVE"], Is.EqualTo(10));
        Assert.That(((Dictionary<string, long>)facets["status"])["INACTIVE"], Is.EqualTo(4));
        Assert.That(((Dictionary<string, long>)facets["fuel"])["gas"], Is.EqualTo(7));
        Assert.That(((Dictionary<string, long>)facets["transmission"])["automatic"], Is.EqualTo(9));
        Assert.That(((Dictionary<string, long>)facets["condition"])["used"], Is.EqualTo(11));
        Assert.That(((Dictionary<string, long>)facets["price_classification"])["normal"], Is.EqualTo(8));
        Assert.That(((Dictionary<string, long>)facets["type"])["suv"], Is.EqualTo(5));
        Assert.That(((Dictionary<string, long>)facets["region"])["CA"], Is.EqualTo(6));
    }

    [Test]
    public async Task SearchAsync_facetsTrue_manufacturerFacet_includesNestedManufacturerModels()
    {
        var docs = new List<CarDocument>
        {
            new() { Manufacturer = "Toyota", Model = "Corolla", Year = 2018, Status = "ACTIVE", Price = 12000, LastSeen = DateTime.UtcNow }
        };

        var toyotaBucket = new StringTermsBucket
        {
            Key = "Toyota",
            DocCount = 10,
            Aggregations = ElasticTestResponses.CreateAggregations(
                ("top_models", TermsAgg("Corolla", 7, "Camry", 3)))
        };

        var hondaBucket = new StringTermsBucket
        {
            Key = "Honda",
            DocCount = 5,
        };

        var manufacturerAgg = new StringTermsAggregate
        {
            Buckets = new List<StringTermsBucket> { toyotaBucket, hondaBucket }
        };

        var aggregations = ElasticTestResponses.CreateAggregations(
            ("manufacturer_facet", manufacturerAgg));

        var response = new CarLineSearchResponse<CarDocument>(
            IsValid: true,
            Total: 1,
            Documents: docs,
            Aggregations: aggregations);

        _es
            .Setup(x => x.SearchAsync<CarDocument>(It.IsAny<Action<SearchRequestDescriptor<CarDocument>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _sut.SearchAsync(
            q: null,
            status: null,
            priceClassification: null,
            manufacturer: null,
            model: null,
            fuel: null,
            transmission: null,
            condition: null,
            type: null,
            minPrice: null,
            maxPrice: null,
            priceFrom: null,
            priceTo: null,
            minYear: null,
            maxYear: null,
            yearFrom: null,
            yearTo: null,
            odometerFrom: null,
            odometerTo: null,
            region: null,
            page: 1,
            pageSize: 10,
            facets: true);

        Assert.That(result.Facets, Is.Not.Null);

        var facets = result.Facets!;
        var manufacturerCounts = (Dictionary<string, long>)facets["manufacturer"];
        Assert.That(manufacturerCounts["Toyota"], Is.EqualTo(10));
        Assert.That(manufacturerCounts["Honda"], Is.EqualTo(5));

        var manufacturerModels = (Dictionary<string, Dictionary<string, long>>)facets["manufacturer_models"];
        Assert.That(manufacturerModels["Toyota"]["Corolla"], Is.EqualTo(7));
        Assert.That(manufacturerModels["Toyota"]["Camry"], Is.EqualTo(3));
        Assert.That(manufacturerModels["Honda"], Is.Empty);
    }

    [Test]
    public void SearchAsync_invalidResponse_throwsInvalidOperationException()
    {
        var response = new CarLineSearchResponse<CarDocument>(
            IsValid: false,
            Total: 0,
            Documents: Array.Empty<CarDocument>(),
            Aggregations: null);

        _es
            .Setup(x => x.SearchAsync<CarDocument>(It.IsAny<Action<SearchRequestDescriptor<CarDocument>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        Assert.That(
            async () => await _sut.SearchAsync(
                q: null,
                status: null,
                priceClassification: null,
                manufacturer: null,
                model: null,
                fuel: null,
                transmission: null,
                condition: null,
                type: null,
                minPrice: null,
                maxPrice: null,
                priceFrom: null,
                priceTo: null,
                minYear: null,
                maxYear: null,
                yearFrom: null,
                yearTo: null,
                odometerFrom: null,
                odometerTo: null,
                region: null,
                page: 1,
                pageSize: 10,
                facets: false),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Search failed"));
    }

    private static StringTermsAggregate TermsAgg(params object[] keyCountPairs)
    {
        if (keyCountPairs.Length % 2 != 0)
            throw new ArgumentException("Provide key/docCount pairs", nameof(keyCountPairs));

        var buckets = new List<StringTermsBucket>();
        for (var i = 0; i < keyCountPairs.Length; i += 2)
        {
            var key = keyCountPairs[i]?.ToString() ?? string.Empty;
            var count = Convert.ToInt64(keyCountPairs[i + 1]);
            buckets.Add(new StringTermsBucket { Key = key, DocCount = count });
        }

        return new StringTermsAggregate { Buckets = buckets };
    }
}