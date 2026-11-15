using System.Text;
using System.Text.Json;
using SimulatorML.ML;
using SimulatorML.Models;
using SimulatorML.Services;
using SimulatorML.Simulation;
using SimulatorML.UI;
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

string csvDatasetPath = Path.Combine(baseDataDir, "bioship_36feat_240k_telemetry.csv");
string zipModelPath = Path.Combine(baseDataDir, "bioship_fasttree_model.zip");
string ensembleJsonPath = Path.Combine(baseDataDir, "bioship_fasttree_ensemble.json");
string nasaSnapshotJsonPath = Path.Combine(baseDataDir, "bioship_nasa_snapshot.json");

BioShipMlAutopilot autopilot = new(zipModelPath, csvDatasetPath, ensembleJsonPath, isPtBr);

if (exportOnly)
{
    Console.WriteLine(isPtBr
        ? "[CI/CD] Treinando Quad-Ensemble ML.NET com 240.000 amostras (80k ISS + 80k Lua + 80k Marte) e 36 variáveis..."
        : "[CI/CD] Training Quad-Ensemble ML.NET with 240,000 samples (80k ISS + 80k Moon + 80k Mars) & 36 features...");

    var multiSnapshot = await NasaApiClient.FetchAllNasaTelemetryAsync();
    File.WriteAllText(nasaSnapshotJsonPath, JsonSerializer.Serialize(multiSnapshot, new JsonSerializerOptions { WriteIndented = true }));

    autopilot.InitializeOrLoadModel(forceRetrain: true);

    Console.WriteLine(isPtBr
        ? $"[CI/CD] 4 Florestas FastTree exportadas em: {baseDataDir}"
        : $"[CI/CD] 4 FastTree Ensembles exported to: {baseDataDir}");
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
        isPtBr ? "[cyan1]Inicializando Motor Quad-Ensemble ML.NET (240.000 amostras / 36 features)...[/]"
               : "[cyan1]Initializing Quad-Ensemble 240,000-Sample / 36-Feature ML.NET Engine...[/]",
        async ctx =>
        {
            nasaSnapshot = await NasaApiClient.FetchAllNasaTelemetryAsync();
            File.WriteAllText(nasaSnapshotJsonPath, JsonSerializer.Serialize(nasaSnapshot, new JsonSerializerOptions { WriteIndented = true }));
            autopilot.InitializeOrLoadModel(forceRetrain: false);
        });

BioShipEngine ship = new(initialTarget, nasaSnapshot, autopilot, isPtBr);
bool isPaused = false;

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
                    case ConsoleKey.C: ship.CycleEngineeringPreset(); break;
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
                        break;
                    case ConsoleKey.R: ship.ForceEmergencyReturn(); break;
                    case ConsoleKey.Spacebar: isPaused = !isPaused; break;
                    case ConsoleKey.Q or ConsoleKey.Escape: cts.Cancel(); break;
                }
            }

            if (!isPaused && ship.CurrentStage != FlightStage.TouchdownEarthLanding)
            {
                ship.StepSimulation();
            }

            TuiDashboardRenderer.UpdateGuiLayout(rootLayout, ship, isPaused);
            ctx.Refresh();

            await Task.Delay(200, cts.Token).ContinueWith(_ => { });
        }
    });

Console.CursorVisible = true;

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
        string simWwwroot = Path.Combine(dir.FullName, "Simulator", "wwwroot");
        if (Directory.Exists(simWwwroot))
            return Path.Combine(simWwwroot, "MLArtifacts");

        dir = dir.Parent;
    }

    return Path.Combine(AppContext.BaseDirectory, "MLArtifacts");
}
