namespace CarLine.DataCleanUp.FSharp

open System
open System.Collections.Generic

/// Tracks distinct values per CSV column for post-run diagnostics.
type DistinctValueTracker(header: string seq) =
    // All state is private — callers interact only through the two public members.
    let values =
        let d = Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        for col in header do d[col] <- HashSet<string>(StringComparer.OrdinalIgnoreCase)
        d   // last expression is the return value — no explicit 'return' keyword

    member _.TrackRow(record: IReadOnlyDictionary<string, string>) =
        for kvp in record do
            if not (String.IsNullOrWhiteSpace(kvp.Value)) then
                match values.TryGetValue(kvp.Key) with
                | true, set -> set.Add(kvp.Value) |> ignore
                | _         -> ()

    /// Returns (columnName, distinctCount) tuples sorted alphabetically.
    member _.GetCountsOrderedByColumn() =
        values
        |> Seq.sortBy (fun kvp -> kvp.Key)
        |> Seq.map    (fun kvp -> kvp.Key, kvp.Value.Count)

