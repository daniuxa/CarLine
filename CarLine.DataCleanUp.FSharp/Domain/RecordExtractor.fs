module CarLine.DataCleanUp.FSharp.RecordExtractor

open System
open System.Collections.Generic
open MongoDB.Bson
open CarLine.DataCleanUp.FSharp.BsonValueConverters

let extractAllFields (doc: BsonDocument) : Dictionary<string, string> =
    let record = Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    for name in doc.Names do
        if not (name.Equals("_id", StringComparison.OrdinalIgnoreCase)) then
            match doc.TryGetValue(name) with
            | true, v when not v.IsBsonNull -> record[name] <- BsonValueConverters.toTrimmedString v
            | _                             -> record[name] <- ""
    record

let extractCsvFields (doc: BsonDocument) (header: ResizeArray<string>) : Dictionary<string, string> =
    let record = Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    for col in header do
        match doc.TryGetValue(col) with
        | true, v when not v.IsBsonNull -> record[col] <- BsonValueConverters.toTrimmedString v
        | _                             -> record[col] <- ""
    record


