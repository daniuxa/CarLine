using MongoDB.Bson;

namespace CarLine.DataCleanUp.Services.Cleanup;

internal static class RecordExtractor
{
    public static Dictionary<string, string> ExtractAllFields(BsonDocument doc)
    {
        var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fieldName in doc.Names)
        {
            // Mongo internal id isn't useful for our output and breaks stable-key logic.
            if (fieldName.Equals("_id", StringComparison.OrdinalIgnoreCase))
                continue;

            if (doc.TryGetValue(fieldName, out var val) && !val.IsBsonNull)
                record[fieldName] = BsonValueConverters.ToTrimmedString(val);
            else
                record[fieldName] = string.Empty;
        }

        return record;
    }

    public static Dictionary<string, string> ExtractCsvFields(BsonDocument doc, IReadOnlyList<string> csvHeader)
    {
        var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var col in csvHeader)
            if (doc.TryGetValue(col, out var val) && !val.IsBsonNull)
                record[col] = BsonValueConverters.ToTrimmedString(val);
            else
                record[col] = string.Empty;

        return record;
    }
}