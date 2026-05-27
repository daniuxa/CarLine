namespace CarLine.DataCleanUp.FSharp

/// Outcome of processing a single document.
/// Explicit DU makes both outcomes visible at the call site;
/// the compiler enforces exhaustive handling in every match.
type DocOutcome = Written | Dropped

/// All pipeline counters in one immutable record.
type ProcessingStats = {
    Read    : int64
    Written : int64
    Dropped : int64
    Errors  : int
    Inserted: int64
} with
    static member Zero = { Read=0L; Written=0L; Dropped=0L; Errors=0; Inserted=0L }

