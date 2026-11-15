namespace Simulator.Models;

public sealed record DestinationProfile(
    int OrderIndex,
    string IdKey,
    string ShortLabelPt,
    string ShortLabelEn,
    string DisplayNamePt,
    string DisplayNameEn,
    string JplHorizonsCommand,
    double MeanDistanceFromEarthKm,
    float SemiMajorAxisAu,
    float SolarIrradianceFactor,
    float AmbientRadiationEnvIndex,
    float ExternalHostilityIndex,
    float SurfaceGravityMs2,
    float EscapeVelocityKms,
    float SuitWearPerDay,
    float TotalMissionDeltaVms,
    float OutboundTransitDays,
    float ReturnTransitDays,
    float SafeReturnYieldLimit,
    float SafeReturnFuelReserveFloor,
    int RecommendedCrew,
    float BaseBioreactorLiters,
    List<string> HazardsPt,
    List<string> HazardsEn
);
