using System.Globalization;
using System.Text;
using System.Text.Json;
using SimulatorML.Models;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;

namespace SimulatorML.ML;

public class BioShipMlAutopilot
{
    private readonly MLContext _mlContext = new(seed: 42);
    private readonly string _modelZipPath;
    private readonly string _csvDataPath;
    private readonly string _ensembleJsonPath;
    private readonly bool _isPtBr;

    private ITransformer? _yieldModel;
    private ITransformer? _stayDaysModel;
    private ITransformer? _healthModel;
    private ITransformer? _reliabilityModel;

    private PredictionEngine<SolarBioCycleData, BioCyclePrediction>? _yieldEngine;
    private PredictionEngine<SolarBioCycleData, BioCyclePrediction>? _stayDaysEngine;
    private PredictionEngine<SolarBioCycleData, BioCyclePrediction>? _healthEngine;
    private PredictionEngine<SolarBioCycleData, BioCyclePrediction>? _reliabilityEngine;

    public string ModelStatusMessage { get; private set; } = "Uninitialized";

    public BioShipMlAutopilot(string modelZipPath, string csvDataPath, string ensembleJsonPath, bool isPtBr = false)
    {
        _modelZipPath = modelZipPath;
        _csvDataPath = csvDataPath;
        _ensembleJsonPath = ensembleJsonPath;
        _isPtBr = isPtBr;
    }

    public void InitializeOrLoadModel(bool forceRetrain = false)
    {
        if (!File.Exists(_csvDataPath) || forceRetrain)
        {
            GenerateThreeDestinationsDatasetCsv(_csvDataPath, samplesPerBody: 80000);
        }

        IDataView trainingData = _mlContext.Data.LoadFromTextFile<SolarBioCycleData>(
            path: _csvDataPath,
            hasHeader: true,
            separatorChar: ',');

        string[] featureNames =
        [
            nameof(SolarBioCycleData.CrewCount),
            nameof(SolarBioCycleData.BioreactorVolumeLiters),
            nameof(SolarBioCycleData.WaterRecoveryEfficiency),
            nameof(SolarBioCycleData.BrineRecoveryEfficiency),
            nameof(SolarBioCycleData.OrganicBiomass),
            nameof(SolarBioCycleData.TechPower),
            nameof(SolarBioCycleData.EntropyAccumulated),
            nameof(SolarBioCycleData.EffectiveRadiationIndex),
            nameof(SolarBioCycleData.BioreactorThrottle),
            nameof(SolarBioCycleData.TechToBioAllocation),
            nameof(SolarBioCycleData.TargetLog10DistanceKm),
            nameof(SolarBioCycleData.TargetSolarIrradianceFactor),
            nameof(SolarBioCycleData.TargetMissionDeltaVms),
            nameof(SolarBioCycleData.TargetSurfaceGravityMs2),
            nameof(SolarBioCycleData.CrewProtectionSubsystemBuff),
            nameof(SolarBioCycleData.BiomeShieldingSubsystemBuff),
            nameof(SolarBioCycleData.TotalShipMassTons),
            nameof(SolarBioCycleData.FoodAndProvisionsKg),
            nameof(SolarBioCycleData.PotableWaterReserveLiters),
            nameof(SolarBioCycleData.MedicalSuppliesKg),
            nameof(SolarBioCycleData.ElectricalPowerKw),
            nameof(SolarBioCycleData.BatteryStorageKwh),
            nameof(SolarBioCycleData.ThermalRejectionCapacityKw),
            nameof(SolarBioCycleData.RadShieldingGcm2),
            nameof(SolarBioCycleData.MicrometeoroidWhippleLayers),
            nameof(SolarBioCycleData.ArtificialGravityG),
            nameof(SolarBioCycleData.EvaSuitDurabilityPct),
            nameof(SolarBioCycleData.ToolsAndSparesKg),
            nameof(SolarBioCycleData.CommsBandwidthGbps),
            nameof(SolarBioCycleData.OgaCapacityKgPerDay),
            nameof(SolarBioCycleData.CdraScrubbingCapacityKgPerDay),
            nameof(SolarBioCycleData.EclssSynergyIndex),
            nameof(SolarBioCycleData.OutpostWattBudgetDelta),
            nameof(SolarBioCycleData.OutpostOxygenLitersPerDayDelta),
            nameof(SolarBioCycleData.OutpostShieldingGcm2Delta),
            nameof(SolarBioCycleData.CommunicationDelaySec)
        ];

        var featPipe = _mlContext.Transforms.Concatenate("Features", featureNames);

        _yieldModel = featPipe.Append(_mlContext.Regression.Trainers.FastTree(
            labelColumnName: nameof(SolarBioCycleData.LabelFuelYieldRate),
            featureColumnName: "Features", numberOfLeaves: 48, numberOfTrees: 160, minimumExampleCountPerLeaf: 14, learningRate: 0.12)).Fit(trainingData);

        _stayDaysModel = featPipe.Append(_mlContext.Regression.Trainers.FastTree(
            labelColumnName: nameof(SolarBioCycleData.LabelMaxExplorationStayDays),
            featureColumnName: "Features", numberOfLeaves: 56, numberOfTrees: 180, minimumExampleCountPerLeaf: 14, learningRate: 0.12)).Fit(trainingData);

        _healthModel = featPipe.Append(_mlContext.Regression.Trainers.FastTree(
            labelColumnName: nameof(SolarBioCycleData.LabelCrewHealthSurvivalProb),
            featureColumnName: "Features", numberOfLeaves: 40, numberOfTrees: 140, minimumExampleCountPerLeaf: 14, learningRate: 0.12)).Fit(trainingData);

        _reliabilityModel = featPipe.Append(_mlContext.Regression.Trainers.FastTree(
            labelColumnName: nameof(SolarBioCycleData.LabelSystemReliabilityIndex),
            featureColumnName: "Features", numberOfLeaves: 40, numberOfTrees: 140, minimumExampleCountPerLeaf: 14, learningRate: 0.12)).Fit(trainingData);

        _mlContext.Model.Save(_yieldModel, trainingData.Schema, _modelZipPath);
        ExportQuadEnsembleToJson(_yieldModel, _stayDaysModel, _healthModel, _reliabilityModel, _ensembleJsonPath);

        _yieldEngine = _mlContext.Model.CreatePredictionEngine<SolarBioCycleData, BioCyclePrediction>(_yieldModel);
        _stayDaysEngine = _mlContext.Model.CreatePredictionEngine<SolarBioCycleData, BioCyclePrediction>(_stayDaysModel);
        _healthEngine = _mlContext.Model.CreatePredictionEngine<SolarBioCycleData, BioCyclePrediction>(_healthModel);
        _reliabilityEngine = _mlContext.Model.CreatePredictionEngine<SolarBioCycleData, BioCyclePrediction>(_reliabilityModel);

        ModelStatusMessage = _isPtBr
            ? "Quad-FastTree (240k amostras / 36 features / 4 saídas)"
            : "Quad-FastTree (240k samples / 36 features / 4 outputs)";
    }

