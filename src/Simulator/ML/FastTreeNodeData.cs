namespace Simulator.ML;

public sealed class FastTreeNodeData
{
    public int[] LeftChild { get; set; } = [];
    public int[] RightChild { get; set; } = [];
    public int[] SplitFeatures { get; set; } = [];
    public double[] Thresholds { get; set; } = [];
    public double[] LeafValues { get; set; } = [];

    public double Evaluate(float[] features)
    {
        int node = 0;
        while (node >= 0)
        {
            int featIdx = SplitFeatures[node];
            double val = featIdx < features.Length ? features[featIdx] : 0.0;
            node = val <= Thresholds[node] ? LeftChild[node] : RightChild[node];
        }
        return LeafValues[~node];
    }
}
