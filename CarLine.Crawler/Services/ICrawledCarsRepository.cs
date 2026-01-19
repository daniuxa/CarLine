namespace CarLine.Crawler.Services;

public interface ICrawledCarsRepository
{
    Task UpsertManyAsync(IEnumerable<ExternalCarListing> listings, string source, CancellationToken cancellationToken);
}