    public static QuadMlPrediction ComputeQuadTargets(SolarBioCycleData s)
    {
        float safeCrew = Math.Max(1f, s.CrewCount);
        float effectiveCrewStress = Math.Clamp(1.0f - (s.CrewProtectionSubsystemBuff * 0.40f), 0.48f, 1.0f);

        float outpostO2KgPerDay = s.OutpostOxygenLitersPerDayDelta / 700.0f;
        float outpostPowerKwDelta = s.OutpostWattBudgetDelta / 1000.0f;
        float totalShieldingGcm2 = Math.Max(4.0f, s.RadShieldingGcm2 + s.OutpostShieldingGcm2Delta);
        float commDelayPenalty = Math.Clamp(s.CommunicationDelaySec / 2400.0f, 0.0f, 0.28f);

        float o2BalanceRatio = Math.Clamp((s.OgaCapacityKgPerDay + outpostO2KgPerDay + (s.BioreactorVolumeLiters / 420f) * 0.84f) / (safeCrew * 0.84f), 0.35f, 3.40f);
        float co2BalanceRatio = Math.Clamp((s.CdraScrubbingCapacityKgPerDay + (s.BioreactorVolumeLiters / 400f) * 1.04f) / (safeCrew * 1.04f), 0.35f, 3.20f);
        float gasLoopRatio = MathF.Sqrt(o2BalanceRatio * co2BalanceRatio);

        float requiredKw = (s.BioreactorVolumeLiters / 1000f) * 14.5f + (safeCrew * 3.4f) + (s.EclssSynergyIndex * 16.0f);
        float electricalRatio = Math.Clamp((s.ElectricalPowerKw + outpostPowerKwDelta + s.BatteryStorageKwh * 0.04f) / Math.Max(15f, requiredKw), 0.40f, 1.45f);
        float thermalRatio = Math.Clamp(s.ThermalRejectionCapacityKw / Math.Max(40f, requiredKw * 0.85f), 0.55f, 1.25f);
        float massEfficiencyFactor = Math.Clamp(112.0f / Math.Max(45.0f, s.TotalShipMassTons), 0.65f, 1.25f);

        float naturalLightBonus = Math.Clamp(MathF.Sqrt(Math.Min(1.5f, s.TargetSolarIrradianceFactor)) * 0.10f, 0.02f, 0.14f);
        float bnnShieldAttenuation = Math.Clamp(totalShieldingGcm2 / 48.0f, 0.05f, 0.78f);
        float shieldedRadiationPenalty = Math.Max(0f, (s.EffectiveRadiationIndex - 0.80f) * 0.075f * (1.0f - s.BiomeShieldingSubsystemBuff * 0.60f) * (1.0f - bnnShieldAttenuation * 0.58f));

        float totalWaterLoop = Math.Clamp((s.WaterRecoveryEfficiency * 0.75f) + (s.BrineRecoveryEfficiency * 0.25f), 0.80f, 0.994f);
        float loopClosure = Math.Clamp(
            (totalWaterLoop * 0.48f) +
            (s.EclssSynergyIndex * 0.26f) +
            ((1.0f - s.EntropyAccumulated) * 0.22f) +
            (s.BiomeShieldingSubsystemBuff * 0.10f) +
            naturalLightBonus -
            shieldedRadiationPenalty,
            0.16f, 0.995f);

        float bioTechSynergy = MathF.Sqrt((s.OrganicBiomass / 100f) * (s.TechPower / 100f)) *
                               (0.62f + 0.38f * MathF.Min(1.25f, s.BioreactorThrottle * s.TechToBioAllocation)) *
                               MathF.Sqrt(electricalRatio * thermalRatio) *
                               Math.Clamp(massEfficiencyFactor, 0.78f, 1.15f);

        float fuelYieldRate = Math.Clamp(
            bioTechSynergy * loopClosure * Math.Clamp(gasLoopRatio, 0.55f, 1.32f),
            0.14f, 0.99f);

        float envHostilityWear = Math.Clamp(1.0f + ((s.TargetLog10DistanceKm - 2.6f) * 0.08f) + ((s.TargetMissionDeltaVms - 9800f) / 35000f), 0.80f, 1.60f);
        float toolMaintenanceFactor = Math.Clamp(1.0f - (s.ToolsAndSparesKg / 2500f), 0.58f, 1.0f);
        float eclssFilterProtection = Math.Clamp(1.0f - (s.EclssSynergyIndex * 0.36f) - (s.BiomeShieldingSubsystemBuff * 0.36f), 0.35f, 1.0f);

        float crewLoadPenalty = Math.Clamp((safeCrew * effectiveCrewStress) / Math.Max(0.5f, s.BioreactorVolumeLiters / 360f), 0.14f, 2.50f);
        float dailyEntropyGain =
            ((0.0015f * crewLoadPenalty) +
             (0.0019f * (1.0f - totalWaterLoop) * 10f) +
             (0.0010f * MathF.Max(0f, s.BioreactorThrottle - 0.75f)) +
             (0.0006f * s.EffectiveRadiationIndex * (1.0f - s.BiomeShieldingSubsystemBuff * 0.60f)) -
             (0.0008f * (s.TechToBioAllocation - 0.70f))) * envHostilityWear * eclssFilterProtection * toolMaintenanceFactor;

        dailyEntropyGain = Math.Clamp(dailyEntropyGain, 0.0009f, 0.035f);
        float safeEntropyCeiling = Math.Clamp(0.85f - ((s.TargetLog10DistanceKm - 2.6f) * 0.020f) + (s.CrewProtectionSubsystemBuff * 0.06f), 0.64f, 0.89f);
        float entropyLimitedDays = ((safeEntropyCeiling - s.EntropyAccumulated) / dailyEntropyGain) * Math.Clamp(gasLoopRatio, 0.50f, 2.05f);

        float algalFoodSupplementRatio = Math.Clamp((s.BioreactorVolumeLiters / (safeCrew * 1050f)) * 0.48f, 0.10f, 0.60f);
        float netDailyFoodKg = safeCrew * 0.62f * (1.0f - algalFoodSupplementRatio);
        float foodLimitedDays = s.FoodAndProvisionsKg / Math.Max(0.20f, netDailyFoodKg);

        float netDailyWaterLossLiters = safeCrew * 3.54f * Math.Max(0.008f, 1.0f - totalWaterLoop);
        float waterLimitedDays = s.PotableWaterReserveLiters / Math.Max(0.10f, netDailyWaterLossLiters);

        float dailyMedConsumptionKg = safeCrew * 0.040f * Math.Max(0.50f, s.EffectiveRadiationIndex * (1.0f - s.CrewProtectionSubsystemBuff * 0.55f));
        float medLimitedDays = (s.MedicalSuppliesKg / Math.Max(0.018f, dailyMedConsumptionKg)) * (1.0f + Math.Min(0.25f, s.CommsBandwidthGbps * 0.012f) - commDelayPenalty * 0.35f);

        float dailySuitWearPct = (0.15f + (s.TargetLog10DistanceKm * 0.032f)) * (1.0f - s.CrewProtectionSubsystemBuff * 0.45f) * toolMaintenanceFactor;
        float suitLimitedDays = Math.Max(15f, (s.EvaSuitDurabilityPct - 25.0f) / Math.Max(0.065f, dailySuitWearPct));

        float logisticsDays = Math.Min(Math.Min(foodLimitedDays, waterLimitedDays), Math.Min(medLimitedDays, suitLimitedDays));
        float combinedStayDays = (entropyLimitedDays * 0.55f) + (Math.Min(entropyLimitedDays * 1.35f, logisticsDays) * 0.45f);
        float maxExplorationStayDays = Math.Clamp(combinedStayDays * massEfficiencyFactor, 12f, 720f);

        float gravityHealthFactor = Math.Clamp((s.TargetSurfaceGravityMs2 / 9.81f) + s.ArtificialGravityG + 0.35f, 0.35f, 1.0f);
        float medReserveRatio = Math.Clamp(s.MedicalSuppliesKg / (safeCrew * 35f), 0.30f, 1.25f);
        float crewHealthProb = Math.Clamp(
            0.52f +
            (s.CrewProtectionSubsystemBuff * 0.22f) +
            (bnnShieldAttenuation * 0.14f) +
            (gravityHealthFactor * 0.10f) +
            (Math.Min(1.0f, medReserveRatio) * 0.08f) -
            (shieldedRadiationPenalty * 0.65f) -
            (s.EntropyAccumulated * 0.12f),
            0.18f, 0.996f);

        float whippleFactor = Math.Clamp(s.MicrometeoroidWhippleLayers / 6.0f, 0.15f, 1.0f);
        float systemReliability = Math.Clamp(
            0.45f +
            (s.EclssSynergyIndex * 0.20f) +
            ((s.EvaSuitDurabilityPct / 100f) * 0.16f) +
            ((1.0f - toolMaintenanceFactor) * 0.22f) +
            (whippleFactor * 0.08f) +
            (Math.Min(1.0f, thermalRatio) * 0.08f) -
            (commDelayPenalty * 0.16f) -
            (s.EntropyAccumulated * 0.18f),
            0.20f, 0.995f);

        return new QuadMlPrediction(fuelYieldRate, maxExplorationStayDays, crewHealthProb, systemReliability);
    }

