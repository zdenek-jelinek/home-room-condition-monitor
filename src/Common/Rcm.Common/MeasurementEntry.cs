using System;

namespace Rcm.Common;

public class MeasurementEntry
{
    public DateTimeOffset Time { get; }
    public decimal CelsiusTemperature { get; }
    public decimal RelativeHumidity { get; }
    public decimal HpaPressure { get; }

    public MeasurementEntry(DateTimeOffset time, decimal celsiusTemperature, decimal relativeHumidity, decimal hpaPressure)
    {
        Time = time;
        CelsiusTemperature = celsiusTemperature;
        RelativeHumidity = relativeHumidity;
        HpaPressure = hpaPressure;
    }

    public override string ToString()
    {
        return $"time: {Time:o}, temperature: {CelsiusTemperature:0.0}°C, "
            + $"humidity: {RelativeHumidity:0.0}%, pressure: {HpaPressure:0.0}hPa";
    }
}