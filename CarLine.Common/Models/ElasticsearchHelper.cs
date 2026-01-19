using Elastic.Clients.Elasticsearch;
using MongoDB.Bson;

namespace CarLine.Common.Models;

public static class ElasticsearchHelper
{
    public const string CarsIndexName = "cars";

    public static async Task EnsureIndexExistsAsync(ElasticsearchClient client, CancellationToken cancellationToken = default)
    {
        var existsResponse = await client.Indices.ExistsAsync(CarsIndexName, cancellationToken);
        
        if (!existsResponse.Exists)
        {
            var createResponse = await client.Indices.CreateAsync<CarDocument>(CarsIndexName, c => c
                .Mappings(m => m
                    .Properties(p => p
                        .Keyword(k => k.Id)
                        .Text(t => t.Manufacturer, t => t.Fields(f => f.Keyword("keyword")))
                        .Text(t => t.Model, t => t.Fields(f => f.Keyword("keyword")))
                        .IntegerNumber(i => i.Year)
                        .Keyword(k => k.Status)
                        .FloatNumber(n => n.Price)
                        .IntegerNumber(i => i.Odometer)
                        .Keyword(k => k.Transmission)
                        .Keyword(k => k.Condition)
                        .Keyword(k => k.Fuel)
                        .Keyword(k => k.Type)
                        .Text(t => t.Region, t => t.Fields(f => f.Keyword("keyword")))
                        .Keyword(k => k.Url)
                        .Keyword(k => k.ImageUrl)
                        .Keyword(k => k.Vin)
                        .Keyword(k => k.PaintColor)
                        .Date(d => d.PostingDate)
                        .Date(d => d.FirstSeen)
                        .Date(d => d.LastSeen)
                        .Keyword(k => k.PriceClassification)
                        .FloatNumber(n => n.PredictedPrice)
                        .FloatNumber(n => n.PriceDifferencePercent)
                        .Date(d => d.ClassificationDate)
                    )
                ), cancellationToken);

            if (!createResponse.IsValidResponse)
            {
                throw new Exception($"Failed to create index: {createResponse.ElasticsearchServerError?.Error?.Reason}");
            }
        }
    }

    public static CarDocument BsonDocumentToCarDocument(BsonDocument doc)
    {
        return new CarDocument
        {
            Id = doc.Contains("_id") ? doc["_id"].ToString() : string.Empty,
            Manufacturer = GetStringValue(doc, "manufacturer"),
            Model = GetStringValue(doc, "model"),
            Year = GetIntValue(doc, "year"),
            Status = GetStringValue(doc, "status"),
            Price = GetDecimalValue(doc, "price"),
            Odometer = GetIntValue(doc, "odometer"),
            Transmission = GetStringValue(doc, "transmission"),
            Condition = GetStringValue(doc, "condition"),
            Fuel = GetStringValue(doc, "fuel"),
            Type = GetStringValue(doc, "type"),
            Region = GetStringValueOrNull(doc, "region"),
            Url = GetStringValueOrNull(doc, "url"),
            ImageUrl = GetStringValueOrNull(doc, "image_url"),
            Vin = GetStringValueOrNull(doc, "vin"),
            PaintColor = GetStringValueOrNull(doc, "paint_color"),
            PostingDate = GetDateTimeValueOrNull(doc, "posting_date"),
            FirstSeen = GetDateTimeValue(doc, "first_seen"),
            LastSeen = GetDateTimeValue(doc, "last_seen"),
            // Coalesce nullable helper results to CarDocument non-nullable defaults
            PriceClassification = GetStringValueOrNull(doc, "price_classification") ?? "unknown",
            PredictedPrice = GetDecimalValueOrNull(doc, "predicted_price") ?? 0m,
            PriceDifferencePercent = GetDecimalValueOrNull(doc, "price_difference_percent") ?? 0m,
            ClassificationDate = GetDateTimeValueOrNull(doc, "classification_date") ?? DateTime.MinValue
        };
    }

    private static string GetStringValue(BsonDocument doc, string fieldName)
    {
        if (doc.Contains(fieldName) && !doc[fieldName].IsBsonNull)
        {
            return doc[fieldName].ToString() ?? string.Empty;
        }
        return string.Empty;
    }

    private static string? GetStringValueOrNull(BsonDocument doc, string fieldName)
    {
        if (doc.Contains(fieldName) && !doc[fieldName].IsBsonNull)
        {
            var value = doc[fieldName].ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        return null;
    }

    private static int GetIntValue(BsonDocument doc, string fieldName)
    {
        if (doc.Contains(fieldName) && !doc[fieldName].IsBsonNull)
        {
            var value = doc[fieldName];
            if (value.IsString && int.TryParse(value.AsString, out var result))
                return result;
            if (value.IsNumeric)
                return value.ToInt32();
        }
        return 0;
    }

    private static decimal GetDecimalValue(BsonDocument doc, string fieldName)
    {
        if (doc.Contains(fieldName) && !doc[fieldName].IsBsonNull)
        {
            var value = doc[fieldName];
            if (value.IsString && decimal.TryParse(value.AsString, out var result))
                return result;
            if (value.IsNumeric)
                return value.ToDecimal();
        }
        return 0;
    }

    private static decimal? GetDecimalValueOrNull(BsonDocument doc, string fieldName)
    {
        if (doc.Contains(fieldName) && !doc[fieldName].IsBsonNull)
        {
            var value = doc[fieldName];
            if (value.IsString && decimal.TryParse(value.AsString, out var result))
                return result;
            if (value.IsNumeric)
                return value.ToDecimal();
        }
        return null;
    }

    private static DateTime GetDateTimeValue(BsonDocument doc, string fieldName)
    {
        if (doc.Contains(fieldName) && !doc[fieldName].IsBsonNull)
        {
            var value = doc[fieldName];
            if (value.IsValidDateTime)
                return value.ToUniversalTime();
            if (value.IsString && DateTime.TryParse(value.AsString, out var result))
                return result.ToUniversalTime();
        }
        return DateTime.UtcNow;
    }

    private static DateTime? GetDateTimeValueOrNull(BsonDocument doc, string fieldName)
    {
        if (doc.Contains(fieldName) && !doc[fieldName].IsBsonNull)
        {
            var value = doc[fieldName];
            if (value.IsValidDateTime)
                return value.ToUniversalTime();
            if (value.IsString && DateTime.TryParse(value.AsString, out var result))
                return result.ToUniversalTime();
        }
        return null;
    }
}

