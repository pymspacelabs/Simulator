namespace BioshipSimulator.Models;

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
