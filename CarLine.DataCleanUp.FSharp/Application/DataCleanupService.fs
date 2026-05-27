namespace CarLine.DataCleanUp.FSharp

// ── Application layer ─────────────────────────────────────────────────────────
// Single responsibility: wire Infrastructure implementations into PipelineDeps,
// then call Pipeline.run. No business logic lives here.

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
            Logger          = logger

            // MongoRepository implements BuildCsvHeader — reads first doc, filters and sorts fields
            BuildCsvHeader  = MongoRepository.buildCsvHeader mongoClient

            // StreamProcessor implements RunDocumentLoop — owns all BSON/CSV/batch details
            RunDocumentLoop =
                StreamProcessor.runLoop
                    (MongoRepository.insertBatch      mongoClient)
                    (ElasticsearchIndexer.createOnly  esClient logger)
                    (MongoRepository.streamAll        mongoClient)
                    logger

            // BlobUploader implements UploadCsv — handles retry and upload
            UploadCsv       = BlobUploader.uploadAsync container logger

            EnsureMongoIdx  = MongoRepository.ensureIndex    mongoClient logger
            EnsureEsIdx     = ElasticsearchIndexer.ensureIndex esClient
        }
        Pipeline.run deps ct

