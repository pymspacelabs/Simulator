namespace Simulator.ML;

public sealed class FastTreeEnsembleModel
{
    public double Bias { get; set; }
    public List<double> TreeWeights { get; set; } = new();
    public List<FastTreeNodeData> Trees { get; set; } = new();

    public float Predict(float[] features)
    {
        double sum = Bias;
        for (int i = 0; i < Trees.Count; i++)
        {
            double weight = i < TreeWeights.Count ? TreeWeights[i] : 1.0;
            sum += weight * Trees[i].Evaluate(features);
        }
        return (float)sum;
    }
}
