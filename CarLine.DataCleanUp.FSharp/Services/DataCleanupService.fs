namespace CarLine.DataCleanUp.FSharp

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

