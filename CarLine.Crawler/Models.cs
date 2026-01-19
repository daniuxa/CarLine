using System.Text.Json.Serialization;

namespace CarLine.Crawler;

public class ExternalCarListing
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
    
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;
    
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;
    
    [JsonPropertyName("year")]
    public int Year { get; set; }
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("odometer")]
    public int Odometer { get; set; }
    
    [JsonPropertyName("transmission")]
    public string Transmission { get; set; } = string.Empty;
    
    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;
    
    [JsonPropertyName("fuel")]
    public string Fuel { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("region")]
    public string? Region { get; set; }
    
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
    
    [JsonPropertyName("vin")]
    public string? Vin { get; set; }
    
    [JsonPropertyName("paint_color")]
    public string? PaintColor { get; set; }
    
    [JsonPropertyName("posting_date")]
    public DateTime PostingDate { get; set; }
}

public class ExternalApiResponse
{
    [JsonPropertyName("data")]
    public List<ExternalCarListing> Data { get; set; } = new();
    
    [JsonPropertyName("page")]
    public int Page { get; set; }
    
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
    
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

