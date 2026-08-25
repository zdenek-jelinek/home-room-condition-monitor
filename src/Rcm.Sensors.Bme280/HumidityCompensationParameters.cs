namespace Rcm.Sensors.Bme280;

public sealed record class HumidityCompensationParameters
{
    public required int Humidity1 { get; init; }
    public required int Humidity2 { get; init; }
    public required int Humidity3 { get; init; }
    public required int Humidity4 { get; init; }
    public required int Humidity5 { get; init; }
    public required int Humidity6 { get; init; }
}
