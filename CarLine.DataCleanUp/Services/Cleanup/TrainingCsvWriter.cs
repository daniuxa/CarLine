using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace CarLine.DataCleanUp.Services.Cleanup;

internal sealed class TrainingCsvWriter : IAsyncDisposable
{
    private readonly CsvWriter _csv;
    private readonly StreamWriter _writer;

    public TrainingCsvWriter(string filePath)
    {
        FilePath = filePath;

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim
        };

        // Use an explicit FileStream to control FileShare flags (important on Windows).
        var stream = new FileStream(
            FilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);

        _writer = new StreamWriter(stream, Encoding.UTF8);
        _csv = new CsvWriter(_writer, csvConfig);
    }

    public string FilePath { get; }

    public async ValueTask DisposeAsync()
    {
        await _csv.DisposeAsync();
        await _writer.DisposeAsync();
    }

    public async Task WriteHeaderAsync(IReadOnlyList<string> header)
    {
        foreach (var h in header)
            _csv.WriteField(h);

        await _csv.NextRecordAsync();
    }

    public void WriteRow(IReadOnlyList<string> header, IReadOnlyDictionary<string, string> record)
    {
        foreach (var col in header)
            _csv.WriteField(record.TryGetValue(col, out var v) ? v : string.Empty);

        _csv.NextRecord();
    }

    public async Task FlushAsync()
    {
        await _csv.FlushAsync();
        await _writer.FlushAsync();
    }
}