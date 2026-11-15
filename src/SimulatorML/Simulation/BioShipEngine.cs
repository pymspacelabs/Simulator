using SimulatorML.ML;
using SimulatorML.Models;
using SimulatorML.Services;
using Spectre.Console;

namespace SimulatorML.Simulation;

public class BioShipEngine
{
    private readonly Queue<string> _logs = new();
    private readonly BioShipMlAutopilot _autopilot;
    private int _engineeringProfileIndex = 0;

    public bool IsPtBr { get; }
    public NasaMultiApiSnapshot Snapshot { get; }
    public CelestialBodyProfile Profile { get; private set; }
    public PlanetaryMissionPlan Plan { get; private set; } = null!;
    public string ModelStatus { get; private set; }

    public float MissionDay { get; private set; }
    public FlightStage CurrentStage { get; private set; } = FlightStage.LaunchAndLiftoff;
    public string CurrentStageTitle => Plan.NominalStages.FirstOrDefault(s => s.Stage == CurrentStage)?.StageTitle ?? CurrentStage.ToString();

    public int CrewCount { get; private set; }
    public string ActiveEngineeringPresetName { get; private set; } = "Baseline Closed-Loop Outpost";
    public float BioreactorVolumeLiters { get; private set; } = 6400f;
    public float WaterRecoveryEfficiency { get; private set; } = 0.985f;
    public float BrineRecoveryEfficiency { get; private set; } = 0.940f;
    public float OrganicBiomass { get; private set; } = 95.0f;
    public float TechPower { get; private set; } = 94.0f;
    public float BioFuelReserve { get; private set; } = 96.0f;
    public float RecyclingEfficiency { get; private set; } = 0.985f;
    public float EntropyAccumulated { get; private set; } = 0.03f;
    public float PredictedFuelYieldRate { get; private set; } = 0.90f;
    public float PredictedCrewHealthProb { get; private set; } = 0.96f;
    public float PredictedSystemReliability { get; private set; } = 0.95f;
    public float BioreactorThrottle { get; private set; } = 0.82f;
    public float TechToBioAllocation { get; private set; } = 0.85f;

    public float TotalShipMassTons { get; private set; } = 96.0f;
    public float FoodAndProvisionsKg { get; private set; } = 3600f;
    public float PotableWaterReserveLiters { get; private set; } = 2400f;
    public float MedicalSuppliesKg { get; private set; } = 420f;
    public float ElectricalPowerKw { get; private set; } = 185f;
    public float BatteryStorageKwh { get; private set; } = 720f;
    public float ThermalRejectionKw { get; private set; } = 260f;
    public float RadShieldingGcm2 { get; private set; } = 38f;
    public int WhippleLayers { get; private set; } = 8;
    public float ArtificialGravityG { get; private set; } = 0.42f;
    public float EvaSuitDurabilityPct { get; private set; } = 99f;
    public float ToolsAndSparesKg { get; private set; } = 980f;
    public float CommsBandwidthGbps { get; private set; } = 23.5f;
    public float OgaCapacityKgPerDay { get; private set; } = 8.4f;
    public float CdraScrubbingCapacityKgPerDay { get; private set; } = 9.6f;
    public float EclssSynergyIndex { get; private set; } = 0.92f;
    public float CrewProtectionSubsystemBuff { get; private set; } = 0.84f;
    public float BiomeShieldingSubsystemBuff { get; private set; } = 0.86f;
    public float OutpostWattBudgetDelta { get; private set; } = 20800f;
    public float OutpostOxygenLitersPerDayDelta { get; private set; } = 6170f;
    public float OutpostShieldingGcm2Delta { get; private set; } = 45.0f;
    public float CommunicationDelaySec { get; private set; } = 1.28f;

    public double CurrentTargetDistanceKm =>
        Snapshot.LiveDistancesFromEarthKm.TryGetValue(Profile.Id.ToString(), out var d) ? d : Profile.MeanDistanceFromEarthKm;

    public double DistanceFromEarthKm { get; private set; }
    public float EffectiveRadiationIndex => Profile.AmbientRadiationEnvIndex * Snapshot.BaseSolarWeatherMultiplier;
    public IEnumerable<string> RecentLogs => _logs;

