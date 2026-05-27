namespace CarLine.DataCleanUp.FSharp

open System

/// Immutable summary of a completed cleanup run.
/// F# records replace the C# mutable class with public setters.
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

