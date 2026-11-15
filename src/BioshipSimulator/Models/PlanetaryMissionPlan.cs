namespace BioshipSimulator.Models;

public sealed record PlanetaryMissionPlan(
    CelestialBodyProfile Target,
    double LiveDistanceKm,
    int ActiveCrew,
    int MaxSupportedCrew,
    float InitialPredictedYield,
    float InitialCrewHealthProb,
    float InitialSystemReliability,
    float OutboundTransitDays,
    float MaxExplorationStayDays,
    float ReturnTransitDays,
    float TotalCampaignDays,
    float SafeReturnFuelReservePercent,
    float CrewO2DemandKgPerDay,
    float AlgalO2ProducedKgPerDay,
    float Ch4SynthesizedKgPerDay,
    List<MissionStageStep> NominalStages
);
