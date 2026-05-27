module CarLine.DataCleanUp.FSharp.ElasticsearchIndexer

// ── SRP: all Elasticsearch operations live here, as plain functions ───────────

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open Elastic.Clients.Elasticsearch
open CarLine.Common.Models
open Microsoft.Extensions.Logging

// Wrap in task {} to return Task<unit> instead of the non-generic Task from C# helper
let ensureIndex (client: ElasticsearchClient) (ct: CancellationToken) = task {
    do! ElasticsearchHelper.EnsureIndexExistsAsync(client, ct)
}

/// Creates documents that don't yet exist (409 conflict = already indexed, silently skipped).
/// Uses a SemaphoreSlim to bound concurrency to 16 parallel requests.
let createOnly (client: ElasticsearchClient) (logger: ILogger) (documents: IReadOnlyList<CarDocument>) (ct: CancellationToken) = task {
    if documents.Count > 0 then
        let mutable created       = 0
        let mutable alreadyExists = 0
        let mutable failed        = 0
        use throttler = new System.Threading.SemaphoreSlim(16)

        let tasks =
            documents
            |> Seq.map (fun doc ->
                task {
                    do! throttler.WaitAsync(ct)
                    // F# requires nested try/with and try/finally — they cannot be combined
                    try
                        try
                            let! response =
                                client.CreateAsync(doc, (fun (c: CreateRequestDescriptor<CarDocument>) ->
                                    c.Index(ElasticsearchHelper.CarsIndexName).Id(doc.Id) |> ignore), ct)
                            if response.IsValidResponse then
                                System.Threading.Interlocked.Increment(&created) |> ignore
                            elif response.ElasticsearchServerError <> null &&
                                 response.ElasticsearchServerError.Status = 409 then
                                System.Threading.Interlocked.Increment(&alreadyExists) |> ignore
                            else
                                System.Threading.Interlocked.Increment(&failed) |> ignore
                                logger.LogError("ES create failed for {id}: {info}", doc.Id, response.DebugInformation)
                        with ex ->
                            System.Threading.Interlocked.Increment(&failed) |> ignore
                            logger.LogError(ex, "ES create threw for {id}", doc.Id)
                    finally
                        throttler.Release() |> ignore
                } :> Task)
            |> Seq.toArray

        do! Task.WhenAll(tasks)
        logger.LogInformation(
            "ES create-only: {c} created, {e} already existed, {f} failed",
            created, alreadyExists, failed)
}