    public BioShipEngine(CelestialTargetId initialTarget, NasaMultiApiSnapshot snapshot, BioShipMlAutopilot autopilot, bool isPtBr = false)
    {
        IsPtBr = isPtBr;
        Snapshot = snapshot;
        _autopilot = autopilot;
        ModelStatus = autopilot.ModelStatusMessage;
        Profile = CelestialTargetCatalog.GetById(initialTarget);
        CrewCount = Profile.RecommendedCrew;

        ApplyEngineeringParameterPreset(0);
        RecalculatePlan();

        AddLog(IsPtBr
            ? $"[cyan1] Engenharia parametrizada ({TotalShipMassTons:F1}t / {ActiveEngineeringPresetName}):[/] [bold]{Markup.Escape(Profile.DisplayNamePt)}[/]"
            : $"[cyan1] Engineering preset loaded ({TotalShipMassTons:F1}t / {ActiveEngineeringPresetName}):[/] [bold]{Markup.Escape(Profile.DisplayNameEn)}[/]");
    }

    private void ApplyEngineeringParameterPreset(int presetIdx)
    {
        int mode = presetIdx % 3;
        if (mode == 0)
        {
            ActiveEngineeringPresetName = "High-Autonomy Fission + Regolith Shield";
            BioreactorVolumeLiters = Profile.BaseBioreactorLiters + 4200f;
            WaterRecoveryEfficiency = 0.988f;
            BrineRecoveryEfficiency = 0.955f;
            ElectricalPowerKw = 215f;
            BatteryStorageKwh = 840f;
            ThermalRejectionKw = 290f;
            RadShieldingGcm2 = 42f;
            WhippleLayers = 9;
            ArtificialGravityG = 0.42f;
            FoodAndProvisionsKg = 3800f;
            PotableWaterReserveLiters = 2600f;
            MedicalSuppliesKg = 450f;
            ToolsAndSparesKg = 1100f;
            CommsBandwidthGbps = 24f;
            OgaCapacityKgPerDay = 9.2f;
            CdraScrubbingCapacityKgPerDay = 10.4f;
            EclssSynergyIndex = 0.94f;
            CrewProtectionSubsystemBuff = 0.88f;
            BiomeShieldingSubsystemBuff = 0.89f;
            OutpostWattBudgetDelta = 28500f;
            OutpostOxygenLitersPerDayDelta = 6800f;
            OutpostShieldingGcm2Delta = 52f;
        }
        else if (mode == 1)
        {
            ActiveEngineeringPresetName = "Lightweight Fast-Transit Configuration";
            BioreactorVolumeLiters = Profile.BaseBioreactorLiters + 2400f;
            WaterRecoveryEfficiency = 0.962f;
            BrineRecoveryEfficiency = 0.910f;
            ElectricalPowerKw = 145f;
            BatteryStorageKwh = 520f;
            ThermalRejectionKw = 210f;
            RadShieldingGcm2 = 24f;
            WhippleLayers = 6;
            ArtificialGravityG = 0.28f;
            FoodAndProvisionsKg = 2400f;
            PotableWaterReserveLiters = 1800f;
            MedicalSuppliesKg = 280f;
            ToolsAndSparesKg = 680f;
            CommsBandwidthGbps = 14f;
            OgaCapacityKgPerDay = 6.8f;
            CdraScrubbingCapacityKgPerDay = 7.5f;
            EclssSynergyIndex = 0.82f;
            CrewProtectionSubsystemBuff = 0.72f;
            BiomeShieldingSubsystemBuff = 0.74f;
            OutpostWattBudgetDelta = 8500f;
            OutpostOxygenLitersPerDayDelta = 3200f;
            OutpostShieldingGcm2Delta = 24f;
        }
        else
        {
            ActiveEngineeringPresetName = "DONKI Solar Storm Survival Fortress";
            BioreactorVolumeLiters = Profile.BaseBioreactorLiters + 5000f;
            WaterRecoveryEfficiency = 0.992f;
            BrineRecoveryEfficiency = 0.975f;
            ElectricalPowerKw = 240f;
            BatteryStorageKwh = 960f;
            ThermalRejectionKw = 310f;
            RadShieldingGcm2 = 54f;
            WhippleLayers = 11;
            ArtificialGravityG = 0.48f;
            FoodAndProvisionsKg = 4400f;
            PotableWaterReserveLiters = 3100f;
            MedicalSuppliesKg = 520f;
            ToolsAndSparesKg = 1280f;
            CommsBandwidthGbps = 28f;
            OgaCapacityKgPerDay = 10.6f;
            CdraScrubbingCapacityKgPerDay = 11.8f;
            EclssSynergyIndex = 0.97f;
            CrewProtectionSubsystemBuff = 0.94f;
            BiomeShieldingSubsystemBuff = 0.95f;
            OutpostWattBudgetDelta = 34000f;
            OutpostOxygenLitersPerDayDelta = 7600f;
            OutpostShieldingGcm2Delta = 68f;
        }

        CommunicationDelaySec = Profile.Id == CelestialTargetId.Mars ? 420.0f : (Profile.Id == CelestialTargetId.Moon ? 1.28f : 0.05f);
        TotalShipMassTons = 42.0f + (BioreactorVolumeLiters / 1000f) + (RadShieldingGcm2 * 0.45f) +
                            ((FoodAndProvisionsKg + PotableWaterReserveLiters + MedicalSuppliesKg + ToolsAndSparesKg) / 1000f);
    }

