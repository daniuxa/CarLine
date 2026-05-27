module CarLine.DataCleanUp.FSharp.Pipeline

// ── Core orchestration ────────────────────────────────────────────────────────
//
// SOLID applied:
//   SRP  — this module only orchestrates; I/O is delegated via PipelineDeps.
//   DIP  — PipelineDeps is a record of functions, not concrete types.
//          To test: pass stub functions — no mocking frameworks needed.
//
// DRY  — ProcessingStats record replaces 5 scattered 'let mutable' bindings.
// KISS — DocOutcome DU makes the two outcomes of processDoc explicit and exhaustive.

open System
open System.Collections.Generic
open System.IO
open System.Threading
open MongoDB.Bson
open MongoDB.Driver
open CarLine.Common.Models
open Microsoft.Extensions.Logging

// ── Stats accumulator ─────────────────────────────────────────────────────────
// All counters live in one place. Updated with record-update syntax:
//   stats <- { stats with Written = stats.Written + 1L }
type ProcessingStats = {
    Read    : int64
    Written : int64
    Dropped : int64
    Errors  : int
    Inserted: int64
} with
    static member Zero = { Read=0L; Written=0L; Dropped=0L; Errors=0; Inserted=0L }

// ── Document outcome ──────────────────────────────────────────────────────────
// Explicit DU avoids boolean flags; match is exhaustive — compiler enforces handling both cases.
type private DocOutcome = Written | Dropped

// ── Dependency Inversion ──────────────────────────────────────────────────────
// The pipeline depends only on this record of functions — zero concrete types.
// DataCleanupService wires the real implementations; tests wire stubs.
type PipelineDeps = {
    Logger        : ILogger
    GetFirstDoc   : CancellationToken -> Threading.Tasks.Task<BsonDocument option>
    StreamDocs    : CancellationToken -> Threading.Tasks.Task<IAsyncCursor<BsonDocument>>
    InsertBatch   : IReadOnlyList<WriteModel<BsonDocument>> -> CancellationToken -> Threading.Tasks.Task<int64>
    IndexDocuments: IReadOnlyList<CarDocument> -> CancellationToken -> Threading.Tasks.Task<unit>
    UploadCsv     : string -> CancellationToken -> Threading.Tasks.Task<string>
    EnsureMongoIdx: CancellationToken -> Threading.Tasks.Task<unit>
    EnsureEsIdx   : CancellationToken -> Threading.Tasks.Task<unit>
}

// ── Private: flush accumulated batch to Mongo + ES ───────────────────────────
let private flushBatch deps (mongoBatch: MongoUpsertBatch) (esBatch: List<CarDocument>) ct = task {
    if mongoBatch.Count = 0 then return 0L
    else
        let! n = deps.InsertBatch mongoBatch.Items ct
        mongoBatch.Clear()
        deps.Logger.LogInformation("MongoDB insert-only: {n} new records (duplicates skipped)", n)

        if esBatch.Count > 0 then
            try   do! deps.IndexDocuments esBatch ct
            with ex -> deps.Logger.LogError(ex, "Elasticsearch batch create failed")
            esBatch.Clear()

        return n
}

// ── Private: process one BsonDocument ────────────────────────────────────────
// Returns DocOutcome DU — caller decides what to count/log.
// Internal side-effects (write CSV row, add to batch) are coherent to "handle one doc".
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

        // Build cleaned BSON for storage (only web-display fields)
        let cleanedDoc = BsonDocument()
        for key in DataCleanupConstants.webDisplayFields do
            match full.TryGetValue(key) with
            | true, v when not (String.IsNullOrWhiteSpace(v)) -> cleanedDoc[key] <- v
            | _ -> ()

        match mongoBatch.TryAdd(full) with
        | Some url ->
            // Best-effort: add to ES batch; a failure here must not drop the Mongo write
            try   esBatch.Add(BsonReader.toCarDocument cleanedDoc url)
            with ex -> logger.LogWarning(ex, "Failed to build ES document for url={url}", url)
        | None -> ()

        Written

// ── Public entry point ────────────────────────────────────────────────────────
let run (deps: PipelineDeps) (ct: CancellationToken) = task {
    let startTime = DateTime.UtcNow
    let logger    = deps.Logger
    let batchSize = 5000

    // Best-effort infrastructure checks
    try   do! deps.EnsureEsIdx ct
    with ex -> logger.LogError(ex, "Elasticsearch index check failed — proceeding without ES")
    do! deps.EnsureMongoIdx ct

    // Determine CSV columns from the schema of the first document
    let! firstDocOpt = deps.GetFirstDoc ct
    match firstDocOpt with
    | None ->
        return { CleanupResult.Success=false; Message="No source documents found"
                 RowsRead=0L; RowsWritten=0L; RowsDropped=0L
                 Errors=0; BlobName=None; Duration=DateTime.UtcNow - startTime }
    | Some firstDoc ->

    let csvHeader =
        firstDoc.Names
        |> Seq.filter (fun n -> not (DataCleanupConstants.columnsToRemoveFromTraining.Contains(n)))
        |> Seq.sort
        |> ResizeArray

    logger.LogInformation("CSV header ({count} cols): {cols}", csvHeader.Count, String.Join(", ", csvHeader))

    let mutable stats = ProcessingStats.Zero
    let tracker       = DistinctValueTracker(csvHeader)
    let mongoBatch    = MongoUpsertBatch()
    let esBatch       = List<CarDocument>()
    let tempCsvPath   = Path.Combine(Path.GetTempPath(), $"cleaned_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv")

    // ── Phase 1: stream → clean → write CSV ──────────────────────────────────
    // 'use csvWriter' disposes (closes the file handle) at the end of this inner
    // task block — before Phase 2 opens the same file for blob upload.
    do! task {
        use csvWriter = new CsvWriter(tempCsvPath)
        do! csvWriter.WriteHeaderAsync(csvHeader)

        let! cursor = deps.StreamDocs ct
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
                                    let! n = flushBatch deps mongoBatch esBatch ct
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

        // Flush any remaining records
        let! n = flushBatch deps mongoBatch esBatch ct
        stats <- { stats with Inserted = stats.Inserted + n }
        do! csvWriter.FlushAsync()
    }   // ← CsvWriter.Dispose() runs here; file handle released before upload

    logger.LogInformation(
        "Cleanup done — Read:{r} Written:{w} Dropped:{d} Errors:{e} Inserted:{i}",
        stats.Read, stats.Written, stats.Dropped, stats.Errors, stats.Inserted)

    logger.LogInformation("Distinct values per column:")
    for col, count in tracker.GetCountsOrderedByColumn() do
        logger.LogInformation("  {col}: {count} distinct", col, count)

    // ── Phase 2: upload CSV to blob storage ───────────────────────────────────
    let! blobName = deps.UploadCsv tempCsvPath ct

    try
        return {
            Success     = true
            Message     = "Cleanup completed successfully"
            RowsRead    = stats.Read
            RowsWritten = stats.Written
            RowsDropped = stats.Dropped
            Errors      = stats.Errors
            BlobName    = Some blobName
            Duration    = DateTime.UtcNow - startTime }
    finally
        if File.Exists(tempCsvPath) then File.Delete(tempCsvPath)
}

