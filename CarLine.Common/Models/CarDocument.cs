using System.Text.Json.Serialization;

namespace CarLine.Common.Models;

public class CarDocument
{
    // Do NOT serialize this into the document body: Elasticsearch _id is a metadata field.
    // Keep the property for use as the document Id when calling the Bulk API (metadata .Id(...)).
    [JsonIgnore]
    public string Id { get; set; } = string.Empty;
    
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Status { get; set; } = string.Empty; // ACTIVE, INACTIVE
    public decimal Price { get; set; }
    public int Odometer { get; set; }
    public string Transmission { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Fuel { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? Url { get; set; }
    
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
    
    public string? Vin { get; set; }
    
    [JsonPropertyName("paint_color")]
    public string? PaintColor { get; set; }
    
    [JsonPropertyName("posting_date")]
    public DateTime? PostingDate { get; set; }
    
    [JsonPropertyName("first_seen")]
    public DateTime FirstSeen { get; set; }
    
    [JsonPropertyName("last_seen")]
    public DateTime LastSeen { get; set; }
    
    // Price classification fields - provide defaults so serialized documents include these fields by default
    [JsonPropertyName("price_classification")]
    public string PriceClassification { get; set; } = "unknown"; // low, normal, high, unknown
    
    [JsonPropertyName("predicted_price")]
    public decimal PredictedPrice { get; set; } = 0m;
    
    [JsonPropertyName("price_difference_percent")]
    public decimal PriceDifferencePercent { get; set; } = 0m;
    
    [JsonPropertyName("classification_date")]
    public DateTime ClassificationDate { get; set; } = DateTime.MinValue;
}
