module CarLine.DataCleanUp.FSharp.BlobUploader

open System
open System.IO
open System.Threading
open Azure.Storage.Blobs
open Microsoft.Extensions.Logging

let rec private openWithRetry path attempt (delayMs: int) (logger: ILogger) (ct: CancellationToken) = task {
    ct.ThrowIfCancellationRequested()
    try
        return new FileStream(path, FileMode.Open, FileAccess.Read,
                              FileShare.ReadWrite ||| FileShare.Delete)
    with :? IOException when attempt < 6 ->
        logger.LogWarning("Retry {n}/6 opening '{path}'. Waiting {delay}ms...", attempt, path, delayMs)
        do! Threading.Tasks.Task.Delay(delayMs, ct)
        return! openWithRetry path (attempt + 1) (delayMs * 2) logger ct
}

let uploadAsync (container: BlobContainerClient) (logger: ILogger) (filePath: string) (ct: CancellationToken) = task {
    let blobName    = $"cleaned/{Path.GetFileName(filePath)}"
    let! fileStream = openWithRetry filePath 1 100 logger ct
    try
        let! _ = container.GetBlobClient(blobName).UploadAsync(fileStream, true, ct)
        logger.LogInformation("Training CSV uploaded to blob: {blob}", blobName)
        return blobName
    finally
        fileStream.Dispose()
}

