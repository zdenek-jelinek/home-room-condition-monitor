namespace Rcm.Sensors.Bme280;

public sealed record class TemperatureCompensationParameters
{
    public required int Temperature1 { get; init; }
    public required int Temperature2 { get; init; }
    public required int Temperature3 { get; init; }
}
