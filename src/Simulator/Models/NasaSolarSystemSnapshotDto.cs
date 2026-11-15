namespace Simulator.Models;

public sealed class NasaSolarSystemSnapshotDto
{
    public string LatestSolarFlareClass { get; set; } = string.Empty;
    public float BaseSolarWeatherMultiplier { get; set; }
    public Dictionary<string, double> LiveDistancesFromEarthKm { get; set; } = new();
}
