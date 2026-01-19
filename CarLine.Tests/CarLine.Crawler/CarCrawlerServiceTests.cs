using System.Net;
using System.Text;
using System.Text.Json;
using CarLine.Crawler;
using CarLine.Crawler.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace CarLine.Tests.CarLine.Crawler;

[TestFixture]
public class CarCrawlerServiceTests
{
    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return client;
        }
    }

    [Test]
    public async Task FetchFromExternalApisAsync_whenNoEnabledApis_doesNotCallRepository()
    {
        var logger = Mock.Of<ILogger<CarCrawlerService>>();
        var repo = new Mock<ICrawledCarsRepository>(MockBehavior.Strict);

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://example") };
        var httpFactory = new StubHttpClientFactory(httpClient);

        var settings = Options.Create(new CrawlerSettings
        {
            MaxCarsPerFetch = 10,
            ExternalApis = new List<ExternalApiConfig>
            {
                new() { Name = "api1", BaseUrl = "http://example", Enabled = false }
            }
        });

        var sut = new CarCrawlerService(logger, httpFactory, repo.Object, settings);

        await sut.FetchFromExternalApisAsync(CancellationToken.None);

        repo.VerifyNoOtherCalls();
    }

    [Test]
    public async Task FetchFromExternalApisAsync_singleApi_singlePage_callsRepositoryOnceWithListings()
    {
        var logger = Mock.Of<ILogger<CarCrawlerService>>();
        var repo = new Mock<ICrawledCarsRepository>();

        var pageSize = 10;
        var payload = CreateApiResponseJson(1, pageSize, 1);

        var handler = new StubHttpMessageHandler(req =>
        {
            Assert.That(req.RequestUri!.ToString(), Does.Contain($"/api/cars?page=1&pageSize={pageSize}"));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var httpFactory = new StubHttpClientFactory(httpClient);

        var settings = Options.Create(new CrawlerSettings
        {
            MaxCarsPerFetch = pageSize,
            ExternalApis = new List<ExternalApiConfig>
            {
                new() { Name = "api1", BaseUrl = "http://example", Enabled = true }
            }
        });

        var sut = new CarCrawlerService(logger, httpFactory, repo.Object, settings);

        await sut.FetchFromExternalApisAsync(CancellationToken.None);

        repo.Verify(r => r.UpsertManyAsync(
                It.Is<IEnumerable<ExternalCarListing>>(l => l.Count() == pageSize && l.First().Url == "u1"),
                "api1",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task FetchFromExternalApisAsync_multiplePages_stopsAtMaxCarsPerFetch()
    {
        var logger = Mock.Of<ILogger<CarCrawlerService>>();
        var repo = new Mock<ICrawledCarsRepository>();

        const int maxCars = 120;
        const int pageSize = 50; // service takes Math.Min(50, max)
        var calls = 0;

        var handler = new StubHttpMessageHandler(req =>
        {
            calls++;
            var page = calls;
            var json = page <= 3
                ? CreateApiResponseJson(page, pageSize, (page - 1) * pageSize + 1)
                : "{ \"data\":[], \"page\":4, \"pageSize\":50, \"total\":0 }";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler);
        var httpFactory = new StubHttpClientFactory(httpClient);

        var settings = Options.Create(new CrawlerSettings
        {
            MaxCarsPerFetch = maxCars,
            ExternalApis = new List<ExternalApiConfig>
            {
                new() { Name = "api1", BaseUrl = "http://example", Enabled = true }
            }
        });

        var sut = new CarCrawlerService(logger, httpFactory, repo.Object, settings);

        await sut.FetchFromExternalApisAsync(CancellationToken.None);

        repo.Verify(
            r => r.UpsertManyAsync(It.IsAny<IEnumerable<ExternalCarListing>>(), "api1", It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        Assert.That(calls, Is.EqualTo(3));
    }

    private static string CreateApiResponseJson(int page, int pageSize, int startId)
    {
        var listings = Enumerable.Range(startId, pageSize)
            .Select(i => new ExternalCarListing
            {
                Id = i.ToString(),
                Url = $"u{i}",
                Manufacturer = "Toyota",
                Model = "Corolla",
                Year = 2018,
                Price = 10000 + i,
                Odometer = 10000 + i,
                Transmission = "auto",
                Condition = "used",
                Fuel = "gas",
                Type = "sedan",
                PostingDate = DateTime.UtcNow
            })
            .ToList();

        var response = new ExternalApiResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = pageSize * 3,
            Data = listings
        };

        return JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}