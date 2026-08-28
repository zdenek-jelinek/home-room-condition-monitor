using System;
using Rcm.Common;
using Rcm.Testing;

namespace Rcm.UnitTests;

public static class MeasurementEntryFactory
{
    public static MeasurementEntry Make(DateTimeOffset? time = null, decimal? temperature = null, decimal? humidity = null, decimal? pressure = null)
    {
        if (time != null && temperature != null && humidity != null && pressure != null)
        {
            return new() { Time = time.Value, CelsiusTemperature = temperature.Value, RelativeHumidity = humidity.Value, HpaPressure = pressure.Value };
        }

        var seed = NumericSequence.Next();

        return new()
        {
            Time = time ?? new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.Zero) + TimeSpan.FromHours(seed),
            CelsiusTemperature = temperature ?? seed % 40,
            RelativeHumidity = humidity ?? (seed + 20) % 100,
            HpaPressure = pressure ?? (seed + 900) % 1200
        };
    }
}
