namespace CarLine.DataCleanUp.FSharp

// ── Composition root for DI → Pipeline wiring ─────────────────────────────────
//
// This class is intentionally thin — its only job is to satisfy ASP.NET DI
// and translate the concrete dependencies into the Pipeline.PipelineDeps record.
// All real logic lives in Pipeline.run.

open System.Threading
open Azure.Storage.Blobs
open Elastic.Clients.Elasticsearch
open Microsoft.Extensions.Logging
open MongoDB.Driver

type DataCleanupService(
    logger     : ILogger<DataCleanupService>,
    container  : BlobContainerClient,
    mongoClient: IMongoClient,
    esClient   : ElasticsearchClient) =

    member _.RunCleanupAsync(ct: CancellationToken) =
        let deps : Pipeline.PipelineDeps = {
            Logger         = logger
            GetFirstDoc    = MongoRepository.findFirst  mongoClient
            StreamDocs     = MongoRepository.streamAll  mongoClient
            InsertBatch    = MongoRepository.insertBatch mongoClient
            IndexDocuments = fun docs ct -> ElasticsearchIndexer.createOnly esClient logger docs ct
            UploadCsv      = fun path ct -> BlobUploader.uploadAsync container logger path ct
            EnsureMongoIdx = MongoRepository.ensureIndex mongoClient logger
            EnsureEsIdx    = ElasticsearchIndexer.ensureIndex esClient
        }
        Pipeline.run deps ct

