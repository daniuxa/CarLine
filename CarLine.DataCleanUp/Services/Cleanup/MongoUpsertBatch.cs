using CarLine.DataCleanUp.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CarLine.DataCleanUp.Services.Cleanup;

internal sealed class MongoUpsertBatch
{
    private readonly List<WriteModel<BsonDocument>> _batch = new();

    public int Count => _batch.Count;
    public IReadOnlyList<WriteModel<BsonDocument>> Items => _batch;

    public void Clear()
    {
        _batch.Clear();
    }

    public bool TryAddFromFullRecord(Dictionary<string, string> fullRecord, out string? url)
    {
        url = null;

        // Prepare MongoDB document (includes web display fields + core fields)
        var cleanedDoc = new BsonDocument();
        foreach (var key in DataCleanupConstants.WebDisplayFields)
            if (fullRecord.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                cleanedDoc[key] = val;

        if (cleanedDoc.ElementCount == 0)
            return false;

        if (!fullRecord.TryGetValue("url", out url) || string.IsNullOrWhiteSpace(url))
            return false;

        // Insert-only: if url exists, Mongo unique index will reject it (E11000) and we'll ignore duplicates.
        cleanedDoc["url"] = url;
        cleanedDoc["first_seen"] = DateTime.UtcNow;
        cleanedDoc["last_seen"] = DateTime.UtcNow;
        cleanedDoc["status"] = "ACTIVE";

        _batch.Add(new InsertOneModel<BsonDocument>(cleanedDoc));
        return true;
    }
}