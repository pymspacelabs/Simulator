namespace Simulator.ML;

public static class QuadMlSurrogateEvaluator
{
    public static (float Yield, float StayDays, float HealthProb, float Reliability) ComputeQuadReference(float[] f)
    {
        float safeCrew = Math.Max(1f, f[0]);
        float bioLiters = f[1], waterEff = f[2], brineEff = f[3], biomass = f[4], tech = f[5], entropy = f[6], rad = f[7];
        float throttle = f[8], alloc = f[9], log10Dist = f[10], solarIrr = f[11], deltaV = f[12], surfG = f[13];
        float crewBuff = f[14], biomeBuff = f[15], massTons = f[16], foodKg = f[17], waterL = f[18], medsKg = f[19];
        float elecKw = f[20], battKwh = f[21], thermKw = f[22], radShield = f[23], whipple = f[24], artG = f[25];
        float suitPct = f[26], toolsKg = f[27], commsGbps = f[28], ogaKg = f[29], cdraKg = f[30], eclssSyn = f[31];
        float outpostWatt = f.Length > 32 ? f[32] : 0f;
        float outpostO2L = f.Length > 33 ? f[33] : 0f;
        float outpostShield = f.Length > 34 ? f[34] : 0f;
        float commDelaySec = f.Length > 35 ? f[35] : 1.28f;

        float commDelayPenalty = Math.Clamp(commDelaySec / 2400f, 0f, 0.28f);
        float effectiveCrewStress = Math.Clamp(1.0f - (crewBuff * 0.40f) + (commDelayPenalty * 0.12f), 0.48f, 1.05f);
        float o2Ratio = Math.Clamp((ogaKg + (outpostO2L / 700f) + (bioLiters / 420f) * 0.84f) / (safeCrew * 0.84f), 0.35f, 3.40f);
        float co2Ratio = Math.Clamp((cdraKg + (bioLiters / 400f) * 1.04f) / (safeCrew * 1.04f), 0.35f, 3.20f);
        float gasLoopRatio = MathF.Sqrt(o2Ratio * co2Ratio);

        float requiredKw = (bioLiters / 1000f) * 14.5f + (safeCrew * 3.4f) + (eclssSyn * 16.0f);
        float electricalRatio = Math.Clamp((elecKw + (outpostWatt / 1000f) + battKwh * 0.04f) / Math.Max(15f, requiredKw), 0.40f, 1.45f);
        float thermalRatio = Math.Clamp(thermKw / Math.Max(40f, requiredKw * 0.85f), 0.55f, 1.25f);
        float massEff = Math.Clamp(112.0f / Math.Max(45.0f, massTons), 0.65f, 1.25f);

        float naturalLight = Math.Clamp(MathF.Sqrt(Math.Min(1.5f, solarIrr)) * 0.10f, 0.02f, 0.14f);
        float bnnAtten = Math.Clamp((radShield + outpostShield) / 48.0f, 0.05f, 0.78f);
        float radPenalty = Math.Max(0f, (rad - 0.80f) * 0.075f * (1.0f - biomeBuff * 0.60f) * (1.0f - bnnAtten * 0.58f));

        float totalWaterLoop = Math.Clamp((waterEff * 0.75f) + (brineEff * 0.25f), 0.80f, 0.994f);
        float loopClosure = Math.Clamp((totalWaterLoop * 0.48f) + (eclssSyn * 0.26f) + ((1.0f - entropy) * 0.22f) + (biomeBuff * 0.10f) + naturalLight - radPenalty, 0.16f, 0.995f);
        float synergy = MathF.Sqrt((biomass / 100f) * (tech / 100f)) * (0.62f + 0.38f * MathF.Min(1.25f, throttle * alloc)) * MathF.Sqrt(electricalRatio * thermalRatio) * Math.Clamp(massEff, 0.78f, 1.15f);
        float yieldRate = Math.Clamp(synergy * loopClosure * Math.Clamp(gasLoopRatio, 0.55f, 1.32f), 0.14f, 0.99f);

        float envWear = Math.Clamp(1.0f + ((log10Dist - 2.6f) * 0.08f) + ((deltaV - 9800f) / 35000f), 0.80f, 1.60f);
        float toolFactor = Math.Clamp(1.0f - (toolsKg / 2500f), 0.58f, 1.0f);
        float filterProt = Math.Clamp(1.0f - (eclssSyn * 0.36f) - (biomeBuff * 0.36f), 0.35f, 1.0f);
        float crewPenalty = Math.Clamp((safeCrew * effectiveCrewStress) / Math.Max(0.5f, bioLiters / 360f), 0.14f, 2.50f);

        float dailyEntropy = ((0.0015f * crewPenalty) + (0.0019f * (1.0f - totalWaterLoop) * 10f) + (0.0010f * MathF.Max(0f, throttle - 0.75f)) + (0.0006f * rad * (1.0f - biomeBuff * 0.60f)) - (0.0008f * (alloc - 0.70f))) * envWear * filterProt * toolFactor;
        dailyEntropy = Math.Clamp(dailyEntropy, 0.0009f, 0.035f);
        float safeCeiling = Math.Clamp(0.85f - ((log10Dist - 2.6f) * 0.020f) + (crewBuff * 0.06f), 0.64f, 0.89f);
        float entropyDays = ((safeCeiling - entropy) / dailyEntropy) * Math.Clamp(gasLoopRatio, 0.50f, 2.05f);

        float algalFoodSup = Math.Clamp((bioLiters / (safeCrew * 1050f)) * 0.48f, 0.10f, 0.60f);
        float foodDays = foodKg / Math.Max(0.20f, safeCrew * 0.62f * (1.0f - algalFoodSup));
        float waterDays = waterL / Math.Max(0.10f, safeCrew * 3.54f * Math.Max(0.008f, 1.0f - totalWaterLoop));
        float medDays = (medsKg / Math.Max(0.018f, safeCrew * 0.040f * Math.Max(0.50f, rad * (1.0f - crewBuff * 0.55f)))) * (1.0f + Math.Min(0.25f, commsGbps * 0.012f) - commDelayPenalty * 0.35f);
        float suitDays = Math.Max(15f, (suitPct - 25.0f) / Math.Max(0.065f, (0.15f + log10Dist * 0.032f) * (1.0f - crewBuff * 0.45f) * toolFactor));

        float logDays = Math.Min(Math.Min(foodDays, waterDays), Math.Min(medDays, suitDays));
        float stayDays = Math.Clamp(((entropyDays * 0.55f) + (Math.Min(entropyDays * 1.35f, logDays) * 0.45f)) * massEff, 12f, 720f);

        float gravHealth = Math.Clamp((surfG / 9.81f) + artG + 0.35f, 0.35f, 1.0f);
        float medRatio = Math.Clamp(medsKg / (safeCrew * 35f), 0.30f, 1.25f);
        float healthProb = Math.Clamp(0.52f + (crewBuff * 0.22f) + (bnnAtten * 0.14f) + (gravHealth * 0.10f) + (Math.Min(1.0f, medRatio) * 0.08f) - (radPenalty * 0.65f) - (entropy * 0.12f), 0.18f, 0.996f);

        float whippleFactor = Math.Clamp(whipple / 6.0f, 0.15f, 1.0f);
        float reliability = Math.Clamp(0.45f + (eclssSyn * 0.20f) + ((suitPct / 100f) * 0.16f) + ((1.0f - toolFactor) * 0.22f) + (whippleFactor * 0.08f) + (Math.Min(1.0f, thermalRatio) * 0.08f) - (commDelayPenalty * 0.16f) - (entropy * 0.18f), 0.20f, 0.995f);

        return (yieldRate, stayDays, healthProb, reliability);
    }
}
