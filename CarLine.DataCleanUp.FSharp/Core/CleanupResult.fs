namespace CarLine.DataCleanUp.FSharp

open System

/// Immutable summary returned after a cleanup run completes.
type CleanupResult = {
    Success    : bool
    Message    : string
    RowsRead   : int64
    RowsWritten: int64
    RowsDropped: int64
    Errors     : int
    BlobName   : string option   // 'option' replaces nullable string?
    Duration   : TimeSpan
}

