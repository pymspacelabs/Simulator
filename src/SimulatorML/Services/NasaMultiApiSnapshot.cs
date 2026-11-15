namespace SimulatorML.Services;

public sealed class NasaMultiApiSnapshot
{
    public string ApiSourceStatus { get; set; } = "8 NASA Public APIs (DONKI + Horizons + OSDR + TechPort + TechTransfer + NTRS + InSight + TLE)";
    public string LatestSolarFlareClass { get; set; } = "M1.4 (Moderate)";
    public float BaseSolarWeatherMultiplier { get; set; } = 1.15f;
    public int ActiveSpaceWeatherEvents { get; set; } = 3;
    public double IssLiveAltitudeKm { get; set; } = 414.8;
    public int OsdrAlgaeStudiesCount { get; set; } = 28;
    public string OsdrReferenceStudy { get; set; } = "OSD-521 (ISS Chlorella, Bone Atrophy & Immune Study)";
    public string ArtemisTechPortProject { get; set; } = "TechPort #182369 (Artemis xEMU Suit & Dust Array)";
    public int EclssPatentsAndReportsCount { get; set; } = 148;
    public string MarsSurfaceSummary { get; set; } = "-62.5°C | 745 Pa CO2 (95.3%) | ClO4- 0.6%";
    public Dictionary<string, double> LiveDistancesFromEarthKm { get; set; } = new();
}
