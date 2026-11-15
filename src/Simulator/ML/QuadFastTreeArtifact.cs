namespace Simulator.ML;

public sealed class QuadFastTreeArtifact
{
    public FastTreeEnsembleModel? YieldEnsemble { get; set; }
    public FastTreeEnsembleModel? DaysEnsemble { get; set; }
    public FastTreeEnsembleModel? HealthEnsemble { get; set; }
    public FastTreeEnsembleModel? ReliabilityEnsemble { get; set; }
    public double Bias { get; set; }
    public List<double> TreeWeights { get; set; } = new();
    public List<FastTreeNodeData> Trees { get; set; } = new();
}
