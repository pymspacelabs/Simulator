using System.Text;
using BioshipSimulator.Models;
using BioshipSimulator.Simulation;
using Spectre.Console;

namespace BioshipSimulator.UI;

public static class TuiDashboardRenderer
{
    public static void UpdateGuiLayout(Layout layout, BioShipEngine ship, bool isPaused)
    {
        var headerGrid = new Grid().AddColumn().AddColumn(new GridColumn().RightAligned());
        headerGrid.AddRow(
            $"[bold white]🚀 BIOSHIP 240K QUAD-ML.NET (36 FEATURES)[/] | [bold SpringGreen1]{Markup.Escape(ship.Profile.DisplayNameEn)}[/]",
            $"[grey]Day:[/] [bold yellow]{ship.MissionDay:F0}/{ship.Plan.TotalCampaignDays:F0}d[/] | [grey]Mass:[/] [bold cyan1]{ship.TotalShipMassTons:F1}t[/] | [grey]Preset:[/] [bold gold1]{Markup.Escape(ship.ActiveEngineeringPresetName)}[/]"
        );
        layout["Header"].Update(new Panel(headerGrid).Border(BoxBorder.Rounded).BorderColor(Color.Cyan1));

        var barsTable = new Table().Border(TableBorder.None).HideHeaders().Expand();
        barsTable.AddColumn("Subsystem", c => c.Width(27));
        barsTable.AddColumn("ANSI Thin Bar");
        barsTable.AddColumn("Value", c => c.RightAligned().Width(15));

        barsTable.AddRow("[SpringGreen1]🌿 Chlorella + APH Biomass[/]", RenderThinAnsiBar(ship.OrganicBiomass, 100f, 26, "SpringGreen1", "yellow", "red"), $"[bold SpringGreen1]{ship.OrganicBiomass,6:F1}%[/]");
        barsTable.AddRow("[cyan1]⚡ Power + Li-S Bank[/]", RenderThinAnsiBar(ship.ElectricalPowerKw, 260f, 26, "cyan1", "deepskyblue1", "red"), $"[bold cyan1]{ship.ElectricalPowerKw,5:F0} kWe[/]");
        barsTable.AddRow("[deepskyblue1]💧 WPA+BPA Water Loop[/]", RenderThinAnsiBar(ship.WaterRecoveryEfficiency * 100f, 100f, 26, "deepskyblue1", "yellow", "red"), $"[bold deepskyblue1]{ship.WaterRecoveryEfficiency * 100f,6:F1}%[/]");
        barsTable.AddRow("[mediumorchid1]💨 OGA/CDRA/Bosch Loop[/]", RenderThinAnsiBar(ship.EclssSynergyIndex * 100f, 100f, 26, "mediumorchid1", "orange1", "red"), $"[bold mediumorchid1]{ship.EclssSynergyIndex * 100f,6:F1}%[/]");
        barsTable.AddRow("[SpringGreen2]🩺 ML Crew Health Prob[/]", RenderThinAnsiBar(ship.PredictedCrewHealthProb * 100f, 100f, 26, "SpringGreen2", "yellow", "red"), $"[bold SpringGreen2]{ship.PredictedCrewHealthProb * 100f,6:F1}%[/]");
        barsTable.AddRow("[aqua]🔧 ML System Reliability[/]", RenderThinAnsiBar(ship.PredictedSystemReliability * 100f, 100f, 26, "aqua", "orange1", "red"), $"[bold aqua]{ship.PredictedSystemReliability * 100f,6:F1}%[/]");
        barsTable.AddRow("[orange1]🔥 Bio-CH₄ Fuel Reserve[/]", RenderThinAnsiBar(ship.BioFuelReserve, 100f, 26, "orange1", "gold1", "red"), $"[bold orange1]{ship.BioFuelReserve,6:F1}%[/]");
        barsTable.AddRow("[gold1]⚗ ML Sabatier/MPA Yield[/]", RenderThinAnsiBar(ship.PredictedFuelYieldRate * 100f, 100f, 26, "gold1", "orange1", "red"), $"[bold gold1]{ship.PredictedFuelYieldRate * 100f,6:F1}%[/]");
        barsTable.AddRow("[red1]☣ Membrane Entropy[/]", RenderThinAnsiBar(ship.EntropyAccumulated * 100f, 100f, 26, "red1", "orange1", "SpringGreen1"), $"[bold red1]{ship.EntropyAccumulated * 100f,6:F1}%[/]");

        layout["Bars"].Update(
            new Panel(barsTable)
                .Header($"[bold white] 🧬 Quad-Ensemble Telemetry — {Markup.Escape(ship.CurrentStageTitle)} [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.SpringGreen2)
        );

        double progressRatio = Math.Clamp(ship.DistanceFromEarthKm / Math.Max(1.0, ship.CurrentTargetDistanceKm), 0.0, 1.0);
        string trajectoryBar = RenderTrajectoryThinTrack(progressRatio, 36, ship.CurrentStage >= FlightStage.SafeRateLimitReturnBurn);

        var trajGrid = new Grid().AddColumn();
        trajGrid.AddRow($"[bold cyan1]🌍 EARTH[/]  {trajectoryBar}  [bold gold1]🪐 {Markup.Escape(ship.Profile.ShortLabelEn)} ({ship.FormatDistance(ship.CurrentTargetDistanceKm)})[/]");
        trajGrid.AddRow($"[grey]Shielding:[/] [bold white]{ship.RadShieldingGcm2 + ship.OutpostShieldingGcm2Delta:F0} g/cm²[/] | [grey]Outpost Watts:[/] [bold cyan1]{ship.OutpostWattBudgetDelta:+0;-0;0} W[/] | [grey]Outpost O2:[/] [bold SpringGreen1]{ship.OutpostOxygenLitersPerDayDelta:+0;-0;0} L/d[/] | [grey]Delay:[/] [bold gold1]{ship.CommunicationDelaySec:F1}s[/]");
        trajGrid.AddRow($"[grey]Outbound:[/] [bold white]{ship.Plan.OutboundTransitDays:F1}d[/] | [grey]Target Stay (ML):[/] [bold SpringGreen1]{ship.Plan.MaxExplorationStayDays:F1}d[/] | [grey]Return:[/] [bold cyan1]{ship.Plan.ReturnTransitDays:F1}d[/]");
        trajGrid.AddRow($"[grey]Safe Rate-Limit:[/] [bold orange1]Yield <= {ship.Profile.SafeReturnYieldLimit * 100f:F0}% or Fuel <= {ship.Plan.SafeReturnFuelReservePercent:F1}% or Reliability <= 42%[/]");

        layout["Trajectory"].Update(
            new Panel(trajGrid)
                .Header("[bold white] 🛰 Outpost Architectural Parameters (Watts / Liters O2 / Grams Shielding / Comms Delay) [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.DeepSkyBlue1)
        );

        var nasaMlTable = new Table().Border(TableBorder.Simple).Expand();
        nasaMlTable.AddColumn("[bold cyan1]NASA API / Quad-ML Output[/]");
        nasaMlTable.AddColumn("[bold white]Live Value[/]", c => c.RightAligned());

        nasaMlTable.AddRow("[grey]1. DONKI Space Weather[/]", $"[orange1]{Markup.Escape(ship.Snapshot.LatestSolarFlareClass)} ({ship.EffectiveRadiationIndex:F2}x rad)[/]");
        nasaMlTable.AddRow("[grey]2. JPL Horizons & TLE[/]", $"[cyan1]COMMAND='{ship.Profile.JplHorizonsCommand}' | ISS {ship.Snapshot.IssLiveAltitudeKm:F1} km[/]");
        nasaMlTable.AddRow("[grey]3. NASA OSDR & NTRS ECLSS[/]", $"[SpringGreen1]{ship.Snapshot.EclssPatentsAndReportsCount} MSFC/NTRS records[/]");
        nasaMlTable.AddRow("[grey]4. Mars InSight / Artemis[/]", $"[gold1]{Markup.Escape(ship.Snapshot.MarsSurfaceSummary)}[/]");
        nasaMlTable.AddRow("[bold mediumorchid1]🧠 ML #1 Fuel Yield Rate[/]", $"[bold gold1]{ship.PredictedFuelYieldRate * 100f:F1}%[/]");
        nasaMlTable.AddRow("[bold mediumorchid1]🧠 ML #2 Max Stay Days[/]", $"[bold SpringGreen1]{ship.Plan.MaxExplorationStayDays:F1} days ({ship.Plan.MaxExplorationStayDays / 30f:F1} mo)[/]");
        nasaMlTable.AddRow("[bold mediumorchid1]🧠 ML #3 Crew Health Prob[/]", $"[bold cyan1]{ship.PredictedCrewHealthProb * 100f:F1}%[/]");
        nasaMlTable.AddRow("[bold mediumorchid1]🧠 ML #4 System Reliability[/]", $"[bold aqua]{ship.PredictedSystemReliability * 100f:F1}%[/]");

        layout["NasaAndMl"].Update(
            new Panel(nasaMlTable)
                .Header("[bold white] 📡 8 NASA Open APIs & 240k Quad-FastTree [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.MediumOrchid1)
        );

        var logLines = new Markup(string.Join("", ship.RecentLogs));
        layout["Logs"].Update(
            new Panel(logLines)
                .Header($"[bold white] 📋 Mission Log ({(ship.IsPtBr ? "pt-BR" : "en-US")}) [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey)
        );

        var footerMarkup = new Markup(
            "[bold gold1][[1]][/] ISS  [bold gold1][[2]][/] Moon  [bold gold1][[3]][/] Mars  " +
            "[bold SpringGreen1][[C]][/] Cycle Preset  " +
            "[bold SpringGreen1][[+/-]][/] Crew  " +
            "[bold cyan1][[↑/↓]][/] Sabatier  " +
            "[bold SpringGreen1][[P]][/] Repair/Purge  " +
            "[bold red1][[Q]][/] Quit"
        );
        layout["Footer"].Update(new Panel(Align.Center(footerMarkup)).Border(BoxBorder.Rounded).BorderColor(Color.Grey39));
    }

    private static string RenderThinAnsiBar(float value, float max, int width, string highColor, string midColor, string lowColor)
    {
        float ratio = Math.Clamp(value / max, 0f, 1f);
        string activeColor = ratio > 0.55f ? highColor : (ratio > 0.28f ? midColor : lowColor);
        int filledSegments = (int)Math.Round(ratio * width);
        int emptySegments = Math.Max(0, width - filledSegments);
        return $"[{activeColor}]{new string('━', filledSegments)}[/][grey27]{new string('─', emptySegments)}[/]";
    }

    private static string RenderTrajectoryThinTrack(double ratio, int width, bool isReturning)
    {
        int shipPos = (int)Math.Round(ratio * (width - 1));
        var sb = new StringBuilder();
        string shipIcon = isReturning ? "[bold orange1]◄🚀[/]" : "[bold cyan1]🚀►[/]";

        for (int i = 0; i < width; i++)
        {
            if (i == shipPos) sb.Append(shipIcon);
            else if (i < shipPos) sb.Append("[cyan1]━[/]");
            else sb.Append("[grey30]─[/]");
        }
        return sb.ToString();
    }
}
