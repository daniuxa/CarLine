module CarLine.DataCleanUp.FSharp.DomainConstants

// Thin wrapper to re-export values from the infrastructure module. This avoids
// duplicate module names in the assembly while keeping the Domain folder
// readable and discoverable.

open CarLine.DataCleanUp.FSharp

let columnsToRemoveFromTraining = DataCleanupConstants.columnsToRemoveFromTraining
let webDisplayFields               = DataCleanupConstants.webDisplayFields
let manufacturerMap                = DataCleanupConstants.manufacturerMap
let validTransmissions             = DataCleanupConstants.validTransmissions
let validConditions                = DataCleanupConstants.validConditions
let validFuels                     = DataCleanupConstants.validFuels
let validTypes                     = DataCleanupConstants.validTypes


