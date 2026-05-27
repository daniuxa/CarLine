module CarLine.DataCleanUp.FSharp.Pipeline

// ── Services layer ─────────────────────────────────────────────────────────────
// Pure orchestration. Depends on Core + Domain only.
//
// KEY DESIGN: PipelineDeps contains ZERO Infrastructure types (no BsonDocument,
// no MongoDB, no ElasticsearchClient, no CsvWriter, no BlobContainerClient).
// All I/O is hidden behind plain functions — Infrastructure implements them,
// Application wires them. This is the Dependency Inversion Principle in practice.
//
// To unit-test Pipeline.run: pass stub functions. No mocking framework needed.

open System
open System.IO
open System.Threading
open Microsoft.Extensions.Logging

/// All external dependencies expressed as plain functions.
/// Services defines the SHAPE of what it needs; Infrastructure provides the implementations.
type PipelineDeps = {
    Logger         : ILogger

    /// Reads the first source document and returns the sorted, filtered CSV column list.
    /// Returns None if the source collection is empty.
    BuildCsvHeader : CancellationToken -> Threading.Tasks.Task<ResizeArray<string> option>

    /// Streams all documents, cleans them, writes CSV rows, and flushes Mongo + ES batches.
    /// Receives the column list and the path to write the CSV to.
    /// Returns the final processing statistics.
    RunDocumentLoop: ResizeArray<string> -> string -> CancellationToken -> Threading.Tasks.Task<ProcessingStats>

    /// Uploads the local CSV file to blob storage; returns the resulting blob name.
    UploadCsv      : string -> CancellationToken -> Threading.Tasks.Task<string>

    /// Infrastructure readiness checks (idempotent, best-effort).
    EnsureMongoIdx : CancellationToken -> Threading.Tasks.Task<unit>
    EnsureEsIdx    : CancellationToken -> Threading.Tasks.Task<unit>
}

/// Runs the full cleanup pipeline and returns a CleanupResult.
/// This function contains only high-level orchestration — no I/O details.
let run (deps: PipelineDeps) (ct: CancellationToken) = task {
    let startTime = DateTime.UtcNow
    let logger    = deps.Logger

    // 1. Readiness checks — best-effort, never block the run
    try   do! deps.EnsureEsIdx ct
    with ex -> logger.LogError(ex, "Elasticsearch index check failed — proceeding without ES")
    do! deps.EnsureMongoIdx ct

    // 2. Determine what columns the CSV will have
    let! headerOpt = deps.BuildCsvHeader ct
    match headerOpt with
    | None ->
        return { Success=false; Message="No source documents found"
                 RowsRead=0L; RowsWritten=0L; RowsDropped=0L
                 Errors=0; BlobName=None; Duration=DateTime.UtcNow - startTime }
    | Some csvHeader ->

    logger.LogInformation("CSV header ({n} cols): {cols}", csvHeader.Count, String.Join(", ", csvHeader))

    // 3. Stream → clean → write CSV (RunDocumentLoop owns all I/O details)
    let tempCsvPath = Path.Combine(Path.GetTempPath(), $"cleaned_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv")
    let! stats = deps.RunDocumentLoop csvHeader tempCsvPath ct

    logger.LogInformation(
        "Cleanup done — Read:{r} Written:{w} Dropped:{d} Errors:{e} Inserted:{i}",
        stats.Read, stats.Written, stats.Dropped, stats.Errors, stats.Inserted)

    // 4. Upload the finished CSV to blob storage
    let! blobName = deps.UploadCsv tempCsvPath ct

    try
        return {
            Success     = true
            Message     = "Cleanup completed successfully"
            RowsRead    = stats.Read
            RowsWritten = stats.Written
            RowsDropped = stats.Dropped
            Errors      = stats.Errors
            BlobName    = Some blobName // So Some blobName wraps the string value blobName into a string option
            Duration    = DateTime.UtcNow - startTime }
    finally
        if File.Exists(tempCsvPath) then File.Delete(tempCsvPath)
}