    private static void GenerateThreeDestinationsDatasetCsv(string filePath, int samplesPerBody)
    {
        var rng = new Random(42);
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8, bufferSize: 131072);
        writer.WriteLine("CrewCount,BioreactorVolumeLiters,WaterRecoveryEfficiency,BrineRecoveryEfficiency,OrganicBiomass,TechPower,EntropyAccumulated,EffectiveRadiationIndex,BioreactorThrottle,TechToBioAllocation,TargetLog10DistanceKm,TargetSolarIrradianceFactor,TargetMissionDeltaVms,TargetSurfaceGravityMs2,CrewProtectionSubsystemBuff,BiomeShieldingSubsystemBuff,TotalShipMassTons,FoodAndProvisionsKg,PotableWaterReserveLiters,MedicalSuppliesKg,ElectricalPowerKw,BatteryStorageKwh,ThermalRejectionCapacityKw,RadShieldingGcm2,MicrometeoroidWhippleLayers,ArtificialGravityG,EvaSuitDurabilityPct,ToolsAndSparesKg,CommsBandwidthGbps,OgaCapacityKgPerDay,CdraScrubbingCapacityKgPerDay,EclssSynergyIndex,OutpostWattBudgetDelta,OutpostOxygenLitersPerDayDelta,OutpostShieldingGcm2Delta,CommunicationDelaySec,LabelFuelYieldRate,LabelMaxExplorationStayDays,LabelCrewHealthSurvivalProb,LabelSystemReliabilityIndex");

