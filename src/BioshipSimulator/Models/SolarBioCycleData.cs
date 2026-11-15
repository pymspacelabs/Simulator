using Microsoft.ML.Data;

namespace BioshipSimulator.Models;

public record class SolarBioCycleData
{
    [LoadColumn(0)]  public float CrewCount { get; set; }
    [LoadColumn(1)]  public float BioreactorVolumeLiters { get; set; }
    [LoadColumn(2)]  public float WaterRecoveryEfficiency { get; set; }
    [LoadColumn(3)]  public float BrineRecoveryEfficiency { get; set; }
    [LoadColumn(4)]  public float OrganicBiomass { get; set; }
    [LoadColumn(5)]  public float TechPower { get; set; }
    [LoadColumn(6)]  public float EntropyAccumulated { get; set; }
    [LoadColumn(7)]  public float EffectiveRadiationIndex { get; set; }
    [LoadColumn(8)]  public float BioreactorThrottle { get; set; }
    [LoadColumn(9)]  public float TechToBioAllocation { get; set; }
    [LoadColumn(10)] public float TargetLog10DistanceKm { get; set; }
    [LoadColumn(11)] public float TargetSolarIrradianceFactor { get; set; }
    [LoadColumn(12)] public float TargetMissionDeltaVms { get; set; }
    [LoadColumn(13)] public float TargetSurfaceGravityMs2 { get; set; }
    [LoadColumn(14)] public float CrewProtectionSubsystemBuff { get; set; }
    [LoadColumn(15)] public float BiomeShieldingSubsystemBuff { get; set; }
    [LoadColumn(16)] public float TotalShipMassTons { get; set; }
    [LoadColumn(17)] public float FoodAndProvisionsKg { get; set; }
    [LoadColumn(18)] public float PotableWaterReserveLiters { get; set; }
    [LoadColumn(19)] public float MedicalSuppliesKg { get; set; }
    [LoadColumn(20)] public float ElectricalPowerKw { get; set; }
    [LoadColumn(21)] public float BatteryStorageKwh { get; set; }
    [LoadColumn(22)] public float ThermalRejectionCapacityKw { get; set; }
    [LoadColumn(23)] public float RadShieldingGcm2 { get; set; }
    [LoadColumn(24)] public float MicrometeoroidWhippleLayers { get; set; }
    [LoadColumn(25)] public float ArtificialGravityG { get; set; }
    [LoadColumn(26)] public float EvaSuitDurabilityPct { get; set; }
    [LoadColumn(27)] public float ToolsAndSparesKg { get; set; }
    [LoadColumn(28)] public float CommsBandwidthGbps { get; set; }
    [LoadColumn(29)] public float OgaCapacityKgPerDay { get; set; }
    [LoadColumn(30)] public float CdraScrubbingCapacityKgPerDay { get; set; }
    [LoadColumn(31)] public float EclssSynergyIndex { get; set; }
    [LoadColumn(32)] public float OutpostWattBudgetDelta { get; set; }
    [LoadColumn(33)] public float OutpostOxygenLitersPerDayDelta { get; set; }
    [LoadColumn(34)] public float OutpostShieldingGcm2Delta { get; set; }
    [LoadColumn(35)] public float CommunicationDelaySec { get; set; }

    [LoadColumn(36)] public float LabelFuelYieldRate { get; set; }
    [LoadColumn(37)] public float LabelMaxExplorationStayDays { get; set; }
    [LoadColumn(38)] public float LabelCrewHealthSurvivalProb { get; set; }
    [LoadColumn(39)] public float LabelSystemReliabilityIndex { get; set; }
}
