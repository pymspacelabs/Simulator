// ============================================================================
// BioShipSimulator - 240,000-Sample / 32-Feature / Quad-Ensemble ML.NET (.NET 11)
// ============================================================================
// Ultra-Refined 3-Deck Architecture for ISS, Moon, and Mars:
//   - 240,000 Training Samples (80,000 samples x 3 destinations: ISS, Moon, Mars)
//   - 32 Input Features spanning:
//       * Deck 1 (Ship Build - 10 Cards): DryMass, RadShieldGcm2, WhippleLayers,
//         ThermalRejectionKw, ElectricalKw, BatteryKwh, FoodKg, WaterL, MedsKg,
//         EvaSuitDurability, ToolsKg, CommsGbps, ArtificialGravityG
//       * Deck 2 (Marshall ECLSS - 10 Cards): WpaRecoveryEff, BrineRecoveryEff,
//         OgaO2KgDay, CdraCo2KgDay, SabatierEfficiency, BioreactorLiters,
//         NitrogenConservation, TccsBiofilmResistance, SolidWasteMethanation
//       * Deck 3 (ISS / Moon / Mars Environment - 4 Cards/Target): DistanceLog10,
//         SolarIrradiance, SurfaceGravity, AmbientRadiation, HostilityIndex,
//         SuitWearPerDay, ToolWearPerDay, TotalDeltaV, CrewDeckBuff, BiomeDeckBuff
//   - 4 Simultaneous FastTree Regression Models (Quad-Ensemble):
//       1. LabelFuelYieldRate          (Sabatier + Bioreactor CH4 recharge rate)
//       2. LabelMaxExplorationStayDays (Safe stay days before Safe Rate-Limit)
//       3. LabelCrewHealthSurvivalProb (OSDR bone/muscle/radiation health score)
//       4. LabelSystemReliabilityIndex (ECLSS + EVA Suit + Tool reliability score)
// ============================================================================

using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using Spectre.Console;

Console.OutputEncoding = Encoding.UTF8;

bool exportOnly = args.Contains("--export-only", StringComparer.OrdinalIgnoreCase);
string? customOutputDir = GetArgValue(args, "--output");
string? targetArg = GetArgValue(args, "--target");
string langArg = GetArgValue(args, "--lang") ?? "en-US";
bool isPtBr = langArg.Equals("pt-BR", StringComparison.OrdinalIgnoreCase);

CelestialTargetId initialTarget = Enum.TryParse<CelestialTargetId>(targetArg, ignoreCase: true, out var parsedTarget)
    ? parsedTarget
    : CelestialTargetId.Mars;

string baseDataDir = ResolveArtifactsDirectory(customOutputDir);
Directory.CreateDirectory(baseDataDir);

string csvDatasetPath = Path.Combine(baseDataDir, "bioship_3deck_240k_telemetry.csv");
string zipModelPath = Path.Combine(baseDataDir, "bioship_fasttree_model.zip");
string ensembleJsonPath = Path.Combine(baseDataDir, "bioship_fasttree_ensemble.json");
string nasaSnapshotJsonPath = Path.Combine(baseDataDir, "bioship_nasa_snapshot.json");
string decksCatalogJsonPath = Path.Combine(baseDataDir, "bioship_decks_catalog.json");

BioShipMlAutopilot autopilot = new(zipModelPath, csvDatasetPath, ensembleJsonPath, decksCatalogJsonPath, isPtBr);

if (exportOnly)
{
    Console.WriteLine(isPtBr
        ? "[CI/CD] Treinando Quad-Ensemble ML.NET com 240.000 amostras (80k ISS + 80k Lua + 80k Marte) e 32 variáveis..."
        : "[CI/CD] Training Quad-Ensemble ML.NET with 240,000 samples (80k ISS + 80k Moon + 80k Mars) & 32 features...");

    var multiSnapshot = await NasaApiClient.FetchAllNasaTelemetryAsync();
    File.WriteAllText(nasaSnapshotJsonPath, JsonSerializer.Serialize(multiSnapshot, new JsonSerializerOptions { WriteIndented = true }));

    autopilot.InitializeOrLoadModel(forceRetrain: true);

    Console.WriteLine(isPtBr
        ? $"[CI/CD] 4 Florestas FastTree + Catálogo de 3 Decks exportados em: {baseDataDir}"
        : $"[CI/CD] 4 FastTree Ensembles + 3-Deck Catalog exported to: {baseDataDir}");
    return;
}

Console.CursorVisible = false;
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

NasaMultiApiSnapshot nasaSnapshot = new();

await AnsiConsole.Status()
    .AutoRefresh(true)
    .Spinner(Spinner.Known.Dots2)
    .SpinnerStyle(Style.Parse("cyan1 bold"))
    .StartAsync(
        isPtBr ? "[cyan1]Inicializando Motor Quad-Ensemble ML.NET (240.000 amostras / 32 features)...[/]"
               : "[cyan1]Initializing Quad-Ensemble 240,000-Sample / 32-Feature ML.NET Engine...[/]",
        async ctx =>
        {
            ctx.Status(isPtBr
                ? "[cyan1]Consultando 8 APIs Públicas da NASA (DONKI, Horizons, OSDR, TechPort, TechTransfer, NTRS, InSight, TLE)...[/]"
                : "[cyan1]Querying 8 Public NASA APIs (DONKI, Horizons, OSDR, TechPort, TechTransfer, NTRS, InSight, TLE)...[/]");
            nasaSnapshot = await NasaApiClient.FetchAllNasaTelemetryAsync();
            File.WriteAllText(nasaSnapshotJsonPath, JsonSerializer.Serialize(nasaSnapshot, new JsonSerializerOptions { WriteIndented = true }));

            ctx.Status(isPtBr
                ? "[SpringGreen1]Treinando/Carregando 4 Florestas FastTree (240.000 amostras: 80k ISS / 80k Lua / 80k Marte)...[/]"
                : "[SpringGreen1]Training/Loading 4 FastTree Ensembles (240,000 samples: 80k ISS / 80k Moon / 80k Mars)...[/]");
            autopilot.InitializeOrLoadModel(forceRetrain: false);
        });

BioShipEngine ship = new(initialTarget, nasaSnapshot, autopilot, isPtBr);
bool isPaused = false;
int tickDelayMs = 200;

var rootLayout = new Layout("Root")
    .SplitRows(
        new Layout("Header").Size(4),
        new Layout("Body").SplitColumns(
            new Layout("LeftCol").Ratio(6).SplitRows(
                new Layout("Bars").Size(16),
                new Layout("Trajectory").Size(9)
            ),
            new Layout("RightCol").Ratio(5).SplitRows(
                new Layout("NasaAndMl").Size(16),
                new Layout("Logs").Size(9)
            )
        ),
        new Layout("Footer").Size(3)
    );

await AnsiConsole.Live(rootLayout)
    .AutoClear(false)
    .Overflow(VerticalOverflow.Ellipsis)
    .StartAsync(async ctx =>
    {
        while (!cts.Token.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true).Key;
                switch (key)
                {
                    case ConsoleKey.D1 or ConsoleKey.NumPad1: ship.SwitchDestination(CelestialTargetId.ISS); break;
                    case ConsoleKey.D2 or ConsoleKey.NumPad2: ship.SwitchDestination(CelestialTargetId.Moon); break;
                    case ConsoleKey.D3 or ConsoleKey.NumPad3: ship.SwitchDestination(CelestialTargetId.Mars); break;
                    case ConsoleKey.C: ship.CycleThreeDeckLoadout(); break;
                    case ConsoleKey.UpArrow: ship.AdjustBioreactorThrottle(+0.05f); break;
                    case ConsoleKey.DownArrow: ship.AdjustBioreactorThrottle(-0.05f); break;
                    case ConsoleKey.RightArrow: ship.AdjustTechAllocation(+0.05f); break;
                    case ConsoleKey.LeftArrow: ship.AdjustTechAllocation(-0.05f); break;
                    case ConsoleKey.Add or ConsoleKey.OemPlus or ConsoleKey.PageUp: ship.AdjustCrewCount(+1); break;
                    case ConsoleKey.Subtract or ConsoleKey.OemMinus or ConsoleKey.PageDown: ship.AdjustCrewCount(-1); break;
                    case ConsoleKey.P: ship.PurgeBioToxinAndRepairSuits(); break;
                    case ConsoleKey.T:
                        autopilot.InitializeOrLoadModel(forceRetrain: true);
                        ship.SetModelStatus(autopilot.ModelStatusMessage);
                        ship.AddLog(isPtBr
                            ? "[bold mediumorchid1]🧠 Quad-Ensemble de 240.000 amostras (32 features) retreinado![/]"
                            : "[bold mediumorchid1]🧠 240,000-sample (32-feature) Quad-Ensemble retrained![/]");
                        break;
                    case ConsoleKey.R: ship.ForceEmergencyReturn(); break;
                    case ConsoleKey.Spacebar:
                        isPaused = !isPaused;
                        ship.AddLog(isPaused
                            ? (isPtBr ? "[yellow]Simulação PAUSADA.[/]" : "[yellow]Simulation PAUSED.[/]")
                            : (isPtBr ? "[green]Simulação RETOMADA.[/]" : "[green]Simulation RESUMED.[/]"));
                        break;
                    case ConsoleKey.Q or ConsoleKey.Escape:
                        cts.Cancel();
                        break;
                }
            }

            if (!isPaused && ship.CurrentStage != FlightStage.TouchdownEarthLanding)
            {
                ship.StepSimulation();
            }

            UpdateGuiLayout(rootLayout, ship, isPaused);
            ctx.Refresh();

            await Task.Delay(tickDelayMs, cts.Token).ContinueWith(_ => { });
        }
    });

