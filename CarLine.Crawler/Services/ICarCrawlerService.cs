namespace CarLine.Crawler.Services;

public interface ICarCrawlerService
{
    Task FetchFromExternalApisAsync(CancellationToken cancellationToken);
}