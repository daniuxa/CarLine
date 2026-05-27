namespace CarLine.DataCleanUp.FSharp

open System
open System.Collections.Generic

type DistinctValueTracker(header: string seq) =
    let values =
        let d = Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        for col in header do d[col] <- HashSet<string>(StringComparer.OrdinalIgnoreCase)
        d

    member _.TrackRow(record: IReadOnlyDictionary<string, string>) =
        for kvp in record do
            if not (String.IsNullOrWhiteSpace(kvp.Value)) then
                match values.TryGetValue(kvp.Key) with
                | true, set -> set.Add(kvp.Value) |> ignore
                | _         -> ()

    member _.GetCountsOrderedByColumn() =
        values
        |> Seq.sortBy (fun kvp -> kvp.Key)
        |> Seq.map    (fun kvp -> kvp.Key, kvp.Value.Count)
