namespace BioshipSimulator.Models;

public sealed record MissionStageStep(
    int StepNumber,
    FlightStage Stage,
    string MissionTimeWindow,
    string DistanceAndLocation,
    string StageTitle,
    string Description,
    float RecyclingRatePercent,
    float MlPredictedYieldPercent,
    float FuelReservePercent,
    string ActiveCrewStatus,
    string BioTechBalanceNote
);
