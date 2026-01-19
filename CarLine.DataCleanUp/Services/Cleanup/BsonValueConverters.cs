using MongoDB.Bson;

namespace CarLine.DataCleanUp.Services.Cleanup;

internal static class BsonValueConverters
{
    public static string ToTrimmedString(BsonValue val)
    {
        return ToStringValue(val).Trim();
    }

    public static string ToStringValue(BsonValue val)
    {
        return val switch
        {
            BsonString s => s.AsString,
            BsonInt32 i => i.ToString(),
            BsonInt64 l => l.ToString(),
            BsonDouble d => d.ToString(),
            BsonBoolean b => b.ToString(),
            _ => val.ToString() ?? string.Empty
        };
    }
}