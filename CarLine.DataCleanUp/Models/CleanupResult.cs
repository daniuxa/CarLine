namespace CarLine.DataCleanUp.Models;

public class CleanupResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long RowsRead { get; set; }
    public long RowsWritten { get; set; }
    public long RowsDropped { get; set; }
    public int Errors { get; set; }
    public string? BlobName { get; set; }
    public TimeSpan Duration { get; set; }
}