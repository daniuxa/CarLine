using Microsoft.ML.Data;

namespace CarLine.Common.Models;

public class CarTrainingModel
{
    // CSV column order: condition,fuel,manufacturer,model,odometer,price,region,transmission,type,year
    [LoadColumn(0)] public string? condition { get; set; }
    [LoadColumn(1)] public string? fuel { get; set; }
    [LoadColumn(2)] public string? manufacturer { get; set; }
    [LoadColumn(3)] public string? model { get; set; }
    [LoadColumn(4)] public float odometer { get; set; }
    [LoadColumn(5)] public float price { get; set; }
    [LoadColumn(6)] public string? region { get; set; }
    [LoadColumn(7)] public string? transmission { get; set; }
    [LoadColumn(8)] public string? type { get; set; }
    [LoadColumn(9)] public float year { get; set; }
}