using System;

namespace Rcm.Sensors.Abstractions;

public sealed record class SensorMeasurement
{
    public required DateTimeOffset Time { get; init; }
    public required decimal CelsiusTemperature { get; init; }
    public required decimal RelativeHumidity { get; init; }
    public required decimal HpaPressure { get; init; }
}
