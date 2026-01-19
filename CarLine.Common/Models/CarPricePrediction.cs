namespace CarLine.Common.Models;

// Prediction result
// Use property name 'Score' to match ML.NET default output name, avoiding ML dependency here
public class CarPricePrediction
{
    public float Score { get; set; }
}