namespace CarLine.DataCleanUp.FSharp

open System
open System.Collections.Generic
open MongoDB.Bson
open MongoDB.Driver

/// Accumulates MongoDB InsertOneModel entries for a single bulk-write batch.
/// Returns 'string option' from TryAdd instead of C#'s 'out string?' — more idiomatic F#.
type MongoUpsertBatch() =
    let batch = List<WriteModel<BsonDocument>>()

    member _.Count = batch.Count
    member _.Items = batch :> IReadOnlyList<WriteModel<BsonDocument>>
    member _.Clear() = batch.Clear()

    /// Returns Some url if the document was queued for insertion, None if skipped.
    member _.TryAdd(fullRecord: Dictionary<string, string>) : string option =
        let doc = BsonDocument()
        for key in DataCleanupConstants.webDisplayFields do
            match fullRecord.TryGetValue(key) with
            | true, v when not (String.IsNullOrWhiteSpace(v)) -> doc[key] <- BsonString(v)
            | _ -> ()

        if doc.ElementCount = 0 then None
        else
            match fullRecord.TryGetValue("url") with
            | true, url when not (String.IsNullOrWhiteSpace(url)) ->
                doc["url"]        <- BsonString(url)
                doc["first_seen"] <- BsonDateTime(DateTime.UtcNow)
                doc["last_seen"]  <- BsonDateTime(DateTime.UtcNow)
                doc["status"]     <- BsonString("ACTIVE")
                batch.Add(InsertOneModel<BsonDocument>(doc))
                Some url
            | _ -> None

