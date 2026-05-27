module CarLine.DataCleanUp.FSharp.MongoRepository

// Infrastructure — depends on Core + MongoDB.Driver.

open System.Threading
open MongoDB.Bson
open MongoDB.Driver
open Microsoft.Extensions.Logging

let private source  (client: IMongoClient) = client.GetDatabase("carsnosql").GetCollection<BsonDocument>("crawled_cars")
let private cleaned (client: IMongoClient) = client.GetDatabase("carsnosql").GetCollection<BsonDocument>("cleaned_cars")

let private findFirst (client: IMongoClient) (ct: CancellationToken) = task {
    let! doc = (source client).Find(FilterDefinition<BsonDocument>.Empty).Limit(1).FirstOrDefaultAsync(ct)
    return if isNull doc then None else Some doc
}

/// Returns the sorted, filtered CSV column list derived from the first source document.
/// Returns None if the collection is empty.
/// This is the implementation of Pipeline.PipelineDeps.BuildCsvHeader.
let buildCsvHeader (client: IMongoClient) (ct: CancellationToken) = task {
    let! firstDocOpt = findFirst client ct
    return
        firstDocOpt |> Option.map (fun doc ->
            doc.Names
            |> Seq.filter (fun n -> not (DataCleanupConstants.columnsToRemoveFromTraining.Contains(n)))
            |> Seq.sort
            |> ResizeArray)
}

/// Opens a cursor over all source documents for streaming.
let streamAll (client: IMongoClient) (ct: CancellationToken) =
    (source client).FindAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken = ct)

/// Bulk-inserts a batch; silently skips duplicates via the unique url index.
/// Returns the number of documents actually inserted.
let insertBatch (client: IMongoClient) (batch: System.Collections.Generic.IReadOnlyList<WriteModel<BsonDocument>>) (ct: CancellationToken) = task {
    if batch.Count = 0 then return 0L
    else
        try
            let! result = (cleaned client).BulkWriteAsync(batch, BulkWriteOptions(IsOrdered = false), ct)
            return result.InsertedCount
        with :? MongoBulkWriteException<BsonDocument> as ex ->
            let dupes =
                ex.WriteErrors
                |> Seq.filter (fun e -> e.Category = ServerErrorCategory.DuplicateKey)
                |> Seq.length
            if dupes = ex.WriteErrors.Count then return ex.Result.InsertedCount
            else return raise ex
}

/// Ensures a unique sparse index on the 'url' field (idempotent).
let ensureIndex (client: IMongoClient) (logger: ILogger) (ct: CancellationToken) = task {
    try
        let keys    = Builders<BsonDocument>.IndexKeys.Ascending("url")
        let options = CreateIndexOptions(Unique = true, Sparse = true, Name = "idx_url_unique")
        let! _      = (cleaned client).Indexes.CreateOneAsync(CreateIndexModel<BsonDocument>(keys, options), cancellationToken = ct)
        logger.LogInformation("Ensured unique index on 'url'")
    with :? MongoCommandException as ex
        when ex.CodeName = "IndexOptionsConflict" || ex.CodeName = "IndexKeySpecsConflict" ->
        logger.LogInformation("Index on 'url' already exists")
}
