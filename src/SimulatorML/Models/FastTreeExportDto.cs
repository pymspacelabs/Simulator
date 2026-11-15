namespace SimulatorML.Models;

public sealed record FastTreeExportDto(
    double Bias,
    List<double> TreeWeights,
    List<FastTreeNodeExportDto> Trees
);

public sealed record FastTreeNodeExportDto(
    int[] LeftChild,
    int[] RightChild,
    int[] SplitFeatures,
    double[] Thresholds,
    double[] LeafValues
);
