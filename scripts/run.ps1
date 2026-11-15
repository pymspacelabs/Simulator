[CmdletBinding()]
param(
    [Parameter(Mandatory = $false, HelpMessage = "Operator switch to set verbosity")]
    [Alias("verb")]
    [switch]$v,
    [Parameter(Mandatory = $false, HelpMessage = "Operator switch to choice if run watch or not the wasm project")]
    [Alias("wasm")]
    [switch]$w)

$root = Resolve-Path $PSScriptRoot\..

$verbosity = "normal"

$watch = $false

$ml_net = Resolve-Path $(Join-Path $root ".\src\SimulatorML\SimulatorML.csproj")
$b_wasm = Resolve-Path $(Join-Path $root ".\src\Simulator\Simulator.csproj")

Push-Location $root

if ($v) {
    $verbosity = "diagnostic"
}

if ($w) {
    $watch = $true
}

dotnet run --project $ml_net -- --export-only  --verbosity $verbosity

if ($watch -eq $true) {
    dotnet watch --project $b_wasm --verbosity $verbosity
}

Pop-Location 