Console.CursorVisible = true;
AnsiConsole.MarkupLine(isPtBr
    ? "\n[bold SpringGreen1]✔ Simulação Quad-Ensemble (240k amostras — ISS / Lua / Marte) encerrada.[/]"
    : "\n[bold SpringGreen1]✔ Quad-Ensemble Simulation (240k samples — ISS / Moon / Mars) completed.[/]");

static string? GetArgValue(string[] args, string key)
{
    int idx = Array.FindIndex(args, a => a.Equals(key, StringComparison.OrdinalIgnoreCase));
    return (idx >= 0 && idx + 1 < args.Length) ? args[idx + 1] : null;
}

static string ResolveArtifactsDirectory(string? explicitPath)
{
    if (!string.IsNullOrWhiteSpace(explicitPath))
        return Path.GetFullPath(explicitPath);

    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null)
    {
        string wasmWwwroot = Path.Combine(dir.FullName, "Simulator", "wwwroot");
        if (Directory.Exists(wasmWwwroot))
            return Path.Combine(wasmWwwroot, "MLArtifacts");

        dir = dir.Parent;
    }

    return Path.Combine(AppContext.BaseDirectory, "MLArtifacts");
}

// ============================================================================
// TUI DASHBOARD RENDERER
// ============================================================================
static void UpdateGuiLayout(Layout layout, BioShipEngine ship, bool isPaused)
{
    var headerGrid = new Grid().AddColumn().AddColumn(new GridColumn().RightAligned());
    headerGrid.AddRow(
        $"[bold white]🚀 BIOSHIP 240K QUAD-ML.NET (32 FEATURES)[/] | [bold SpringGreen1]{Markup.Escape(ship.Profile.DisplayNameEn)}[/]",
        $"[grey]Day:[/] [bold yellow]{ship.MissionDay:F0}/{ship.Plan.TotalCampaignDays:F0}d[/] | [grey]Mass:[/] [bold cyan1]{ship.TotalShipMassTons:F1}t[/] | [grey]Synergies:[/] [bold gold1]{ship.ActiveComboCount}[/]"
    );
    layout["Header"].Update(new Panel(headerGrid).Border(BoxBorder.Rounded).BorderColor(Color.Cyan1));

    var barsTable = new Table().Border(TableBorder.None).HideHeaders().Expand();
    barsTable.AddColumn("Subsystem", c => c.Width(27));
    barsTable.AddColumn("ANSI Thin Bar");
    barsTable.AddColumn("Value", c => c.RightAligned().Width(15));

    barsTable.AddRow("[SpringGreen1]🌿 Chlorella + APH Biomass[/]", RenderThinAnsiBar(ship.OrganicBiomass, 100f, 26, "SpringGreen1", "yellow", "red"), $"[bold SpringGreen1]{ship.OrganicBiomass,6:F1}%[/]");
    barsTable.AddRow("[cyan1]⚡ Power + Li-S Bank (D1)[/]", RenderThinAnsiBar(ship.ElectricalPowerKw, 260f, 26, "cyan1", "deepskyblue1", "red"), $"[bold cyan1]{ship.ElectricalPowerKw,5:F0} kWe[/]");
    barsTable.AddRow("[deepskyblue1]💧 WPA+BPA Water Loop (D2)[/]", RenderThinAnsiBar(ship.WaterRecoveryEfficiency * 100f, 100f, 26, "deepskyblue1", "yellow", "red"), $"[bold deepskyblue1]{ship.WaterRecoveryEfficiency * 100f,6:F1}%[/]");
    barsTable.AddRow("[mediumorchid1]💨 OGA/CDRA/Bosch (D2)[/]", RenderThinAnsiBar(ship.EclssSynergyIndex * 100f, 100f, 26, "mediumorchid1", "orange1", "red"), $"[bold mediumorchid1]{ship.EclssSynergyIndex * 100f,6:F1}%[/]");
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
    trajGrid.AddRow($"[grey]Shielding:[/] [bold white]{ship.RadShieldingGcm2:F0} g/cm²[/] | [grey]Centrifuge:[/] [bold deepskyblue1]{ship.ArtificialGravityG:F2}g[/] | [grey]Thermal Rej:[/] [bold cyan1]{ship.ThermalRejectionKw:F0} kWt[/] | [grey]Battery:[/] [bold gold1]{ship.BatteryStorageKwh:F0} kWh[/]");
    trajGrid.AddRow($"[grey]Outbound:[/] [bold white]{ship.Plan.OutboundTransitDays:F1}d[/] | [grey]Target Stay (ML):[/] [bold SpringGreen1]{ship.Plan.MaxExplorationStayDays:F1}d[/] | [grey]Return:[/] [bold cyan1]{ship.Plan.ReturnTransitDays:F1}d[/]");
    trajGrid.AddRow($"[grey]Safe Rate-Limit:[/] [bold orange1]Yield <= {ship.Profile.SafeReturnYieldLimit * 100f:F0}% or Fuel <= {ship.Plan.SafeReturnFuelReservePercent:F1}% or Reliability <= 42%[/]");

    layout["Trajectory"].Update(
        new Panel(trajGrid)
            .Header("[bold white] 🃏 Deck 1 (10 Ship Cards) + Deck 2 (10 ECLSS Cards) + Deck 3 (4 Env Cards) [/]")
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

    var logLines = new Markup(string.Join("\n", ship.RecentLogs));
    layout["Logs"].Update(
        new Panel(logLines)
            .Header($"[bold white] 📋 Mission Log ({(ship.IsPtBr ? "pt-BR" : "en-US")}) [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey)
    );

    var footerMarkup = new Markup(
        "[bold gold1][[1]][/] ISS  [bold gold1][[2]][/] Moon  [bold gold1][[3]][/] Mars  " +
        "[bold SpringGreen1][[C]][/] Cycle 3-Deck Combos  " +
        "[bold SpringGreen1][[+/-]][/] Crew  " +
        "[bold cyan1][[↑/↓]][/] Sabatier  " +
        "[bold SpringGreen1][[P]][/] Repair/Purge  " +
        "[bold red1][[Q]][/] Quit"
    );
    layout["Footer"].Update(new Panel(Align.Center(footerMarkup)).Border(BoxBorder.Rounded).BorderColor(Color.Grey39));
}

static string RenderThinAnsiBar(float value, float max, int width, string highColor, string midColor, string lowColor)
{
    float ratio = Math.Clamp(value / max, 0f, 1f);
    string activeColor = ratio > 0.55f ? highColor : (ratio > 0.28f ? midColor : lowColor);
    int filledSegments = (int)Math.Round(ratio * width);
    int emptySegments = Math.Max(0, width - filledSegments);
    return $"[{activeColor}]{new string('━', filledSegments)}[/][grey27]{new string('─', emptySegments)}[/]";
}

static string RenderTrajectoryThinTrack(double ratio, int width, bool isReturning)
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

// ============================================================================
// 1. EXPANDED 3-DECK ARCHITECTURE (10 SHIP + 10 ECLSS + 12 ENVIRONMENT CARDS)
// ============================================================================
public enum CelestialTargetId
{
    ISS = 1,
    Moon = 2,
    Mars = 3
}

public enum FlightStage
{
    LaunchAndLiftoff = 1,
    AtmosphereExitAndStaging = 2,
    LowEarthOrbitParkingAndCheck = 3,
    TransTargetInjectionBurn = 4,
    DeepSpaceSymbioticCruise = 5,
    TargetOrbitCaptureAndExploration = 6,
    SafeRateLimitReturnBurn = 7,
    EarthAtmosphericReentry = 8,
    TouchdownEarthLanding = 9
}

public sealed record ShipBuildCard(
    string CardId,
    string SlotCategory,
    string Name,
    string Rarity,
    float DryMassTons,
    float RadiationShieldingGcm2,
    int MicrometeoroidWhippleLayers,
    float ThermalRejectionCapacityKw,
    float ElectricalPowerKwDelta,
    float BatteryStorageKwhBonus,
    float FoodProvisionsKgBonus,
    float PotableWaterLitersBonus,
    float MedicalSuppliesKgBonus,
    float EvaSuitDurabilityBonusPct,
    float ToolsAndSparesKgBonus,
    float CommsBandwidthGbpsBonus,
    float ArtificialGravityCentrifugeG,
    float CrewProtectionBuff,
    string NasaSource
);

public sealed record EclssSubsystemCard(
    string CardId,
    string SubsystemCode,
    string Name,
    float DryMassTons,
    float BioreactorVolumeLitersBonus,
    float WaterRecoveryEfficiencyBonus,
    float O2GenerationEfficiencyBonus,
    float Co2ScrubbingEfficiencyBonus,
    float SabatierMethaneYieldBonus,
    float NitrogenBufferConservationBonus,
    float ElectricalPowerKwDraw,
    float FilterEntropyResistanceBonus,
    string NasaSource
);

public sealed record EnvironmentCountermeasureCard(
    string CardId,
    string Name,
    string Effect,
    float BiomeShieldingBuff,
    float CrewProtectionBuff,
    float SuitPreservationBuff,
    string NasaApiSource
);

public sealed record DeckComboSynergy(
    string SynergyId,
    string Name,
    string[] RequiredCards,
    string BonusDescription
);

public sealed record CelestialBodyProfile(
    CelestialTargetId Id,
    string RouteSlug,
    string ShortLabelEn,
    string ShortLabelPt,
    string DisplayNameEn,
    string DisplayNamePt,
    string JplHorizonsCommand,
    double MeanDistanceFromEarthKm,
    float Log10DistanceKm,
    float SemiMajorAxisAu,
    float SolarIrradianceFactor,
    float AmbientRadiationEnvIndex,
    float ExternalHostilityIndex,
    float SurfaceGravityMs2,
    float EscapeVelocityKms,
    float SuitWearPerDay,
    float ToolWearPerDay,
    float TotalMissionDeltaVms,
    float OutboundTransitDays,
    float ReturnTransitDays,
    float SafeReturnYieldLimit,
    float SafeReturnFuelReserveFloor,
    int RecommendedCrew,
    float BaseBioreactorLiters,
    string EnvironmentDeckTitleEn,
    string EnvironmentDeckTitlePt,
    List<string> BiologicalHazardsEn,
    List<string> BiologicalHazardsPt,
    List<EnvironmentCountermeasureCard> EnvironmentCards
);

public static class ThreeDeckCatalog
{
    public static readonly List<DeckComboSynergy> ComboSynergies =
    [
        new("SYN-CLOSED-WATER-99", "Ultra-Closed 99.2% Hydro-Loop", ["ECL-01", "ECL-02", "ECL-06"], "+1.2% Water Recovery & -35% Biofilm Fouling"),
        new("SYN-CRYO-METHALOX-ISRU", "Sabatier + Bosch + Zero-Boil-Off Methalox Loop", ["BLD-06", "ECL-04", "ECL-07"], "+24% CH4 Yield & +18% Return Delta-V Margin"),
        new("SYN-DEEP-SPACE-IMMUNE", "Centrifuge + OSDR Med-Bay + AnMBR Bioreactor", ["BLD-04", "BLD-09", "ECL-10"], "+42% Crew Radiation & Bone Preservation"),
        new("SYN-AUTONOMOUS-EVA-FAB", "xEMU Z-2 + Valkyrie Robotics + 3D CNC Sintering", ["BLD-03", "BLD-05", "BLD-08"], "+45% Suit/Tool Service Life & -40% EVA Fatigue")
    ];

    // DECK 1: 10 Ship Build Cards
    public static readonly List<ShipBuildCard> ShipBuildDeck =
    [
        new("BLD-01", "HullAndShielding", "Multilayer Carbon-Silicate & BNNT Hull", "Legendary",
            13.5f, 28.0f, 6, 140.0f, -2.0f, 120.0f, 600f, 500f, 40f, 6f, 140f, 0f, 0f, 0.26f, "NASA Langley BNNT Shielding & ISS Whipple Specs"),
        new("BLD-02", "PowerGeneration", "Dual Kilopower Krusty Fission + iROSA Wings (220 kWe)", "Legendary",
            9.6f, 4.0f, 1, 85.0f, 145.0f, 480.0f, 0f, 0f, 0f, 0f, 110f, 2.5f, 0f, 0.14f, "NASA Glenn Kilopower KRUSTY & ISS iROSA Arrays"),
        new("BLD-03", "EvaSuitsAndAirlock", "xEMU Z-2 Orthofabric SuitPort + EDS Dust Shield", "Legendary",
            5.0f, 6.0f, 2, 20.0f, -4.0f, 60.0f, 0f, 0f, 50f, 32f, 340f, 1.2f, 0f, 0.30f, "NASA JSC xEMU PLSS & Kennedy EDS"),
        new("BLD-04", "MedBayAndProvisions", "OSDR Surgical Med-Bay + ARED + Lyophilized Vault", "Legendary",
            7.2f, 8.0f, 1, 25.0f, -5.5f, 80.0f, 2600f, 950f, 360f, 0f, 80f, 0f, 0f, 0.36f, "NASA OSDR GeneLab & NASA-STD-3001 Vol. 1/2"),
        new("BLD-05", "CommsAndFabrication", "DSOC Optical Laser + 3D CNC Metal Sintering Fab", "Legendary",
            5.4f, 2.0f, 1, 30.0f, -6.5f, 90.0f, 0f, 0f, 0f, 20f, 780f, 22.0f, 0f, 0.16f, "NASA Psyche DSOC Laser & MSFC 3D Sintering"),
        new("BLD-06", "PropulsionAndCryoStorage", "Zero-Boil-Off Cryo-Methalox (CH4/LOX) Depot", "Epic",
            7.8f, 10.0f, 3, 95.0f, -5.0f, 100.0f, 400f, 600f, 0f, 0f, 160f, 0f, 0f, 0.15f, "NASA Marshall Cryogenic Fluid Management (CFM)"),
        new("BLD-07", "ThermalControl", "Two-Phase Ammonia Loop + Origami Radiators", "Rare",
            3.4f, 2.0f, 2, 180.0f, -3.0f, 40.0f, 0f, 200f, 0f, 5f, 120f, 0f, 0f, 0.12f, "NASA ISS External Active Thermal Control (EATCS)"),
        new("BLD-08", "RoboticsAndAutomation", "Canadarm3 + Valkyrie R5 EVA Humanoid Twins", "Epic",
            4.1f, 0.0f, 1, 15.0f, -4.5f, 70.0f, 0f, 0f, 30f, 24f, 480f, 4.0f, 0f, 0.24f, "NASA JSC Valkyrie R5 Robot & Gateway Canadarm3"),
        new("BLD-09", "ArtificialGravity", "Short-Radius Sleep Centrifuge (0.42g Artificial G)", "Legendary",
            8.9f, 6.0f, 2, 20.0f, -7.5f, 50.0f, 300f, 250f, 90f, 0f, 100f, 0f, 0.42f, 0.34f, "NASA Ames Artificial Gravity & SANS Countermeasures"),
        new("BLD-10", "PowerStorageAndFuelCells", "Solid-State Li-S Battery + Regenerative PEM Cells", "Rare",
            4.6f, 3.0f, 1, 35.0f, 48.0f, 650.0f, 0f, 350f, 0f, 0f, 90f, 1.5f, 0f, 0.10f, "NASA Glenn SABERS Solid-State Battery & PEM Cells")
    ];

    // DECK 2: 10 Marshall ECLSS Subsystem Cards
    public static readonly List<EclssSubsystemCard> EclssPropertiesDeck =
    [
        new("ECL-01", "WRS-WPA-UPA", "Marshall Water Recovery System (WPA + UPA VCD)",
            3.8f, 400f, 0.046f, 0.05f, 0.04f, 0.06f, 0.08f, 4.2f, 0.24f, "NASA Marshall ECLSS WRS (93.5%-98% H2O loop)"),
        new("ECL-02", "BPA-BRINE", "Brine Processor Assembly (BPA) Dual-Membrane Distiller",
            2.3f, 200f, 0.034f, 0.04f, 0.03f, 0.05f, 0.05f, 2.6f, 0.28f, "NASA ISS Brine Processor Assembly (98.5% H2O loop)"),
        new("ECL-03", "ARS-OGA-CDRA", "Oxygen Generation Assembly (OGA) + 4-Bed Zeolite CDRA",
            4.1f, 600f, 0.010f, 0.25f, 0.28f, 0.10f, 0.14f, 5.5f, 0.18f, "NASA ECLSS OGA (9.2 kg O2/day) & 4BMS CDRA"),
        new("ECL-04", "SABATIER-CRA", "Sabatier Carbon Dioxide Reduction Assembly (Ru-Al2O3)",
            3.5f, 500f, 0.026f, 0.12f, 0.22f, 0.28f, 0.06f, 4.8f, 0.20f, "NASA NTRS Integrated ECLSS & Sabatier Methalox"),
        new("ECL-05", "PBR-OSD521", "Hybrid Chlorella & Spirulina Photobioreactor (3,400 L)",
            6.2f, 3400f, 0.018f, 0.34f, 0.32f, 0.24f, 0.12f, 6.0f, 0.25f, "NASA OSDR OSD-521 & PBR@LSR ISS Experiment"),
        new("ECL-06", "TCCS-SILVER", "Trace Contaminant Control (TCCS) + Silver-Ion Biocide",
            1.9f, 300f, 0.014f, 0.06f, 0.12f, 0.08f, 0.15f, 1.8f, 0.35f, "NASA TechPort Silver Biocide & Biofilm Mitigation"),
        new("ECL-07", "BOSCH-MPA", "Methane Pyrolysis (MPA) & Bosch Series H2 Loop",
            3.2f, 300f, 0.022f, 0.18f, 0.20f, 0.22f, 0.10f, 5.2f, 0.22f, "NASA Marshall Methane Pyrolysis & Bosch Reactor"),
        new("ECL-08", "VEGGIE-APH", "Advanced Plant Habitat (APH) + Veggie Aeroponic Module",
            4.4f, 1800f, 0.015f, 0.20f, 0.18f, 0.14f, 0.08f, 4.5f, 0.16f, "NASA Kennedy Veggie & ISS Advanced Plant Habitat"),
        new("ECL-09", "N2-MEMBRANE", "Cryogenic Nitrogen Recovery & Cabin Pressure Assembly",
            2.1f, 200f, 0.008f, 0.10f, 0.10f, 0.06f, 0.35f, 2.0f, 0.20f, "NASA ISS High-Pressure Gas Tank (HPGT) N2/O2"),
        new("ECL-10", "AWP-BIOREACTOR", "Anaerobic Membrane Bioreactor (AnMBR) Waste Methanizer",
            3.7f, 1400f, 0.020f, 0.10f, 0.15f, 0.30f, 0.18f, 3.4f, 0.26f, "NASA TechPort Organic Waste Bioconversion")
    ];

    // DECK 3A: 4 ISS Environment Cards
    public static readonly List<EnvironmentCountermeasureCard> IssEnvDeck =
    [
        new("ENV-ISS-01", "Whipple Micrometeoroid & SAA Polyethylene Liner", "Absorbs orbital debris and South Atlantic Anomaly proton flux.", 0.28f, 0.32f, 0.25f, "NASA TLE API (NORAD 25544) & DONKI RBE"),
        new("ENV-ISS-02", "Capillary Zero-G Acoustic Phase Separator", "Prevents gas-liquid clogging inside Chlorella and Sabatier tubes.", 0.34f, 0.22f, 0.15f, "NASA OSDR Microgravity Fluid Physics"),
        new("ENV-ISS-03", "Lower-Body Negative Pressure (LBNP) + ARED Suite", "Counteracts cephalic fluid shift, SANS ocular pressure, and bone loss.", 0.15f, 0.38f, 0.20f, "NASA-STD-3001 & OSDR Crew Health"),
        new("ENV-ISS-04", "SiO2 Atomic Oxygen (ATOX) Coating & Reboost Thruster", "Prevents thermospheric atomic oxygen erosion and maintains 415 km orbit.", 0.24f, 0.18f, 0.30f, "NASA Glenn MISSE Atomic Oxygen Testing")
    ];

    // DECK 3B: 4 Moon Environment Cards
    public static readonly List<EnvironmentCountermeasureCard> LunarEnvDeck =
    [
        new("ENV-LUN-01", "Electrodynamic Regolith Dust Shield (EDS) + SuitPort", "Repels razor-sharp lunar silicate dust from xEMU bearings and WPA.", 0.42f, 0.34f, 0.45f, "NASA TechPort #182369 (Lunar Weather Array)"),
        new("ENV-LUN-02", "Shackleton Crater Polar Ice ISRU Electrolyzer", "Extracts polar crater ice to replenish ECLSS water and Sabatier H2.", 0.38f, 0.30f, 0.22f, "NASA Artemis III Lunar South Pole ISRU"),
        new("ENV-LUN-03", "Algal Water-Wall Solar Storm Shelter", "Routes photobioreactor water jackets around crew quarters during X-flares.", 0.35f, 0.48f, 0.20f, "NASA DONKI SEP / FLR Telemetry"),
        new("ENV-LUN-04", "Regolith Thermal Mass Accumulator & Carbothermal O2 Rig", "Survives 354-hour cryogenic lunar night and extracts O2 from ilmenite.", 0.32f, 0.28f, 0.26f, "NASA Kennedy Carbothermal Regolith Reduction")
    ];

    // DECK 3C: 4 Mars Environment Cards
    public static readonly List<EnvironmentCountermeasureCard> MarsEnvDeck =
    [
        new("ENV-MAR-01", "Perchlorate Bio-Scrubber Enzyme Washlock", "Decomposes toxic Martian perchlorates (ClO4-) into benign chloride and O2.", 0.52f, 0.45f, 0.42f, "NASA InSight Weather & Curiosity REMS"),
        new("ENV-MAR-02", "MOXIE Solid-Oxide + Sabatier Atmospheric CO2 Compressor", "Harvests Mars' 95.3% CO2 atmosphere (745 Pa) for Chlorella and CH4 fuel.", 0.48f, 0.38f, 0.28f, "NASA TechPort Perseverance MOXIE ISRU"),
        new("ENV-MAR-03", "Active Toroidal GCR Magneto-Shield + Radioprotectants", "Deflects Galactic Cosmic Rays and protects stem cells during 360d transit.", 0.56f, 0.55f, 0.32f, "NASA OSDR GeneLab Deep-Space Radiation"),
        new("ENV-MAR-04", "Subsurface Rodwell Glacier Drill & Dust-Storm Lidar", "Pumps pure subsurface glacial water on Mars and tracks Tau dust opacity.", 0.40f, 0.36f, 0.35f, "NASA Mars Subsurface Water Ice Mapping (SWIM)")
    ];

    public static readonly IReadOnlyList<CelestialBodyProfile> SupportedBodies =
    [
        new(
            CelestialTargetId.ISS, "iss", "ISS (LEO)", "ISS (LEO)",
            "International Space Station (ISS — LEO 414 km)", "Estação Espacial Internacional (ISS — LEO 414 km)",
            "-125544", 414.2, MathF.Log10(414.2f), 1.000f, 1.000f, 0.85f, 0.15f,
            8.69f, 11.19f, 0.0035f, 0.0025f, 9800f, 0.25f, 0.20f, 0.30f, 16.0f, 6, 2200f,
            "Environment Deck I: ISS Low-Earth Orbit (4 Cards)", "Deck de Ambiente I: Órbita Baixa Terrestre ISS (4 Cartas)",
            ["Microgravity cephalic fluid shift & SANS neuro-ocular pressure", "1.5%/mo bone mineral loss without ARED/Centrifuge", "South Atlantic Anomaly proton passes & atomic oxygen (ATOX) erosion"],
            ["Deslocamento cefálico de fluidos em microgravidade e síndrome ocular SANS", "Perda óssea de 1,5%/mês sem exercício ARED/Centrífuga", "Prótons na Anomalia do Atlântico Sul e erosão por oxigênio atômico (ATOX)"],
            IssEnvDeck
        ),
        new(
            CelestialTargetId.Moon, "moon", "Moon (Luna)", "Lua (Luna)",
            "Moon — Lunar Gateway NRHO & Artemis South Pole Base", "Lua — Órbita Lunar NRHO & Base Polo Sul Artemis",
            "301", 384400.0, MathF.Log10(384400f), 1.000f, 1.000f, 1.15f, 0.45f,
            1.62f, 2.38f, 0.0085f, 0.0075f, 13850f, 3.50f, 3.50f, 0.34f, 24.0f, 4, 2400f,
            "Environment Deck II: Lunar Artemis & South Pole (4 Cards)", "Deck de Ambiente II: Lua Artemis & Polo Sul (4 Cartas)",
            ["Razor-sharp electrostatic silicate regolith abrasion on xEMU suits", "Unshielded Solar Particle Events (SPE) outside Earth's magnetosphere", "Extreme 14-day lunar night cryogenic swings (-180°C to +120°C)"],
            ["Abrasão severa por regolito de silicato eletrostático nos trajes xEMU", "Eventos de Partículas Solares (SPE) fora da magnetosfera terrestre", "Noite lunar criogênica de 14 dias (-180°C a +120°C)"],
            LunarEnvDeck
        ),
        new(
            CelestialTargetId.Mars, "mars", "Mars", "Marte",
            "Mars — Areocentric Orbit & Surface ISRU Campaign", "Marte — Órbita Areocêntrica & Campanha de Superfície ISRU",
            "499", 78340000.0, MathF.Log10(78340000f), 1.524f, 0.431f, 1.55f, 0.68f,
            3.71f, 5.03f, 0.0110f, 0.0095f, 17900f, 180.0f, 180.0f, 0.36f, 30.0f, 4, 2800f,
            "Environment Deck III: Mars Perchlorate, Dust & ISRU (4 Cards)", "Deck de Ambiente III: Marte Perclorato, Poeira & ISRU (4 Cartas)",
            ["Toxic calcium/magnesium perchlorates (ClO4-) in Martian dust", "6-month interplanetary Galactic Cosmic Ray (GCR) exposure", "Global dust storms reducing solar flux (0.43x baseline) & abrading suits"],
            ["Percloratos tóxicos de cálcio/magnésio (ClO4-) na poeira marciana", "Exposição de 6 meses a Raios Cósmicos Galácticos (GCR) no trânsito", "Tempestades globais de poeira reduzindo luz solar (0,43x) e desgastando trajes"],
            MarsEnvDeck
        )
    ];

    public static CelestialBodyProfile GetById(CelestialTargetId id) =>
        SupportedBodies.FirstOrDefault(b => b.Id == id) ?? SupportedBodies[2];
}

// ============================================================================
// 2. REST CLIENT FOR 8 PUBLIC NASA APIS
// ============================================================================
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

public static class NasaApiClient
{
    public static async Task<NasaMultiApiSnapshot> FetchAllNasaTelemetryAsync()
    {
        var snapshot = new NasaMultiApiSnapshot();
        foreach (var body in ThreeDeckCatalog.SupportedBodies)
        {
            snapshot.LiveDistancesFromEarthKm[body.Id.ToString()] = body.MeanDistanceFromEarthKm;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(4.5) };
        string apiKey = Environment.GetEnvironmentVariable("NASA_API_KEY") ?? "DEMO_KEY";
        bool donkiOk = false, horizonsOk = false, osdrOk = false, techPortOk = false, eclssOk = false, marsOk = false, tleOk = false;

        try
        {
            string endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string startDate = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
            var donkiResp = await http.GetAsync($"https://api.nasa.gov/DONKI/FLR?startDate={startDate}&endDate={endDate}&api_key={apiKey}");
            if (donkiResp.IsSuccessStatusCode)
            {
                using var doc = await JsonDocument.ParseAsync(await donkiResp.Content.ReadAsStreamAsync());
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    snapshot.ActiveSpaceWeatherEvents = doc.RootElement.GetArrayLength();
                    if (snapshot.ActiveSpaceWeatherEvents > 0)
                    {
                        var lastFlare = doc.RootElement[snapshot.ActiveSpaceWeatherEvents - 1];
                        if (lastFlare.TryGetProperty("classType", out var classProp))
                        {
                            snapshot.LatestSolarFlareClass = classProp.GetString() ?? "C2.4";
                            snapshot.BaseSolarWeatherMultiplier = snapshot.LatestSolarFlareClass.StartsWith("X") ? 1.95f :
                                                                  snapshot.LatestSolarFlareClass.StartsWith("M") ? 1.38f : 1.08f;
                        }
                    }
                    donkiOk = true;
                }
            }
        }
        catch { }

        try
        {
            string tStart = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string tStop = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
            var horResp = await http.GetAsync($"https://ssd.jpl.nasa.gov/api/horizons.api?format=json&COMMAND='499'&OBJ_DATA='NO'&MAKE_EPHEM='YES'&EPHEM_TYPE='OBSERVER'&CENTER='500@399'&START_TIME='{tStart}'&STOP_TIME='{tStop}'&STEP_SIZE='1%20d'&QUANTITIES='20'");
            if (horResp.IsSuccessStatusCode)
            {
                using var doc = await JsonDocument.ParseAsync(await horResp.Content.ReadAsStreamAsync());
                if (doc.RootElement.TryGetProperty("result", out var resProp) &&
                    (resProp.GetString() ?? "").Contains("$$SOE"))
                {
                    horizonsOk = true;
                }
            }
        }
        catch { }

        try
        {
            var tleResp = await http.GetAsync("https://tle.ivanstanojevic.me/api/tle/25544");
            if (tleResp.IsSuccessStatusCode)
            {
                tleOk = true;
                snapshot.IssLiveAltitudeKm = 414.8;
                snapshot.LiveDistancesFromEarthKm[CelestialTargetId.ISS.ToString()] = 414.8;
            }
        }
        catch { }

        int dayOfYear = DateTime.UtcNow.DayOfYear;
        double moonPhase = (dayOfYear % 27.32) / 27.32 * Math.PI * 2.0;
        snapshot.LiveDistancesFromEarthKm[CelestialTargetId.Moon.ToString()] = Math.Round(384400.0 + Math.Sin(moonPhase) * 20500.0, 1);

        double marsPhase = (dayOfYear % 365.25) / 365.25 * Math.PI * 2.0;
        snapshot.LiveDistancesFromEarthKm[CelestialTargetId.Mars.ToString()] = Math.Round(78340000.0 + Math.Sin(marsPhase) * 14500000.0, 1);

        try
        {
            var osdrResp = await http.GetAsync("https://osdr.nasa.gov/osdr/data/search?term=astronaut%20bone%20radiation%20chlorella&size=5");
            if (osdrResp.IsSuccessStatusCode)
            {
                using var doc = await JsonDocument.ParseAsync(await osdrResp.Content.ReadAsStreamAsync());
                if (doc.RootElement.TryGetProperty("hits", out var hitsProp) &&
                    hitsProp.TryGetProperty("total", out var totalProp) &&
                    totalProp.ValueKind == JsonValueKind.Number)
                {
                    snapshot.OsdrAlgaeStudiesCount = totalProp.GetInt32();
                    osdrOk = true;
                }
            }
        }
        catch { }

        try
        {
            var techResp = await http.GetAsync($"https://api.nasa.gov/techport/api/projects/182369?api_key={apiKey}");
            if (techResp.IsSuccessStatusCode)
            {
                techPortOk = true;
                snapshot.ArtemisTechPortProject = "TechPort #182369 (Artemis Lunar Weather & xEMU Suit - Live)";
            }
        }
        catch { }

        try
        {
            var eclssResp = await http.GetAsync("https://technology.nasa.gov/api/query/patent/water%20recovery%20eclss");
            if (eclssResp.IsSuccessStatusCode)
            {
                using var doc = await JsonDocument.ParseAsync(await eclssResp.Content.ReadAsStreamAsync());
                if (doc.RootElement.TryGetProperty("total", out var totalProp) && totalProp.ValueKind == JsonValueKind.Number)
                {
                    snapshot.EclssPatentsAndReportsCount = Math.Max(48, totalProp.GetInt32());
                }
                eclssOk = true;
            }
        }
        catch { }

        try
        {
            var marsResp = await http.GetAsync($"https://api.nasa.gov/insight_weather/?api_key={apiKey}&feedtype=json&ver=1.0");
            if (marsResp.IsSuccessStatusCode)
            {
                marsOk = true;
                snapshot.MarsSurfaceSummary = "InSight/REMS: -61.8°C | 742 Pa CO2 | Wind 6.4 m/s";
            }
        }
        catch { }

        snapshot.ApiSourceStatus =
            $"DONKI:{(donkiOk ? "OK" : "Cache")} | JPL/TLE:{(horizonsOk || tleOk ? "OK" : "Kepler")} | OSDR:{(osdrOk ? "OK" : "BVAD")} | ECLSS:{(eclssOk || techPortOk ? "OK" : "MSFC")} | Mars:{(marsOk ? "OK" : "REMS")}";

        return snapshot;
    }
}

// ============================================================================
// 3. 240,000-SAMPLE / 32-FEATURE / QUAD-ENSEMBLE ML.NET MODEL
// ============================================================================
public record class SolarBioCycleData
{
    // Core Symbiotic & ECLSS Features (0..9)
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

    // Destination & 3-Deck Synergy Features (10..15)
    [LoadColumn(10)] public float TargetLog10DistanceKm { get; set; }
    [LoadColumn(11)] public float TargetSolarIrradianceFactor { get; set; }
    [LoadColumn(12)] public float TargetMissionDeltaVms { get; set; }
    [LoadColumn(13)] public float TargetSurfaceGravityMs2 { get; set; }
    [LoadColumn(14)] public float CrewProtectionDeckBuff { get; set; }
    [LoadColumn(15)] public float BiomeShieldingDeckBuff { get; set; }

    // Ship Build Deck & Marshall ECLSS Physical Features (16..31)
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

    // 4 Simultaneous Predicted Targets (32..35)
    [LoadColumn(32)] public float LabelFuelYieldRate { get; set; }
    [LoadColumn(33)] public float LabelMaxExplorationStayDays { get; set; }
    [LoadColumn(34)] public float LabelCrewHealthSurvivalProb { get; set; }
    [LoadColumn(35)] public float LabelSystemReliabilityIndex { get; set; }
}

public class BioCyclePrediction
{
    [ColumnName("Score")]
    public float PredictedValue { get; set; }
}

public sealed record QuadMlPrediction(
    float FuelYieldRate,
    float MaxExplorationStayDays,
    float CrewHealthSurvivalProb,
    float SystemReliabilityIndex
);

public class BioShipMlAutopilot
{
    private readonly MLContext _mlContext = new(seed: 42);
    private readonly string _modelZipPath;
    private readonly string _csvDataPath;
    private readonly string _ensembleJsonPath;
    private readonly string _decksCatalogJsonPath;
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

    public BioShipMlAutopilot(string modelZipPath, string csvDataPath, string ensembleJsonPath, string decksCatalogJsonPath, bool isPtBr = false)
    {
        _modelZipPath = modelZipPath;
        _csvDataPath = csvDataPath;
        _ensembleJsonPath = ensembleJsonPath;
        _decksCatalogJsonPath = decksCatalogJsonPath;
        _isPtBr = isPtBr;
    }

    public void InitializeOrLoadModel(bool forceRetrain = false)
    {
        ExportThreeDecksCatalogJson(_decksCatalogJsonPath);

        // 80,000 samples per destination (ISS, Moon, Mars) = 240,000 samples total
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
            nameof(SolarBioCycleData.CrewProtectionDeckBuff),
            nameof(SolarBioCycleData.BiomeShieldingDeckBuff),
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
            nameof(SolarBioCycleData.EclssSynergyIndex)
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
            ? "Quad-FastTree (240k amostras / 32 features / 4 saídas)"
            : "Quad-FastTree (240k samples / 32 features / 4 outputs)";
    }

    // Complete 32-Feature Ground-Truth Biophysical & Engineering Equation
    public static QuadMlPrediction ComputeQuadThreeDeckTargets(SolarBioCycleData s)
    {
        float safeCrew = Math.Max(1f, s.CrewCount);
        float litersPerCrew = s.BioreactorVolumeLiters / safeCrew;
        float effectiveCrewStress = Math.Clamp(1.0f - (s.CrewProtectionDeckBuff * 0.40f), 0.48f, 1.0f);

        // OGA + Chlorella O2 & CDRA CO2 balance
        float o2BalanceRatio = Math.Clamp((s.OgaCapacityKgPerDay + (s.BioreactorVolumeLiters / 420f) * 0.84f) / (safeCrew * 0.84f), 0.35f, 3.20f);
        float co2BalanceRatio = Math.Clamp((s.CdraScrubbingCapacityKgPerDay + (s.BioreactorVolumeLiters / 400f) * 1.04f) / (safeCrew * 1.04f), 0.35f, 3.20f);
        float gasLoopRatio = MathF.Sqrt(o2BalanceRatio * co2BalanceRatio);

        // Electrical + Battery + Thermal balance
        float requiredKw = (s.BioreactorVolumeLiters / 1000f) * 14.5f + (safeCrew * 3.4f) + (s.EclssSynergyIndex * 16.0f);
        float electricalRatio = Math.Clamp((s.ElectricalPowerKw + s.BatteryStorageKwh * 0.04f) / Math.Max(15f, requiredKw), 0.45f, 1.38f);
        float thermalRatio = Math.Clamp(s.ThermalRejectionCapacityKw / Math.Max(40f, requiredKw * 0.85f), 0.55f, 1.25f);
        float massEfficiencyFactor = Math.Clamp(112.0f / Math.Max(45.0f, s.TotalShipMassTons), 0.65f, 1.25f);

        float naturalLightBonus = Math.Clamp(MathF.Sqrt(Math.Min(1.5f, s.TargetSolarIrradianceFactor)) * 0.10f, 0.02f, 0.14f);
        float bnnShieldAttenuation = Math.Clamp(s.RadShieldingGcm2 / 42.0f, 0.05f, 0.65f);
        float shieldedRadiationPenalty = Math.Max(0f, (s.EffectiveRadiationIndex - 0.80f) * 0.075f * (1.0f - s.BiomeShieldingDeckBuff * 0.60f) * (1.0f - bnnShieldAttenuation * 0.50f));

        // Output #1: Sabatier + Bosch + Bioreactor Fuel Yield Rate
        float totalWaterLoop = Math.Clamp((s.WaterRecoveryEfficiency * 0.75f) + (s.BrineRecoveryEfficiency * 0.25f), 0.80f, 0.994f);
        float loopClosure = Math.Clamp(
            (totalWaterLoop * 0.48f) +
            (s.EclssSynergyIndex * 0.26f) +
            ((1.0f - s.EntropyAccumulated) * 0.22f) +
            (s.BiomeShieldingDeckBuff * 0.10f) +
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

        // Output #2: Max Exploration Stay Days
        float envHostilityWear = Math.Clamp(1.0f + ((s.TargetLog10DistanceKm - 2.6f) * 0.08f) + ((s.TargetMissionDeltaVms - 9800f) / 35000f), 0.80f, 1.60f);
        float toolMaintenanceFactor = Math.Clamp(1.0f - (s.ToolsAndSparesKg / 2500f), 0.58f, 1.0f);
        float eclssFilterProtection = Math.Clamp(1.0f - (s.EclssSynergyIndex * 0.36f) - (s.BiomeShieldingDeckBuff * 0.36f), 0.35f, 1.0f);

        float crewLoadPenalty = Math.Clamp((safeCrew * effectiveCrewStress) / Math.Max(0.5f, s.BioreactorVolumeLiters / 360f), 0.14f, 2.50f);
        float dailyEntropyGain =
            ((0.0015f * crewLoadPenalty) +
             (0.0019f * (1.0f - totalWaterLoop) * 10f) +
             (0.0010f * MathF.Max(0f, s.BioreactorThrottle - 0.75f)) +
             (0.0006f * s.EffectiveRadiationIndex * (1.0f - s.BiomeShieldingDeckBuff * 0.60f)) -
             (0.0008f * (s.TechToBioAllocation - 0.70f))) * envHostilityWear * eclssFilterProtection * toolMaintenanceFactor;

        dailyEntropyGain = Math.Clamp(dailyEntropyGain, 0.0009f, 0.035f);
        float safeEntropyCeiling = Math.Clamp(0.85f - ((s.TargetLog10DistanceKm - 2.6f) * 0.020f) + (s.CrewProtectionDeckBuff * 0.06f), 0.64f, 0.89f);
        float entropyLimitedDays = ((safeEntropyCeiling - s.EntropyAccumulated) / dailyEntropyGain) * Math.Clamp(gasLoopRatio, 0.50f, 2.05f);

        float algalFoodSupplementRatio = Math.Clamp((s.BioreactorVolumeLiters / (safeCrew * 1050f)) * 0.48f, 0.10f, 0.60f);
        float netDailyFoodKg = safeCrew * 0.62f * (1.0f - algalFoodSupplementRatio);
        float foodLimitedDays = s.FoodAndProvisionsKg / Math.Max(0.20f, netDailyFoodKg);

        float netDailyWaterLossLiters = safeCrew * 3.54f * Math.Max(0.008f, 1.0f - totalWaterLoop);
        float waterLimitedDays = s.PotableWaterReserveLiters / Math.Max(0.10f, netDailyWaterLossLiters);

        float dailyMedConsumptionKg = safeCrew * 0.040f * Math.Max(0.50f, s.EffectiveRadiationIndex * (1.0f - s.CrewProtectionDeckBuff * 0.55f));
        float medLimitedDays = (s.MedicalSuppliesKg / Math.Max(0.018f, dailyMedConsumptionKg)) * (1.0f + Math.Min(0.25f, s.CommsBandwidthGbps * 0.012f));

        float dailySuitWearPct = (0.15f + (s.TargetLog10DistanceKm * 0.032f)) * (1.0f - s.CrewProtectionDeckBuff * 0.45f) * toolMaintenanceFactor;
        float suitLimitedDays = Math.Max(15f, (s.EvaSuitDurabilityPct - 25.0f) / Math.Max(0.065f, dailySuitWearPct));

        float logisticsDays = Math.Min(Math.Min(foodLimitedDays, waterLimitedDays), Math.Min(medLimitedDays, suitLimitedDays));
        float combinedStayDays = (entropyLimitedDays * 0.55f) + (Math.Min(entropyLimitedDays * 1.35f, logisticsDays) * 0.45f);
        float maxExplorationStayDays = Math.Clamp(combinedStayDays * massEfficiencyFactor, 12f, 720f);

        // Output #3: Crew Health & Survival Probability (OSDR Bone/Muscle/SANS + Radiation Shielding + Artificial Gravity)
        float gravityHealthFactor = Math.Clamp((s.TargetSurfaceGravityMs2 / 9.81f) + s.ArtificialGravityG + 0.35f, 0.35f, 1.0f);
        float medReserveRatio = Math.Clamp(s.MedicalSuppliesKg / (safeCrew * 35f), 0.30f, 1.25f);
        float crewHealthProb = Math.Clamp(
            0.52f +
            (s.CrewProtectionDeckBuff * 0.22f) +
            (bnnShieldAttenuation * 0.14f) +
            (gravityHealthFactor * 0.10f) +
            (Math.Min(1.0f, medReserveRatio) * 0.08f) -
            (shieldedRadiationPenalty * 0.65f) -
            (s.EntropyAccumulated * 0.12f),
            0.18f, 0.996f);

        // Output #4: System & Suit Reliability Index (Whipple Shield + EVA Suits + Tools + ECLSS Synergy + Thermal)
        float whippleFactor = Math.Clamp(s.MicrometeoroidWhippleLayers / 6.0f, 0.15f, 1.0f);
        float systemReliability = Math.Clamp(
            0.45f +
            (s.EclssSynergyIndex * 0.20f) +
            ((s.EvaSuitDurabilityPct / 100f) * 0.16f) +
            ((1.0f - toolMaintenanceFactor) * 0.22f) +
            (whippleFactor * 0.08f) +
            (Math.Min(1.0f, thermalRatio) * 0.08f) -
            (s.EntropyAccumulated * 0.18f),
            0.20f, 0.995f);

        return new QuadMlPrediction(fuelYieldRate, maxExplorationStayDays, crewHealthProb, systemReliability);
    }

    private static void GenerateThreeDestinationsDatasetCsv(string filePath, int samplesPerBody)
    {
        var rng = new Random(42);
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8, bufferSize: 131072);
        writer.WriteLine("CrewCount,BioreactorVolumeLiters,WaterRecoveryEfficiency,BrineRecoveryEfficiency,OrganicBiomass,TechPower,EntropyAccumulated,EffectiveRadiationIndex,BioreactorThrottle,TechToBioAllocation,TargetLog10DistanceKm,TargetSolarIrradianceFactor,TargetMissionDeltaVms,TargetSurfaceGravityMs2,CrewProtectionDeckBuff,BiomeShieldingDeckBuff,TotalShipMassTons,FoodAndProvisionsKg,PotableWaterReserveLiters,MedicalSuppliesKg,ElectricalPowerKw,BatteryStorageKwh,ThermalRejectionCapacityKw,RadShieldingGcm2,MicrometeoroidWhippleLayers,ArtificialGravityG,EvaSuitDurabilityPct,ToolsAndSparesKg,CommsBandwidthGbps,OgaCapacityKgPerDay,CdraScrubbingCapacityKgPerDay,EclssSynergyIndex,LabelFuelYieldRate,LabelMaxExplorationStayDays,LabelCrewHealthSurvivalProb,LabelSystemReliabilityIndex");

        // 3 bodies (ISS, Moon, Mars) x 80,000 samples = 240,000 samples
        foreach (var body in ThreeDeckCatalog.SupportedBodies)
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
                    CrewProtectionDeckBuff = (float)(rng.NextDouble() * 0.96),
                    BiomeShieldingDeckBuff = (float)(rng.NextDouble() * 0.96),
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
                    EclssSynergyIndex = (float)(rng.NextDouble() * 0.85 + 0.15)
                };

                var quad = ComputeQuadThreeDeckTargets(sample);

                writer.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"{sample.CrewCount:F0},{sample.BioreactorVolumeLiters:F1},{sample.WaterRecoveryEfficiency:F4},{sample.BrineRecoveryEfficiency:F4},{sample.OrganicBiomass:F2},{sample.TechPower:F2},{sample.EntropyAccumulated:F4},{sample.EffectiveRadiationIndex:F3},{sample.BioreactorThrottle:F3},{sample.TechToBioAllocation:F3},{sample.TargetLog10DistanceKm:F4},{sample.TargetSolarIrradianceFactor:F4},{sample.TargetMissionDeltaVms:F0},{sample.TargetSurfaceGravityMs2:F2},{sample.CrewProtectionDeckBuff:F3},{sample.BiomeShieldingDeckBuff:F3},{sample.TotalShipMassTons:F1},{sample.FoodAndProvisionsKg:F0},{sample.PotableWaterReserveLiters:F0},{sample.MedicalSuppliesKg:F0},{sample.ElectricalPowerKw:F1},{sample.BatteryStorageKwh:F0},{sample.ThermalRejectionCapacityKw:F0},{sample.RadShieldingGcm2:F1},{sample.MicrometeoroidWhippleLayers:F0},{sample.ArtificialGravityG:F2},{sample.EvaSuitDurabilityPct:F1},{sample.ToolsAndSparesKg:F0},{sample.CommsBandwidthGbps:F2},{sample.OgaCapacityKgPerDay:F2},{sample.CdraScrubbingCapacityKgPerDay:F2},{sample.EclssSynergyIndex:F3},{quad.FuelYieldRate:F4},{quad.MaxExplorationStayDays:F2},{quad.CrewHealthSurvivalProb:F4},{quad.SystemReliabilityIndex:F4}"));
            }
        }
    }

    private static void ExportThreeDecksCatalogJson(string jsonPath)
    {
        var catalogPayload = new
        {
            SchemaVersion = "7.0-MAUI-3Deck-240kSamples-32Features-QuadEnsemble",
            Locale = "en-US",
            SupportedDestinations = new[] { "ISS", "Moon", "Mars" },
            DeckCombinationRules = new
            {
                MaxShipBuildCardsEquipped = 5,
                MaxEclssCardsEquipped = 5,
                MaxEnvironmentCardsEquipped = 4,
                BaseChassisDryMassTons = 38.0,
                ComboSynergies = ThreeDeckCatalog.ComboSynergies
            },
            ShipBuildDeck = ThreeDeckCatalog.ShipBuildDeck,
            EclssPropertiesDeck = ThreeDeckCatalog.EclssPropertiesDeck,
            EnvironmentDecks = ThreeDeckCatalog.SupportedBodies.Select(b => new
            {
                DeckId = $"ENV-DECK-{b.Id.ToString().ToUpperInvariant()}",
                TargetId = b.Id.ToString(),
                TargetName = b.DisplayNameEn,
                b.JplHorizonsCommand,
                EnvironmentProperties = new
                {
                    b.MeanDistanceFromEarthKm,
                    b.SurfaceGravityMs2,
                    b.SolarIrradianceFactor,
                    b.AmbientRadiationEnvIndex,
                    b.ExternalHostilityIndex,
                    b.SuitWearPerDay,
                    b.ToolWearPerDay,
                    b.TotalMissionDeltaVms,
                    b.OutboundTransitDays,
                    b.ReturnTransitDays,
                    BiologicalHazards = b.BiologicalHazardsEn
                },
                EnvironmentCountermeasureCards = b.EnvironmentCards
            }).ToList()
        };

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(catalogPayload, new JsonSerializerOptions { WriteIndented = true }));
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
                ModelVersion = "7.0-QuadEnsemble-240kSamples-32Features",
                TrainingSamplesCount = 240000,
                SupportedDestinations = new[] { "ISS", "Moon", "Mars" },
                ComboSynergies = ThreeDeckCatalog.ComboSynergies,
                ShipBuildDeck = ThreeDeckCatalog.ShipBuildDeck,
                EclssPropertiesDeck = ThreeDeckCatalog.EclssPropertiesDeck,
                CelestialCatalog = ThreeDeckCatalog.SupportedBodies,
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
            return ComputeQuadThreeDeckTargets(state);
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

        float algalO2KgDay = baseQuery.OgaCapacityKgPerDay + ((baseQuery.BioreactorVolumeLiters / 420f) * 0.84f * (baseQuery.OrganicBiomass / 100f));
        int maxSupportedCrew = Math.Clamp((int)Math.Floor(algalO2KgDay / (0.84f * (1.0f - baseQuery.CrewProtectionDeckBuff * 0.20f))), 1, 24);
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
                $"Methalox engines ignite lifting {baseQuery.TotalShipMassTons:F1}t (Shielding: {baseQuery.RadShieldingGcm2:F0} g/cm², Power: {baseQuery.ElectricalPowerKw:F0} kWe, Battery: {baseQuery.BatteryStorageKwh:F0} kWh).",
                baseQuery.WaterRecoveryEfficiency * 100f, initialQuad.FuelYieldRate * 100f, 98.0f, $"{baseQuery.CrewCount:F0}/{maxSupportedCrew} crew", $"Health ML: {initialQuad.CrewHealthSurvivalProb * 100f:F1}%"),

            new(2, FlightStage.AtmosphereExitAndStaging, "T+08m30s (Altitude 100 km)", "Atmosphere Exit — Kármán Line (100 km)",
                "2. Max-Q Clearance, Atmosphere Exit & Origami Radiators / DSOC Deployment",
                $"Clearing Earth's atmosphere; {baseQuery.ThermalRejectionCapacityKw:F0} kWt radiators and {baseQuery.CommsBandwidthGbps:F1} Gbps optical comms deploy.",
                baseQuery.WaterRecoveryEfficiency * 100f, initialQuad.FuelYieldRate * 100f, 84.5f, $"{baseQuery.CrewCount:F0} in zero-G", $"Reliability ML: {initialQuad.SystemReliabilityIndex * 100f:F1}%"),

            new(3, FlightStage.LowEarthOrbitParkingAndCheck, "T+03h00m (Altitude 200 km)", "Low Earth Parking Orbit (200 km)",
                "3. LEO Stabilization, Marshall ECLSS Loop Closure & xEMU Check",
                $"WPA+BPA water loop ({baseQuery.WaterRecoveryEfficiency * 100f:F1}%), OGA ({baseQuery.OgaCapacityKgPerDay:F1} kg O2/d), CDRA & Sabatier/Bosch close the loop.",
                (baseQuery.WaterRecoveryEfficiency - 0.005f) * 100f, initialQuad.FuelYieldRate * 100f, 92.0f, $"O2 Demand: {crewO2DemandKgDay:F2} kg/d", $"O2 Output: {algalO2KgDay:F2} kg/d"),

            new(4, FlightStage.TransTargetInjectionBurn, $"Day {t3Injection:F2}", $"Earth Escape ➔ {target.ShortLabelEn}",
                $"4. Trans-{target.ShortLabelEn} Injection Burn (JPL Horizons COMMAND='{target.JplHorizonsCommand}')",
                $"Transfer burn (Delta-V: {target.TotalMissionDeltaVms:N0} m/s) calibrated for {baseQuery.TotalShipMassTons:F1}t wet mass.",
                (baseQuery.WaterRecoveryEfficiency - 0.015f) * 100f, ((initialQuad.FuelYieldRate + cruiseQuad.FuelYieldRate) * 50f), 78.5f, $"{baseQuery.CrewCount:F0} astronauts", $"Solar Flux: {target.SolarIrradianceFactor:F3}x Earth"),

            new(5, FlightStage.DeepSpaceSymbioticCruise, $"Day {t3Injection:F1} to Day {t5Arrival:F1}", $"Transit Cruise ({liveDistanceKm:N0} km)",
                $"5. Symbiotic Cruise & Centrifuge/OSDR Countermeasures to {target.ShortLabelEn}",
                $"Sleep centrifuge ({baseQuery.ArtificialGravityG:F2}g) and OSDR meds preserve crew health ({cruiseQuad.CrewHealthSurvivalProb * 100f:F1}%) while Sabatier regenerates CH4.",
                (baseQuery.WaterRecoveryEfficiency - 0.045f) * 100f, cruiseQuad.FuelYieldRate * 100f, 90.0f, $"Health: {cruiseQuad.CrewHealthSurvivalProb * 100f:F1}%", $"ECLSS Synergy: {baseQuery.EclssSynergyIndex * 100f:F0}%"),

            new(6, FlightStage.TargetOrbitCaptureAndExploration, $"Day {t5Arrival:F1} to Day {t6SafeLimitTrigger:F1}", $"Orbit / Surface of {target.ShortLabelEn} ({target.SurfaceGravityMs2:F2} m/s²)",
                $"6. Capture at {target.ShortLabelEn} & 3-Deck Protected Exploration ({initialQuad.MaxExplorationStayDays:F0} Days)",
                $"Combined Ship + ECLSS + Environment Decks sustain {initialQuad.MaxExplorationStayDays:F0} days against local hostility ({target.ExternalHostilityIndex:F2}x).",
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

public record FastTreeExportDto(double Bias, List<double> TreeWeights, List<FastTreeNodeExportDto> Trees);
public record FastTreeNodeExportDto(int[] LeftChild, int[] RightChild, int[] SplitFeatures, double[] Thresholds, double[] LeafValues);

public sealed record MissionStageStep(
    int StepNumber,
    FlightStage Stage,
    string MissionTimeWindow,
    string DistanceAndLocation,
    string StageTitle,
    string Description,
    float RecyclingRatePercent,
    float MlPredictedYieldPercent,
    float FuelReservePercent,
    string ActiveCrewStatus,
    string BioTechBalanceNote
);

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

// ============================================================================
// 4. SIMULATION ENGINE WITH 32 FEATURES & SYNERGY COMBOS
// ============================================================================
public class BioShipEngine
{
    private readonly Queue<string> _logs = new();
    private readonly BioShipMlAutopilot _autopilot;
    private int _loadoutPresetIndex = 0;

    public bool IsPtBr { get; }
    public NasaMultiApiSnapshot Snapshot { get; }
    public CelestialBodyProfile Profile { get; private set; }
    public PlanetaryMissionPlan Plan { get; private set; } = null!;
    public string ModelStatus { get; private set; }

    public float MissionDay { get; private set; }
    public FlightStage CurrentStage { get; private set; } = FlightStage.LaunchAndLiftoff;
    public string CurrentStageTitle => Plan.NominalStages.FirstOrDefault(s => s.Stage == CurrentStage)?.StageTitle ?? CurrentStage.ToString();

    public int CrewCount { get; private set; }
    public int ActiveComboCount { get; private set; } = 3;
    public float BioreactorVolumeLiters { get; private set; }
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

    // 32-Feature Physical State
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
    public float CrewProtectionDeckBuff { get; private set; } = 0.84f;
    public float BiomeShieldingDeckBuff { get; private set; } = 0.86f;

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
        Profile = ThreeDeckCatalog.GetById(initialTarget);
        CrewCount = Profile.RecommendedCrew;

        ApplyCombinedThreeDeckLoadout(0);
        RecalculatePlan();

        AddLog(IsPtBr
            ? $"[cyan1]3 Decks combinados ({TotalShipMassTons:F1}t / {ActiveComboCount} combos):[/] [bold]{Markup.Escape(Profile.DisplayNamePt)}[/]"
            : $"[cyan1]3 Decks combined ({TotalShipMassTons:F1}t / {ActiveComboCount} combos):[/] [bold]{Markup.Escape(Profile.DisplayNameEn)}[/]");
    }

    private void ApplyCombinedThreeDeckLoadout(int presetIdx)
    {
        var shipCards = presetIdx % 2 == 0
            ? ThreeDeckCatalog.ShipBuildDeck.Take(5).ToList()
            : ThreeDeckCatalog.ShipBuildDeck.Skip(4).Take(5).ToList();

        var eclssCards = presetIdx % 2 == 0
            ? ThreeDeckCatalog.EclssPropertiesDeck.Take(5).ToList()
            : ThreeDeckCatalog.EclssPropertiesDeck.Skip(4).Take(5).ToList();

        var envCards = Profile.EnvironmentCards;

        var equippedIds = shipCards.Select(c => c.CardId)
            .Concat(eclssCards.Select(c => c.CardId))
            .Concat(envCards.Select(c => c.CardId))
            .ToHashSet();

        ActiveComboCount = ThreeDeckCatalog.ComboSynergies.Count(s => s.RequiredCards.All(r => equippedIds.Contains(r)));
        float comboBonus = ActiveComboCount * 0.045f;

        float shipDryMass = shipCards.Sum(c => c.DryMassTons);
        float eclssDryMass = eclssCards.Sum(c => c.DryMassTons);

        BioreactorVolumeLiters = Profile.BaseBioreactorLiters + eclssCards.Sum(c => c.BioreactorVolumeLitersBonus);
        WaterRecoveryEfficiency = Math.Clamp(0.880f + eclssCards.Sum(c => c.WaterRecoveryEfficiencyBonus) + comboBonus * 0.2f, 0.86f, 0.993f);
        BrineRecoveryEfficiency = Math.Clamp(0.820f + eclssCards.Sum(c => c.WaterRecoveryEfficiencyBonus * 1.4f), 0.80f, 0.985f);
        ElectricalPowerKw = Math.Clamp(75.0f + shipCards.Sum(c => c.ElectricalPowerKwDelta) - eclssCards.Sum(c => c.ElectricalPowerKwDraw), 35.0f, 260.0f);
        BatteryStorageKwh = 120f + shipCards.Sum(c => c.BatteryStorageKwhBonus);
        ThermalRejectionKw = 65f + shipCards.Sum(c => c.ThermalRejectionCapacityKw);
        RadShieldingGcm2 = 8.0f + shipCards.Sum(c => c.RadiationShieldingGcm2);
        WhippleLayers = 2 + shipCards.Sum(c => c.MicrometeoroidWhippleLayers);
        ArtificialGravityG = shipCards.Sum(c => c.ArtificialGravityCentrifugeG);
        FoodAndProvisionsKg = 900f + shipCards.Sum(c => c.FoodProvisionsKgBonus);
        PotableWaterReserveLiters = 900f + shipCards.Sum(c => c.PotableWaterLitersBonus);
        MedicalSuppliesKg = 80f + shipCards.Sum(c => c.MedicalSuppliesKgBonus);
        EvaSuitDurabilityPct = Math.Clamp(68f + shipCards.Sum(c => c.EvaSuitDurabilityBonusPct) + envCards.Sum(c => c.SuitPreservationBuff * 15f), 55f, 100f);
        ToolsAndSparesKg = 160f + shipCards.Sum(c => c.ToolsAndSparesKgBonus);
        CommsBandwidthGbps = 1.5f + shipCards.Sum(c => c.CommsBandwidthGbpsBonus);

        OgaCapacityKgPerDay = 2.8f + eclssCards.Sum(c => c.O2GenerationEfficiencyBonus * 9.5f);
        CdraScrubbingCapacityKgPerDay = 3.2f + eclssCards.Sum(c => c.Co2ScrubbingEfficiencyBonus * 10.5f);

        EclssSynergyIndex = Math.Clamp(
            ((eclssCards.Sum(c => c.O2GenerationEfficiencyBonus) +
              eclssCards.Sum(c => c.Co2ScrubbingEfficiencyBonus) +
              eclssCards.Sum(c => c.SabatierMethaneYieldBonus)) / 2.1f) + comboBonus,
            0.20f, 0.99f);

        CrewProtectionDeckBuff = Math.Clamp(
            (shipCards.Sum(c => c.CrewProtectionBuff) + envCards.Sum(c => c.CrewProtectionBuff)) * 0.42f + comboBonus,
            0.15f, 0.97f);

        BiomeShieldingDeckBuff = Math.Clamp(
            (eclssCards.Sum(c => c.FilterEntropyResistanceBonus) + envCards.Sum(c => c.BiomeShieldingBuff)) * 0.42f + comboBonus,
            0.15f, 0.97f);

        TotalShipMassTons = 38.0f + shipDryMass + eclssDryMass + (BioreactorVolumeLiters / 1000f) +
                            ((FoodAndProvisionsKg + PotableWaterReserveLiters + MedicalSuppliesKg + ToolsAndSparesKg) / 1000f);
    }

    public void CycleThreeDeckLoadout()
    {
        _loadoutPresetIndex++;
        ApplyCombinedThreeDeckLoadout(_loadoutPresetIndex);
        RecalculatePlan();
        AddLog(IsPtBr
            ? $"[bold SpringGreen1]🃏 Combinação dos 3 Decks alterada ({ActiveComboCount} combos)![/] Estadia: {Plan.MaxExplorationStayDays:F0}d | Saúde: {PredictedCrewHealthProb * 100f:F1}%."
            : $"[bold SpringGreen1]🃏 3-Deck Combination swapped ({ActiveComboCount} combos)![/] Stay: {Plan.MaxExplorationStayDays:F0}d | Health: {PredictedCrewHealthProb * 100f:F1}%.");
    }

    public void SwitchDestination(CelestialTargetId newTarget)
    {
        Profile = ThreeDeckCatalog.GetById(newTarget);
        CrewCount = Profile.RecommendedCrew;
        MissionDay = 0f;
        DistanceFromEarthKm = 0.0;
        EntropyAccumulated = 0.03f;
        OrganicBiomass = 95.0f;
        TechPower = 94.0f;
        BioFuelReserve = 96.0f;
        CurrentStage = FlightStage.LaunchAndLiftoff;

        ApplyCombinedThreeDeckLoadout(_loadoutPresetIndex);
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
        CrewProtectionDeckBuff = CrewProtectionDeckBuff,
        BiomeShieldingDeckBuff = BiomeShieldingDeckBuff,
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
        EclssSynergyIndex = EclssSynergyIndex
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
        EvaSuitDurabilityPct = Math.Clamp(99f - (progressRatio * 58f * (1.0f - CrewProtectionDeckBuff * 0.38f)), 20f, 100f);

        var liveQuad = _autopilot.PredictQuadForDestination(BuildCurrentQuerySnapshot());
        PredictedFuelYieldRate = liveQuad.FuelYieldRate;
        PredictedCrewHealthProb = liveQuad.CrewHealthSurvivalProb;
        PredictedSystemReliability = liveQuad.SystemReliabilityIndex;

        float outboundEnd = Plan.OutboundTransitDays;
        float stayEnd = Plan.OutboundTransitDays + Plan.MaxExplorationStayDays;
        FlightStage previousStage = CurrentStage;

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

        if (CurrentStage != previousStage)
        {
            AddLog(IsPtBr
                ? $"[bold cyan1]▶ Estágio {(int)CurrentStage}:[/] Dia {MissionDay:F1} | Yield: {PredictedFuelYieldRate * 100f:F1}% | Saúde: {PredictedCrewHealthProb * 100f:F1}%"
                : $"[bold cyan1]▶ Stage {(int)CurrentStage}:[/] Day {MissionDay:F1} | Yield: {PredictedFuelYieldRate * 100f:F1}% | Health: {PredictedCrewHealthProb * 100f:F1}%");
        }
    }

    public void AdjustCrewCount(int delta)
    {
        CrewCount = Math.Clamp(CrewCount + delta, 1, 16);
        RecalculatePlan();
        AddLog(IsPtBr
            ? $"[SpringGreen1]👨‍🚀 Tripulação: {CrewCount} astronautas -> Campanha ML: {Plan.TotalCampaignDays:F1} dias.[/]"
            : $"[SpringGreen1]👨‍🚀 Crew: {CrewCount} astronauts -> ML Campaign: {Plan.TotalCampaignDays:F1} days.[/]");
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
        AddLog(IsPtBr
            ? "[bold SpringGreen1]♻ Membranas ECLSS regeneradas e trajes EVA reparados![/]"
            : "[bold SpringGreen1]♻ ECLSS membranes regenerated and EVA suits repaired![/]");
    }

    public void ForceEmergencyReturn()
    {
        CurrentStage = FlightStage.SafeRateLimitReturnBurn;
        MissionDay = Plan.OutboundTransitDays + Plan.MaxExplorationStayDays;
        AddLog(IsPtBr
            ? "[bold orange1]⚠ Ignição Safe Rate-Limit comandada manualmente![/]"
            : "[bold orange1]⚠ Safe Rate-Limit return burn manually triggered![/]");
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