        foreach (var body in CelestialTargetCatalog.SupportedBodies)
        {
            for (int i = 0; i < samplesPerBody; i++)
            {
                var sample = new SolarBioCycleData
                {
                    CrewCount = rng.Next(1, 17),
                    BioreactorVolumeLiters = (float)(rng.NextDouble() * 8200 + 1200),
                    WaterRecoveryEfficiency = (float)(rng.NextDouble() * 0.135 + 0.855),
                    BrineRecoveryEfficiency = (float)(rng.NextDouble() * 0.18 + 0.80),
                    OrganicBiomass = (float)(rng.NextDouble() * 75 + 25),
                    TechPower = (float)(rng.NextDouble() * 75 + 25),
                    EntropyAccumulated = (float)(rng.NextDouble() * 0.55 + 0.02),
                    EffectiveRadiationIndex = body.AmbientRadiationEnvIndex * (float)(rng.NextDouble() * 0.65 + 0.85),
                    BioreactorThrottle = (float)(rng.NextDouble() * 0.85 + 0.35),
                    TechToBioAllocation = (float)(rng.NextDouble() * 0.85 + 0.35),
                    TargetLog10DistanceKm = body.Log10DistanceKm,
                    TargetSolarIrradianceFactor = body.SolarIrradianceFactor,
                    TargetMissionDeltaVms = body.TotalMissionDeltaVms,
                    TargetSurfaceGravityMs2 = body.SurfaceGravityMs2,
                    CrewProtectionSubsystemBuff = (float)(rng.NextDouble() * 0.96),
                    BiomeShieldingSubsystemBuff = (float)(rng.NextDouble() * 0.96),
                    TotalShipMassTons = (float)(rng.NextDouble() * 135 + 48),
                    FoodAndProvisionsKg = (float)(rng.NextDouble() * 5200 + 400),
                    PotableWaterReserveLiters = (float)(rng.NextDouble() * 3800 + 400),
                    MedicalSuppliesKg = (float)(rng.NextDouble() * 500 + 30),
                    ElectricalPowerKw = (float)(rng.NextDouble() * 220 + 40),
                    BatteryStorageKwh = (float)(rng.NextDouble() * 900 + 80),
                    ThermalRejectionCapacityKw = (float)(rng.NextDouble() * 260 + 50),
                    RadShieldingGcm2 = (float)(rng.NextDouble() * 45 + 5),
                    MicrometeoroidWhippleLayers = rng.Next(1, 10),
                    ArtificialGravityG = (float)(rng.NextDouble() * 0.55),
                    EvaSuitDurabilityPct = (float)(rng.NextDouble() * 45 + 55),
                    ToolsAndSparesKg = (float)(rng.NextDouble() * 1400 + 100),
                    CommsBandwidthGbps = (float)(rng.NextDouble() * 26 + 0.5),
                    OgaCapacityKgPerDay = (float)(rng.NextDouble() * 12 + 2.3),
                    CdraScrubbingCapacityKgPerDay = (float)(rng.NextDouble() * 14 + 2.5),
                    EclssSynergyIndex = (float)(rng.NextDouble() * 0.85 + 0.15),
                    OutpostWattBudgetDelta = (float)(rng.NextDouble() * 68000 - 22000),
                    OutpostOxygenLitersPerDayDelta = (float)(rng.NextDouble() * 10500 - 1500),
                    OutpostShieldingGcm2Delta = (float)(rng.NextDouble() * 72),
                    CommunicationDelaySec = body.Id == CelestialTargetId.Mars ? (float)(rng.NextDouble() * 1400 + 60) : (body.Id == CelestialTargetId.Moon ? (float)(rng.NextDouble() * 120 + 1.28) : (float)(rng.NextDouble() * 15 + 0.05))
                };

                var quad = ComputeQuadTargets(sample);

                writer.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{sample.CrewCount:F0},{sample.BioreactorVolumeLiters:F1},{sample.WaterRecoveryEfficiency:F4},{sample.BrineRecoveryEfficiency:F4},{sample.OrganicBiomass:F2},{sample.TechPower:F2},{sample.EntropyAccumulated:F4},{sample.EffectiveRadiationIndex:F3},{sample.BioreactorThrottle:F3},{sample.TechToBioAllocation:F3},{sample.TargetLog10DistanceKm:F4},{sample.TargetSolarIrradianceFactor:F4},{sample.TargetMissionDeltaVms:F0},{sample.TargetSurfaceGravityMs2:F2},{sample.CrewProtectionSubsystemBuff:F3},{sample.BiomeShieldingSubsystemBuff:F3},{sample.TotalShipMassTons:F1},{sample.FoodAndProvisionsKg:F0},{sample.PotableWaterReserveLiters:F0},{sample.MedicalSuppliesKg:F0},{sample.ElectricalPowerKw:F1},{sample.BatteryStorageKwh:F0},{sample.ThermalRejectionCapacityKw:F0},{sample.RadShieldingGcm2:F1},{sample.MicrometeoroidWhippleLayers:F0},{sample.ArtificialGravityG:F2},{sample.EvaSuitDurabilityPct:F1},{sample.ToolsAndSparesKg:F0},{sample.CommsBandwidthGbps:F2},{sample.OgaCapacityKgPerDay:F2},{sample.CdraScrubbingCapacityKgPerDay:F2},{sample.EclssSynergyIndex:F3},{sample.OutpostWattBudgetDelta:F0},{sample.OutpostOxygenLitersPerDayDelta:F0},{sample.OutpostShieldingGcm2Delta:F1},{sample.CommunicationDelaySec:F1},{quad.FuelYieldRate:F4},{quad.MaxExplorationStayDays:F2},{quad.CrewHealthSurvivalProb:F4},{quad.SystemReliabilityIndex:F4}"));
            }
        }
    }

    private static void ExportQuadEnsembleToJson(
        ITransformer yieldModel,
        ITransformer stayDaysModel,
        ITransformer healthModel,
        ITransformer reliabilityModel,
        string jsonPath)
    {
        var yieldEnsemble = ExtractTreeEnsembleDto(yieldModel);
        var stayDaysEnsemble = ExtractTreeEnsembleDto(stayDaysModel);
        var healthEnsemble = ExtractTreeEnsembleDto(healthModel);
        var reliabilityEnsemble = ExtractTreeEnsembleDto(reliabilityModel);

        if (yieldEnsemble is not null && stayDaysEnsemble is not null)
        {
            var exportPayload = new
            {
                ModelVersion = "7.0-QuadEnsemble-240kSamples-36Features",
                TrainingSamplesCount = 240000,
                InputFeaturesCount = 36,
                SupportedDestinations = new[] { "ISS", "Moon", "Mars" },
                CelestialCatalog = CelestialTargetCatalog.SupportedBodies,
                YieldEnsemble = yieldEnsemble,
                DaysEnsemble = stayDaysEnsemble,
                HealthEnsemble = healthEnsemble,
                ReliabilityEnsemble = reliabilityEnsemble,
                Bias = yieldEnsemble.Bias,
                TreeWeights = yieldEnsemble.TreeWeights,
                Trees = yieldEnsemble.Trees
            };

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(exportPayload));
        }
    }

    private static FastTreeExportDto? ExtractTreeEnsembleDto(ITransformer model)
    {
        if (model is TransformerChain<ITransformer> chain &&
            chain.LastTransformer is ISingleFeaturePredictionTransformer<object> predTransformer &&
            predTransformer.Model is FastTreeRegressionModelParameters treeParams)
        {
            var ensemble = treeParams.TrainedTreeEnsemble;
            return new FastTreeExportDto(
                ensemble.Bias,
                ensemble.TreeWeights.ToList(),
                ensemble.Trees.Select(t => new FastTreeNodeExportDto(
                    t.LeftChild.ToArray(),
                    t.RightChild.ToArray(),
                    t.NumericalSplitFeatureIndexes.ToArray(),
                    t.NumericalSplitThresholds.Select(x => (double)x).ToArray(),
                    t.LeafValues.ToArray()
                )).ToList()
            );
        }
        return null;
    }

    public QuadMlPrediction PredictQuadForDestination(SolarBioCycleData state)
    {
        if (_yieldEngine is null || _stayDaysEngine is null || _healthEngine is null || _reliabilityEngine is null)
        {
            return ComputeQuadTargets(state);
        }

        float y = Math.Clamp(_yieldEngine.Predict(state).PredictedValue, 0.14f, 0.99f);
        float d = Math.Clamp(_stayDaysEngine.Predict(state).PredictedValue, 12f, 720f);
        float h = Math.Clamp(_healthEngine.Predict(state).PredictedValue, 0.18f, 0.996f);
        float r = Math.Clamp(_reliabilityEngine.Predict(state).PredictedValue, 0.20f, 0.995f);
        return new QuadMlPrediction(y, d, h, r);
    }

    public PlanetaryMissionPlan BuildCompleteMissionPlan(
        CelestialBodyProfile target,
        double liveDistanceKm,
        SolarBioCycleData baseQuery)
    {
        var initialQuad = PredictQuadForDestination(baseQuery);
        var cruiseQuad = PredictQuadForDestination(baseQuery with
        {
            OrganicBiomass = baseQuery.OrganicBiomass * 0.94f,
            TechPower = baseQuery.TechPower * 0.93f,
            EntropyAccumulated = baseQuery.EntropyAccumulated + 0.12f
        });
        var arrivalQuad = PredictQuadForDestination(baseQuery with
        {
            OrganicBiomass = baseQuery.OrganicBiomass * 0.88f,
            TechPower = baseQuery.TechPower * 0.88f,
            EntropyAccumulated = baseQuery.EntropyAccumulated + 0.24f
        });
        var safeLimitQuad = PredictQuadForDestination(baseQuery with
        {
            OrganicBiomass = baseQuery.OrganicBiomass * 0.74f,
            TechPower = baseQuery.TechPower * 0.76f,
            EntropyAccumulated = 0.68f
        });

        float algalO2KgDay = baseQuery.OgaCapacityKgPerDay + (baseQuery.OutpostOxygenLitersPerDayDelta / 700f) + ((baseQuery.BioreactorVolumeLiters / 420f) * 0.84f * (baseQuery.OrganicBiomass / 100f));
        int maxSupportedCrew = Math.Clamp((int)Math.Floor(algalO2KgDay / (0.84f * (1.0f - baseQuery.CrewProtectionSubsystemBuff * 0.20f))), 1, 24);
        float crewO2DemandKgDay = baseQuery.CrewCount * 0.84f;
        float ch4SynthesizedKgDay = 15.0f + (initialQuad.FuelYieldRate * 30.0f * baseQuery.BioreactorThrottle);

        float outboundDays = target.OutboundTransitDays;
        float returnDays = target.ReturnTransitDays;
        float totalCampaignDays = outboundDays + initialQuad.MaxExplorationStayDays + returnDays;
        float safeReturnReservePct = Math.Clamp(target.SafeReturnFuelReserveFloor + (target.TotalMissionDeltaVms / 2200f) + (baseQuery.TotalShipMassTons / 25f), 18.0f, 52.0f);

        float t3Injection = 0.25f;
        float t5Arrival = outboundDays;
        float t6SafeLimitTrigger = outboundDays + initialQuad.MaxExplorationStayDays;
        float t7ReturnCruise = t6SafeLimitTrigger + (returnDays * 0.60f);
        float t8Landing = totalCampaignDays;

        var nominalStages = new List<MissionStageStep>
        {
            new(1, FlightStage.LaunchAndLiftoff, "T+00m00s (Day 0.0)", "Earth Launch Complex (0 km)",
                $"1. Liftoff & Main Engine Ignition ({baseQuery.TotalShipMassTons:F1}t Bio-Ship)",
                $"Methalox engines ignite lifting {baseQuery.TotalShipMassTons:F1}t (Shielding: {baseQuery.RadShieldingGcm2:F0} g/cm², Power: {baseQuery.ElectricalPowerKw:F0} kWe).",
                baseQuery.WaterRecoveryEfficiency * 100f, initialQuad.FuelYieldRate * 100f, 98.0f, $"{baseQuery.CrewCount:F0}/{maxSupportedCrew} crew", $"Health ML: {initialQuad.CrewHealthSurvivalProb * 100f:F1}%"),
            new(2, FlightStage.AtmosphereExitAndStaging, "T+08m30s (Altitude 100 km)", "Atmosphere Exit — Kármán Line (100 km)",
                "2. Max-Q Clearance, Atmosphere Exit & Radiators / DSOC Deployment",
                $"Clearing Earth's atmosphere; {baseQuery.ThermalRejectionCapacityKw:F0} kWt radiators and {baseQuery.CommsBandwidthGbps:F1} Gbps optical comms deploy.",
                baseQuery.WaterRecoveryEfficiency * 100f, initialQuad.FuelYieldRate * 100f, 84.5f, $"{baseQuery.CrewCount:F0} in zero-G", $"Reliability ML: {initialQuad.SystemReliabilityIndex * 100f:F1}%"),
            new(3, FlightStage.LowEarthOrbitParkingAndCheck, "T+03h00m (Altitude 200 km)", "Low Earth Parking Orbit (200 km)",
                "3. LEO Stabilization & Marshall ECLSS Loop Closure",
                $"WPA+BPA water loop ({baseQuery.WaterRecoveryEfficiency * 100f:F1}%), OGA ({baseQuery.OgaCapacityKgPerDay:F1} kg O2/d), CDRA & Sabatier close the loop.",
                (baseQuery.WaterRecoveryEfficiency - 0.005f) * 100f, initialQuad.FuelYieldRate * 100f, 92.0f, $"O2 Demand: {crewO2DemandKgDay:F2} kg/d", $"O2 Output: {algalO2KgDay:F2} kg/d"),
            new(4, FlightStage.TransTargetInjectionBurn, $"Day {t3Injection:F2}", $"Earth Escape ➔ {target.ShortLabelEn}",
                $"4. Trans-{target.ShortLabelEn} Injection Burn (JPL Horizons COMMAND='{target.JplHorizonsCommand}')",
                $"Transfer burn (Delta-V: {target.TotalMissionDeltaVms:N0} m/s) calibrated for {baseQuery.TotalShipMassTons:F1}t wet mass.",
                (baseQuery.WaterRecoveryEfficiency - 0.015f) * 100f, ((initialQuad.FuelYieldRate + cruiseQuad.FuelYieldRate) * 50f), 78.5f, $"{baseQuery.CrewCount:F0} astronauts", $"Solar Flux: {target.SolarIrradianceFactor:F3}x Earth"),
            new(5, FlightStage.DeepSpaceSymbioticCruise, $"Day {t3Injection:F1} to Day {t5Arrival:F1}", $"Transit Cruise ({liveDistanceKm:N0} km)",
                $"5. Symbiotic Cruise & Centrifuge/OSDR Countermeasures to {target.ShortLabelEn}",
                $"Sleep centrifuge ({baseQuery.ArtificialGravityG:F2}g) and OSDR meds preserve crew health ({cruiseQuad.CrewHealthSurvivalProb * 100f:F1}%) while Sabatier regenerates CH4.",
                (baseQuery.WaterRecoveryEfficiency - 0.045f) * 100f, cruiseQuad.FuelYieldRate * 100f, 90.0f, $"Health: {cruiseQuad.CrewHealthSurvivalProb * 100f:F1}%", $"ECLSS Synergy: {baseQuery.EclssSynergyIndex * 100f:F0}%"),
            new(6, FlightStage.TargetOrbitCaptureAndExploration, $"Day {t5Arrival:F1} to Day {t6SafeLimitTrigger:F1}", $"Orbit / Outpost at {target.ShortLabelEn} ({target.SurfaceGravityMs2:F2} m/s²)",
                $"6. Outpost Operations at {target.ShortLabelEn} ({initialQuad.MaxExplorationStayDays:F0} Days)",
                $"Outpost systems sustain {initialQuad.MaxExplorationStayDays:F0} days against local hostility ({target.ExternalHostilityIndex:F2}x) with {baseQuery.CommunicationDelaySec:F1}s comms latency.",
                74.0f, arrivalQuad.FuelYieldRate * 100f, 74.0f, $"{baseQuery.CrewCount:F0} on EVA", $"Stay ML: {initialQuad.MaxExplorationStayDays:F0}d"),
            new(7, FlightStage.SafeRateLimitReturnBurn, $"Day {t6SafeLimitTrigger:F1} (Safe Rate-Limit)", $"Departure from {target.ShortLabelEn}",
                $"7. Safe Rate-Limit Reached (Yield <= {target.SafeReturnYieldLimit * 100f:F0}%) ➔ Automated Return Burn",
                $"240k Quad-ML.NET triggers return burn as recharge rate, provisions, or suit reliability reach safe floor with {safeReturnReservePct + 3.5f:F1}% CH4.",
                53.0f, Math.Max(target.SafeReturnYieldLimit * 100f, safeLimitQuad.FuelYieldRate * 100f), safeReturnReservePct + 3.5f, $"{baseQuery.CrewCount:F0} returning", "Safe Rate-Limit Triggered"),
            new(8, FlightStage.EarthAtmosphericReentry, $"Day {t7ReturnCruise:F1} to Day {t8Landing:F1}", "Earth Reentry Interface (120 km)",
                "8. Return Transit & Earth Aerothermal Braking",
                "Spacecraft crosses return leg using preserved bio-methane and executes atmospheric capture at 120 km.",
                46.5f, (target.SafeReturnYieldLimit - 0.01f) * 100f, 14.2f, $"{baseQuery.CrewCount:F0} decelerating", "Heatshield & retro-burn"),
            new(9, FlightStage.TouchdownEarthLanding, $"Day {t8Landing:F1} ({totalCampaignDays / 30f:F1} Months)", "Earth Propulsive Touchdown (0 km)",
                $"9. Safe Earth Touchdown after {totalCampaignDays:F1} Days ({target.ShortLabelEn})",
                $"Earth ➔ {target.ShortLabelEn} ➔ Earth mission completed with {initialQuad.CrewHealthSurvivalProb * 100f:F1}% crew health.",
                44.0f, (target.SafeReturnYieldLimit - 0.02f) * 100f, 7.5f, $"{baseQuery.CrewCount:F0}/{baseQuery.CrewCount:F0} safe", "Closed-loop completed")
        };

        return new PlanetaryMissionPlan(
            target, liveDistanceKm, (int)baseQuery.CrewCount, maxSupportedCrew,
            initialQuad.FuelYieldRate, initialQuad.CrewHealthSurvivalProb, initialQuad.SystemReliabilityIndex,
            outboundDays, initialQuad.MaxExplorationStayDays, returnDays, totalCampaignDays,
            safeReturnReservePct, crewO2DemandKgDay, algalO2KgDay, ch4SynthesizedKgDay, nominalStages
        );
    }
}
