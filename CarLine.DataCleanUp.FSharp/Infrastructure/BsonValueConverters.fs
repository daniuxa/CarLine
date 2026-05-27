module CarLine.DataCleanUp.FSharp.BsonValueConverters

open MongoDB.Bson

let toStringValue (value: BsonValue) =
    match value with
    | :? BsonString  as s -> s.AsString
    | :? BsonInt32   as i -> i.ToString()
    | :? BsonInt64   as l -> l.ToString()
    | :? BsonDouble  as d -> d.ToString()
    | :? BsonBoolean as b -> b.ToString()
    | _                   -> value.ToString() |> Option.ofObj |> Option.defaultValue ""

let toTrimmedString (value: BsonValue) =
    toStringValue value |> fun s -> s.Trim()