    public void CycleEngineeringPreset()
    {
        _engineeringProfileIndex++;
        ApplyEngineeringParameterPreset(_engineeringProfileIndex);
        RecalculatePlan();
        AddLog(IsPtBr
            ? $"[bold SpringGreen1]⚙ Perfil de Engenharia alterado ({ActiveEngineeringPresetName})![/] Estadia: {Plan.MaxExplorationStayDays:F0}d."
            : $"[bold SpringGreen1]⚙ Engineering Preset switched ({ActiveEngineeringPresetName})![/] Stay: {Plan.MaxExplorationStayDays:F0}d.");
    }

    public void SwitchDestination(CelestialTargetId newTarget)
    {
        Profile = CelestialTargetCatalog.GetById(newTarget);
        CrewCount = Profile.RecommendedCrew;
        MissionDay = 0f;
        DistanceFromEarthKm = 0.0;
        EntropyAccumulated = 0.03f;
        OrganicBiomass = 95.0f;
        TechPower = 94.0f;
        BioFuelReserve = 96.0f;
        CurrentStage = FlightStage.LaunchAndLiftoff;

        ApplyEngineeringParameterPreset(_engineeringProfileIndex);
        RecalculatePlan();

        AddLog(IsPtBr
            ? $"[bold gold1]🪐 Destino: {Markup.Escape(Profile.DisplayNamePt)}[/] | Campanha ML: {Plan.TotalCampaignDays:F1} dias."
            : $"[bold gold1]🪐 Target: {Markup.Escape(Profile.DisplayNameEn)}[/] | ML Campaign: {Plan.TotalCampaignDays:F1} days.");
    }

