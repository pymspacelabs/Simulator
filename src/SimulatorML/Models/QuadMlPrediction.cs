namespace SimulatorML.Models;

public sealed record QuadMlPrediction(
    float FuelYieldRate,
    float MaxExplorationStayDays,
    float CrewHealthSurvivalProb,
    float SystemReliabilityIndex
);
