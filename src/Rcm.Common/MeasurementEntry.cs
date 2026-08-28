using System;

namespace Rcm.Common;

public sealed record class MeasurementEntry
{
    public required DateTimeOffset Time { get; init; }
    public required decimal CelsiusTemperature { get; init; }
    public required decimal RelativeHumidity { get; init; }
    public required decimal HpaPressure { get; init; }

    public override string ToString()
    {
        return $"time: {Time:o}, temperature: {CelsiusTemperature:0.0}°C, "
            + $"humidity: {RelativeHumidity:0.0}%, pressure: {HpaPressure:0.0}hPa";
    }
}