    private SolarBioCycleData BuildCurrentQuerySnapshot() => new()
    {
        CrewCount = CrewCount,
        BioreactorVolumeLiters = BioreactorVolumeLiters,
        WaterRecoveryEfficiency = WaterRecoveryEfficiency,
        BrineRecoveryEfficiency = BrineRecoveryEfficiency,
        OrganicBiomass = OrganicBiomass,
        TechPower = TechPower,
        EntropyAccumulated = EntropyAccumulated,
        EffectiveRadiationIndex = EffectiveRadiationIndex,
        BioreactorThrottle = BioreactorThrottle,
        TechToBioAllocation = TechToBioAllocation,
        TargetLog10DistanceKm = (float)Math.Log10(Math.Max(100.0, CurrentTargetDistanceKm)),
        TargetSolarIrradianceFactor = Profile.SolarIrradianceFactor,
        TargetMissionDeltaVms = Profile.TotalMissionDeltaVms,
        TargetSurfaceGravityMs2 = Profile.SurfaceGravityMs2,
        CrewProtectionSubsystemBuff = CrewProtectionSubsystemBuff,
        BiomeShieldingSubsystemBuff = BiomeShieldingSubsystemBuff,
        TotalShipMassTons = TotalShipMassTons,
        FoodAndProvisionsKg = FoodAndProvisionsKg,
        PotableWaterReserveLiters = PotableWaterReserveLiters,
        MedicalSuppliesKg = MedicalSuppliesKg,
        ElectricalPowerKw = ElectricalPowerKw,
        BatteryStorageKwh = BatteryStorageKwh,
        ThermalRejectionCapacityKw = ThermalRejectionKw,
        RadShieldingGcm2 = RadShieldingGcm2,
        MicrometeoroidWhippleLayers = WhippleLayers,
        ArtificialGravityG = ArtificialGravityG,
        EvaSuitDurabilityPct = EvaSuitDurabilityPct,
        ToolsAndSparesKg = ToolsAndSparesKg,
        CommsBandwidthGbps = CommsBandwidthGbps,
        OgaCapacityKgPerDay = OgaCapacityKgPerDay,
        CdraScrubbingCapacityKgPerDay = CdraScrubbingCapacityKgPerDay,
        EclssSynergyIndex = EclssSynergyIndex,
        OutpostWattBudgetDelta = OutpostWattBudgetDelta,
        OutpostOxygenLitersPerDayDelta = OutpostOxygenLitersPerDayDelta,
        OutpostShieldingGcm2Delta = OutpostShieldingGcm2Delta,
        CommunicationDelaySec = CommunicationDelaySec
    };

    public void RecalculatePlan()
    {
        Plan = _autopilot.BuildCompleteMissionPlan(Profile, CurrentTargetDistanceKm, BuildCurrentQuerySnapshot());
        PredictedFuelYieldRate = Plan.InitialPredictedYield;
        PredictedCrewHealthProb = Plan.InitialCrewHealthProb;
        PredictedSystemReliability = Plan.InitialSystemReliability;
    }

