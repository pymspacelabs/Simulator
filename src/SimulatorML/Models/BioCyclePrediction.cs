using Microsoft.ML.Data;

namespace SimulatorML.Models;

public class BioCyclePrediction
{
    [ColumnName("Score")]
    public float PredictedValue { get; set; }
}
