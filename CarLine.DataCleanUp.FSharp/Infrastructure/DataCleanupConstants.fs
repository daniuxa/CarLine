module CarLine.DataCleanUp.FSharp.DataCleanupConstants

open System
open System.Collections.Generic

let columnsToRemoveFromTraining =
    HashSet<string>(
        [| "_id"; "url"; "region_url"; "title_status"; "vin"; "image_url"; "state"
           "lat"; "long"; "posting_date"; "paint_color"; "size"; "county"; "cylinders"; "drive" |],
        StringComparer.OrdinalIgnoreCase)

let webDisplayFields =
    HashSet<string>(
        [| "url"; "posting_date"; "image_url"; "region_url"; "title_status"; "vin"; "state"
           "lat"; "long"; "paint_color"; "size"; "county"; "cylinders"; "drive"
           "manufacturer"; "model"; "year"; "price"; "odometer"; "region"
           "transmission"; "condition"; "fuel"; "type" |],
        StringComparer.OrdinalIgnoreCase)

let manufacturerMap =
    dict [
        "chevy",            "chevrolet"
        "mercedes benz",    "mercedes-benz"
        "mercedes",         "mercedes-benz"
        "benz",             "mercedes-benz"
        "vw",               "volkswagen"
        "land rover",       "land-rover"
        "landrover",        "land-rover"
        "rover",            "land-rover"
        "alfa romeo",       "alfa-romeo"
        "alfaromeo",        "alfa-romeo"
        "harley davidson",  "harley-davidson"
        "harley",           "harley-davidson"
        "aston martin",     "aston-martin"
        "astonmartin",      "aston-martin"
        "datsun",           "nissan"
    ]

let validTransmissions =
    HashSet<string>([ "automatic"; "manual"; "other" ], StringComparer.OrdinalIgnoreCase)

let validConditions =
    HashSet<string>([ "excellent"; "good"; "fair"; "like new" ], StringComparer.OrdinalIgnoreCase)

let validFuels =
    HashSet<string>([ "gas"; "diesel"; "electric"; "hybrid"; "other" ], StringComparer.OrdinalIgnoreCase)

let validTypes =
    HashSet<string>(
        [ "sedan"; "suv"; "truck"; "coupe"; "van"; "wagon"
          "convertible"; "hatchback"; "pickup"; "other" ],
        StringComparer.OrdinalIgnoreCase)