    public void StepSimulation()
    {
        float dayStep = Math.Max(0.25f, Plan.TotalCampaignDays / 90f);
        MissionDay += dayStep;

        float progressRatio = Math.Clamp(MissionDay / Math.Max(1f, Plan.TotalCampaignDays), 0f, 1f);
        EntropyAccumulated = Math.Clamp(0.03f + (progressRatio * 0.65f), 0.03f, 0.92f);
        RecyclingEfficiency = Math.Clamp(WaterRecoveryEfficiency * (1.0f - EntropyAccumulated * 0.62f), 0.18f, 0.993f);
        EvaSuitDurabilityPct = Math.Clamp(99f - (progressRatio * 58f * (1.0f - CrewProtectionSubsystemBuff * 0.38f)), 20f, 100f);

        var liveQuad = _autopilot.PredictQuadForDestination(BuildCurrentQuerySnapshot());
        PredictedFuelYieldRate = liveQuad.FuelYieldRate;
        PredictedCrewHealthProb = liveQuad.CrewHealthSurvivalProb;
        PredictedSystemReliability = liveQuad.SystemReliabilityIndex;

        float outboundEnd = Plan.OutboundTransitDays;
        float stayEnd = Plan.OutboundTransitDays + Plan.MaxExplorationStayDays;

        if (MissionDay <= outboundEnd * 0.08f)
        {
            CurrentStage = MissionDay <= outboundEnd * 0.03f ? FlightStage.LaunchAndLiftoff : FlightStage.AtmosphereExitAndStaging;
            DistanceFromEarthKm = CurrentTargetDistanceKm * 0.02;
            BioFuelReserve = 86.0f;
        }
        else if (MissionDay <= outboundEnd * 0.16f)
        {
            CurrentStage = FlightStage.LowEarthOrbitParkingAndCheck;
            DistanceFromEarthKm = CurrentTargetDistanceKm * 0.05;
            BioFuelReserve = 91.0f;
        }
        else if (MissionDay <= outboundEnd * 0.25f)
        {
            CurrentStage = FlightStage.TransTargetInjectionBurn;
            DistanceFromEarthKm = CurrentTargetDistanceKm * 0.15;
            BioFuelReserve = 79.0f;
        }
        else if (MissionDay < outboundEnd)
        {
            CurrentStage = FlightStage.DeepSpaceSymbioticCruise;
            double cruiseFrac = (MissionDay - (outboundEnd * 0.25f)) / Math.Max(0.1f, outboundEnd * 0.75f);
            DistanceFromEarthKm = CurrentTargetDistanceKm * (0.15 + 0.85 * Math.Clamp(cruiseFrac, 0.0, 1.0));
            BioFuelReserve = Math.Clamp(82.0f + (PredictedFuelYieldRate * 10f), 60f, 95f);
        }
        else if (MissionDay < stayEnd && PredictedFuelYieldRate > Profile.SafeReturnYieldLimit)
        {
            CurrentStage = FlightStage.TargetOrbitCaptureAndExploration;
            DistanceFromEarthKm = CurrentTargetDistanceKm;
            float stayFrac = (MissionDay - outboundEnd) / Math.Max(1f, Plan.MaxExplorationStayDays);
            BioFuelReserve = Math.Clamp(85.0f - (stayFrac * (85.0f - Plan.SafeReturnFuelReservePercent - 3f)), Plan.SafeReturnFuelReservePercent, 90f);
        }
        else if (MissionDay < stayEnd + (Plan.ReturnTransitDays * 0.20f))
        {
            CurrentStage = FlightStage.SafeRateLimitReturnBurn;
            DistanceFromEarthKm = CurrentTargetDistanceKm * 0.90;
            BioFuelReserve = Plan.SafeReturnFuelReservePercent;
        }
        else if (MissionDay < Plan.TotalCampaignDays)
        {
            CurrentStage = FlightStage.EarthAtmosphericReentry;
            double retFrac = (MissionDay - stayEnd) / Math.Max(0.1f, Plan.ReturnTransitDays);
            DistanceFromEarthKm = CurrentTargetDistanceKm * Math.Clamp(1.0 - retFrac, 0.0, 1.0);
            BioFuelReserve = Math.Clamp(Plan.SafeReturnFuelReservePercent * (float)(1.0 - retFrac * 0.75), 7.5f, 50f);
        }
        else
        {
            CurrentStage = FlightStage.TouchdownEarthLanding;
            DistanceFromEarthKm = 0.0;
            BioFuelReserve = 7.5f;
        }
    }

    public void AdjustCrewCount(int delta)
    {
        CrewCount = Math.Clamp(CrewCount + delta, 1, 16);
        RecalculatePlan();
    }

    public void AdjustBioreactorThrottle(float delta)
    {
        BioreactorThrottle = Math.Clamp(BioreactorThrottle + delta, 0.40f, 1.20f);
        RecalculatePlan();
    }

    public void AdjustTechAllocation(float delta)
    {
        TechToBioAllocation = Math.Clamp(TechToBioAllocation + delta, 0.40f, 1.20f);
        RecalculatePlan();
    }

    public void PurgeBioToxinAndRepairSuits()
    {
        EntropyAccumulated = Math.Max(0.03f, EntropyAccumulated - 0.18f);
        EvaSuitDurabilityPct = Math.Min(100f, EvaSuitDurabilityPct + 15f);
        RecalculatePlan();
    }

    public void ForceEmergencyReturn()
    {
        CurrentStage = FlightStage.SafeRateLimitReturnBurn;
        MissionDay = Plan.OutboundTransitDays + Plan.MaxExplorationStayDays;
    }

    public void SetModelStatus(string status) => ModelStatus = status;

    public string FormatDistance(double km) =>
        km >= 1_000_000.0 ? $"{km / 1_000_000.0:N2} M km" : $"{km:N0} km";

    public void AddLog(string message)
    {
        _logs.Enqueue($"[grey]{DateTime.Now:HH:mm:ss}[/] {message}");
        while (_logs.Count > 6)
            _logs.Dequeue();
    }
}
