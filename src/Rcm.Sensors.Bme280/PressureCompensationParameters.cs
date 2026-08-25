namespace Rcm.Sensors.Bme280;

public sealed record class PressureCompensationParameters
{
    public required long Pressure1 { get; init; }
    public required long Pressure2 { get; init; }
    public required long Pressure3 { get; init; }
    public required long Pressure4 { get; init; }
    public required long Pressure5 { get; init; }
    public required long Pressure6 { get; init; }
    public required long Pressure7 { get; init; }
    public required long Pressure8 { get; init; }
    public required long Pressure9 { get; init; }
}
