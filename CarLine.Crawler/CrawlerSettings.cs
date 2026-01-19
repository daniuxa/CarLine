namespace CarLine.Crawler;

public class CrawlerSettings
{
    public int FetchIntervalHours { get; set; } = 24;
    public int MaxCarsPerFetch { get; set; } = 100;
    public List<ExternalApiConfig> ExternalApis { get; set; } = new();
}

public class ExternalApiConfig
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

