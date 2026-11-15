using MudBlazor;

namespace Simulator.Models;

public sealed class TrajectoryScenario
{
    public string ShortTabTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public Color BadgeColor { get; set; } = Color.Primary;
    public string HeaderIcon { get; set; } = Icons.Material.Filled.Rocket;
    public float MaxTrajectoryDays { get; set; }
    public int MaxCrewCapacity { get; set; }
    public string CrewSurvivalStatus { get; set; } = string.Empty;
    public float FinalMlYieldPercent { get; set; }
    public float FinalRecyclingPercent { get; set; }
    public List<TimelineStageStep> Steps { get; set; } = new();
}
