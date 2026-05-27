namespace CarLine.DataCleanUp.FSharp

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.Text
open CsvHelper
open CsvHelper.Configuration

/// Wraps CsvHelper for sequential row-by-row writing of the training CSV.
/// Implements IDisposable so it can be used with 'use' inside task {} — the
/// file handle is released synchronously as soon as the scope ends, before
/// the blob upload phase opens the same file.
type CsvWriter(filePath: string) =
    let stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read)
    let writer = new StreamWriter(stream, Encoding.UTF8)
    let config = CsvConfiguration(CultureInfo.InvariantCulture, HasHeaderRecord = true, TrimOptions = TrimOptions.Trim)
    let csv    = new CsvHelper.CsvWriter(writer, config)

    member _.FilePath = filePath

    member _.WriteHeaderAsync(header: ResizeArray<string>) = task {
        for h in header do csv.WriteField(h)
        do! csv.NextRecordAsync()
    }

    member _.WriteRow(header: ResizeArray<string>, record: IReadOnlyDictionary<string, string>) =
        for col in header do
            csv.WriteField(match record.TryGetValue(col) with true, v -> v | _ -> "")
        csv.NextRecord()

    member _.FlushAsync() =
        csv.FlushAsync()

    interface IDisposable with
        member _.Dispose() =
            csv.Dispose()
            writer.Dispose()

