module CarLine.DataCleanUp.FSharp.BsonReader

open System
open MongoDB.Bson
open CarLine.Common.Models

let private tryRead (doc: BsonDocument) key =
    match doc.TryGetValue(key) with
    | true, v when not v.IsBsonNull ->
        BsonValueConverters.toStringValue v |> Option.ofObj
    | _ -> None

let getString    doc key = tryRead doc key |> Option.defaultValue ""
let getStringOpt doc key = tryRead doc key
let getInt       doc key = tryRead doc key |> Option.bind (fun s -> match Int32.TryParse(s)    with true, v -> Some v | _ -> None) |> Option.defaultValue 0
let getDecimal   doc key = tryRead doc key |> Option.bind (fun s -> match Decimal.TryParse(s)  with true, v -> Some v | _ -> None) |> Option.defaultValue 0m
let getDate      doc key = tryRead doc key |> Option.bind (fun s -> match DateTime.TryParse(s) with true, v -> Some v | _ -> None)

let toCarDocument (doc: BsonDocument) (url: string) : CarDocument =
    let s = getString    doc
    let o = getStringOpt doc

    let c = CarDocument()
    c.Id           <- url
    c.Manufacturer <- s "manufacturer"
    c.Model        <- s "model"
    c.Year         <- getInt     doc "year"
    c.Status       <- "ACTIVE"
    c.Price        <- getDecimal doc "price"
    c.Odometer     <- getInt     doc "odometer"
    c.Transmission <- s "transmission"
    c.Condition    <- s "condition"
    c.Fuel         <- s "fuel"
    c.Type         <- s "type"
    c.Region       <- o "region"      |> Option.defaultValue null
    c.Url          <- url
    c.ImageUrl     <- o "image_url"   |> Option.defaultValue null
    c.Vin          <- o "vin"         |> Option.defaultValue null
    c.PaintColor   <- o "paint_color" |> Option.defaultValue null
    c.PostingDate  <- getDate doc "posting_date" |> Option.toNullable
    c.FirstSeen    <- DateTime.UtcNow
    c.LastSeen     <- DateTime.UtcNow
    c

