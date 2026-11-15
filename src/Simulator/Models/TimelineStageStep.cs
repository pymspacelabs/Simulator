using MudBlazor;

namespace Simulator.Models;

public sealed record TimelineStageStep(
    string MissionTimeWindow,
    string DistanceAndLocation,
    string StageTitle,
    string Description,
    float RecyclingRatePercent,
    float MlPredictedYieldPercent,
    float FuelReservePercent,
    string ActiveCrewStatus,
    string BioTechBalanceNote,
    Color DotColor
);
