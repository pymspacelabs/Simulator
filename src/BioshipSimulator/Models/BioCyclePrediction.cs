using Microsoft.ML.Data;

namespace BioshipSimulator.Models;

public class BioCyclePrediction
{
    [ColumnName("Score")]
    public float PredictedValue { get; set; }
}
