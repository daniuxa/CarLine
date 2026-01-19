namespace CarLine.Common.Models;

public sealed class BatchPredictionResponse
{
    public List<PredictionResponse>? Predictions { get; set; }
    public List<object>? Errors { get; set; }

    public int TotalRequested { get; set; }
    public int TotalSuccessful { get; set; }
    public int TotalFailed { get; set; }

    public DateTime Timestamp { get; set; }
}