module CarLine.DataCleanUp.FSharp.RecordCleaner

// Railway-Oriented Programming

open System
open System.Collections.Generic

type private Records = Dictionary<string, string> * Dictionary<string, string>

let private validateField fieldName (validValues: HashSet<string>) ((csv, full): Records) =
    match csv.TryGetValue(fieldName) with
    | true, v when not (String.IsNullOrWhiteSpace(v)) ->
        let norm = v.Trim().ToLowerInvariant()
        if validValues.Contains(norm) then
            csv[fieldName]  <- norm
            full[fieldName] <- norm
            Ok (csv, full)
        else Error $"Invalid {fieldName}: '{norm}'"
    | _ -> Error $"Missing {fieldName}"

let private standardizeManufacturer ((csv, full): Records) =
    match csv.TryGetValue("manufacturer") with
    | true, manu when not (String.IsNullOrWhiteSpace(manu)) ->
        let norm = manu.Trim().ToLowerInvariant()
        let standardized =
            match DataCleanupConstants.manufacturerMap.TryGetValue(norm) with
            | true, mapped -> mapped
            | _            -> norm
        csv["manufacturer"]  <- standardized
        full["manufacturer"] <- standardized
        Ok (csv, full)
    | _ -> Error "Missing manufacturer"

let private normalizeModel ((csv, full): Records) =
    match csv.TryGetValue("model") with
    | true, model when not (String.IsNullOrWhiteSpace(model)) ->
        let firstWord =
            model.Trim().ToLowerInvariant()
                 .Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.tryHead
        match firstWord with
        | Some word ->
            csv["model"]  <- word
            full["model"] <- word
            Ok (csv, full)
        | None -> Error "Empty model after normalization"
    | _ -> Error "Missing model"

let private validateYear ((csv, full): Records) =
    match csv.TryGetValue("year") with
    | true, s when not (String.IsNullOrWhiteSpace(s)) ->
        match Int32.TryParse(s) with
        | true, y when y >= 1900 && y <= DateTime.UtcNow.Year + 1 ->
            let str = string y
            csv["year"]  <- str
            full["year"] <- str
            Ok (csv, full)
        | true, _ -> Error "Year out of range"
        | _       -> Error "Invalid year"
    | _ -> Error "Missing year"

let private validatePrice ((csv, full): Records) =
    match csv.TryGetValue("price") with
    | true, s when not (String.IsNullOrWhiteSpace(s)) ->
        match Decimal.TryParse(s) with
        | true, p when p >= 100m && p <= 1_000_000m ->
            let norm = p.ToString("F0")
            csv["price"]  <- norm
            full["price"] <- norm
            Ok (csv, full)
        | true, _ -> Error "Price out of range"
        | _       -> Error "Invalid price"
    | _ -> Error "Missing price"

let private validateOdometer ((csv, full): Records) =
    match csv.TryGetValue("odometer") with
    | true, s when not (String.IsNullOrWhiteSpace(s)) ->
        match Int32.TryParse(s) with
        | true, odo when odo >= 0 && odo <= 500_000 ->
            full["odometer"] <- string odo
            csv["odometer"]  <- Math.Log10(float odo + 1.0).ToString("F3")
            Ok (csv, full)
        | true, _ -> Error "Odometer out of range"
        | _       -> Error "Invalid odometer"
    | _ -> Error "Missing odometer"

let private normalizeRegion ((csv, full): Records) =
    match csv.TryGetValue("region") with
    | true, region when not (String.IsNullOrWhiteSpace(region)) ->
        let norm = region.Trim().ToLowerInvariant()
        csv["region"]  <- norm
        full["region"] <- norm
    | _ -> ()
    Ok (csv, full)

let cleanAndValidate (csvRecord: Dictionary<string, string>) (fullRecord: Dictionary<string, string>) : bool =
    Ok (csvRecord, fullRecord)
    |> Result.bind (validateField "transmission" DataCleanupConstants.validTransmissions)
    |> Result.bind (validateField "condition"    DataCleanupConstants.validConditions)
    |> Result.bind (validateField "fuel"         DataCleanupConstants.validFuels)
    |> Result.bind (validateField "type"         DataCleanupConstants.validTypes)
    |> Result.bind standardizeManufacturer
    |> Result.bind normalizeModel
    |> Result.bind validateYear
    |> Result.bind validatePrice
    |> Result.bind validateOdometer
    |> Result.bind normalizeRegion
    |> Result.isOk
