using System;
using System.Threading;
using System.Threading.Tasks;
using Rcm.Common.Temporal;
using Rcm.Sensors.Abstractions;

namespace Rcm.Sensors.Fakes;

public class FakeSensor(IClock clock) : ISensor
{
    private readonly Random _random = new();

    public Task<SensorMeasurement> ReadMeasurementAsync(CancellationToken token)
    {
        return Task.FromResult(GenerateMeasurement());
    }

    private SensorMeasurement GenerateMeasurement()
    {
        var now = clock.Now;

        var baseTemperature = 15 + 10 * Math.Sin(Math.PI * now.Month / 12.0);
        var temperature = baseTemperature - 8 * Math.Sin(Math.PI * (now.Hour + 6) / 12.0);

        return new()
        {
            Time = now,
            CelsiusTemperature = (decimal)temperature,
            RelativeHumidity = _random.Next(3000, 6000) / 100m,
            HpaPressure = _random.Next(95000, 105000) / 100m
        };
    }
}
