module CarLine.DataCleanUp.FSharp.BsonReader

// ── DRY: the single source of truth for reading a BsonDocument field ──────────
//
// Every caller in the codebase goes through these functions.
// Eliminates the 30-line ternary chain in the original C# DataCleanupService
// and the repeated TryGetValue + null-check patterns scattered across files.

open System
open MongoDB.Bson
open CarLine.Common.Models

let private tryRead (doc: BsonDocument) key =
    match doc.TryGetValue(key) with
    | true, v when not v.IsBsonNull ->
        BsonValueConverters.toStringValue v |> Option.ofObj
    | _ -> None

// Partial-application-friendly: pass 'doc' once, then apply 'key' per field.
// Example usage: let s = getString doc  →  s "manufacturer", s "model", ...
let getString    doc key = tryRead doc key |> Option.defaultValue ""
let getStringOpt doc key = tryRead doc key
let getInt       doc key = tryRead doc key |> Option.bind (fun s -> match Int32.TryParse(s)    with true, v -> Some v | _ -> None) |> Option.defaultValue 0
let getDecimal   doc key = tryRead doc key |> Option.bind (fun s -> match Decimal.TryParse(s)  with true, v -> Some v | _ -> None) |> Option.defaultValue 0m
let getDate      doc key = tryRead doc key |> Option.bind (fun s -> match DateTime.TryParse(s) with true, v -> Some v | _ -> None)

/// Maps a cleaned BsonDocument to a CarDocument.
/// Replaces the 30-line ternary chain from the original C# DataCleanupService.
/// Partial application (let s = getString doc) keeps each line short and uniform.
let toCarDocument (doc: BsonDocument) (url: string) : CarDocument =
    let s = getString    doc   // s "field" → string (empty string if missing)
    let o = getStringOpt doc   // o "field" → string option

    // C# POCO with a parameterless constructor — assign properties individually.
    // In F# this is the idiomatic way to initialise a mutable C# class.
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


