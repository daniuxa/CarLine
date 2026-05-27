module CarLine.DataCleanUp.FSharp.StreamProcessor

// ── Infrastructure layer ───────────────────────────────────────────────────────
// Implements Pipeline.PipelineDeps.RunDocumentLoop.
//
// This is the only place that knows about BsonDocument, CsvWriter, MongoUpsertBatch,
// DistinctValueTracker, BsonReader, RecordExtractor — all I/O and data-mapping details
// are contained here, invisible to the Services layer above.

open System
open System.Collections.Generic
open System.Threading
open MongoDB.Bson
open MongoDB.Driver
open CarLine.Common.Models
open Microsoft.Extensions.Logging

// ── Private: process one document ─────────────────────────────────────────────
// Returns DocOutcome so the caller can count Written vs Dropped without knowing
// how the decision was made.
let private handleDoc
    (csvHeader : ResizeArray<string>)
    (tracker   : DistinctValueTracker)
    (csvWriter : CsvWriter)
    (mongoBatch: MongoUpsertBatch)
    (esBatch   : List<CarDocument>)
    (logger    : ILogger)
    (doc       : BsonDocument) : DocOutcome =

    let full = RecordExtractor.extractAllFields doc
    let csv  = RecordExtractor.extractCsvFields doc csvHeader

    if not (RecordCleaner.cleanAndValidate csv full) then Dropped
    else
        tracker.TrackRow(csv)
        csvWriter.WriteRow(csvHeader, csv)

        // Build cleaned BSON for storage (web-display fields only)
        let cleanedDoc = BsonDocument()
        for key in DataCleanupConstants.webDisplayFields do
            match full.TryGetValue(key) with
            | true, v when not (String.IsNullOrWhiteSpace(v)) -> cleanedDoc[key] <- v
            | _ -> ()

        match mongoBatch.TryAdd(full) with
        | Some url ->
            try   esBatch.Add(BsonReader.toCarDocument cleanedDoc url)
            with ex -> logger.LogWarning(ex, "Failed to build ES document for url={url}", url)
        | None -> ()

        Written

// ── Private: flush accumulated batch to Mongo + ES ────────────────────────────
let private flushBatch
    (insertBatch   : IReadOnlyList<WriteModel<BsonDocument>> -> CancellationToken -> Threading.Tasks.Task<int64>)
    (indexDocuments: IReadOnlyList<CarDocument> -> CancellationToken -> Threading.Tasks.Task<unit>)
    (logger        : ILogger)
    (mongoBatch    : MongoUpsertBatch)
    (esBatch       : List<CarDocument>)
    (ct            : CancellationToken) = task {
    if mongoBatch.Count = 0 then return 0L
    else
        let! n = insertBatch mongoBatch.Items ct
        mongoBatch.Clear()
        logger.LogInformation("MongoDB insert-only: {n} new records (duplicates skipped)", n)

        if esBatch.Count > 0 then
            try   do! indexDocuments esBatch ct
            with ex -> logger.LogError(ex, "Elasticsearch batch create failed")
            esBatch.Clear()

        return n
}

// ── Public: the function injected into PipelineDeps.RunDocumentLoop ───────────
// Partially apply the first three args in DataCleanupService to produce
// the exact signature Pipeline expects:  ResizeArray<string> -> string -> CancellationToken -> Task<ProcessingStats>
let runLoop
    (insertBatch   : IReadOnlyList<WriteModel<BsonDocument>> -> CancellationToken -> Threading.Tasks.Task<int64>)
    (indexDocuments: IReadOnlyList<CarDocument> -> CancellationToken -> Threading.Tasks.Task<unit>)
    (streamAll     : CancellationToken -> Threading.Tasks.Task<IAsyncCursor<BsonDocument>>)
    (logger        : ILogger)
    (csvHeader     : ResizeArray<string>)
    (tempCsvPath   : string)
    (ct            : CancellationToken) : Threading.Tasks.Task<ProcessingStats> = task {

    let mutable stats = ProcessingStats.Zero
    let tracker       = DistinctValueTracker(csvHeader)
    let mongoBatch    = MongoUpsertBatch()
    let esBatch       = List<CarDocument>()
    let batchSize     = 5000

    // 'use' calls IDisposable.Dispose() at end of scope — file handle released before upload
    use csvWriter = new CsvWriter(tempCsvPath)
    do! csvWriter.WriteHeaderAsync(csvHeader)

    let! cursor = streamAll ct
    try
        let mutable loop = true
        while loop do
            let! hasNext = cursor.MoveNextAsync(ct)
            loop <- hasNext
            if hasNext then
                for doc in cursor.Current do
                    stats <- { stats with Read = stats.Read + 1L }
                    try
                        match handleDoc csvHeader tracker csvWriter mongoBatch esBatch logger doc with
                        | Written ->
                            stats <- { stats with Written = stats.Written + 1L }
                            if mongoBatch.Count >= batchSize then
                                let! n = flushBatch insertBatch indexDocuments logger mongoBatch esBatch ct
                                stats <- { stats with Inserted = stats.Inserted + n }
                            if stats.Written % 5000L = 0L then
                                logger.LogInformation("Processed {n} rows...", stats.Written)
                        | Dropped ->
                            stats <- { stats with Dropped = stats.Dropped + 1L }
                    with ex ->
                        stats <- { stats with Errors = stats.Errors + 1 }
                        logger.LogError(ex, "Error on row {n}", stats.Read)
    finally
        cursor.Dispose()

    // Final batch flush
    let! n = flushBatch insertBatch indexDocuments logger mongoBatch esBatch ct
    stats <- { stats with Inserted = stats.Inserted + n }
    do! csvWriter.FlushAsync()

    logger.LogInformation("Distinct values per column:")
    for col, count in tracker.GetCountsOrderedByColumn() do
        logger.LogInformation("  {col}: {count} distinct", col, count)

    return stats
}

