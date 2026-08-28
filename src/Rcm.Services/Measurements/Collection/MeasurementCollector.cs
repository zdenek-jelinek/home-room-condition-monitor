using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rcm.Common;
using Rcm.Persistence.Abstractions;
using Rcm.Sensors.Abstractions;

namespace Rcm.Services.Measurements.Collection;

public class MeasurementCollector(ILogger<MeasurementCollector> logger, ISensor sensor, IMeasurementsWriter measurementsWriter) : IMeasurementCollector
{
    private readonly List<SensorMeasurement> _measurements = new();

    private int _measurementInProgress;

    public async Task MeasureAsync(CancellationToken token)
    {
        if (Interlocked.CompareExchange(ref _measurementInProgress, 1, 0) == 1)
        {
            logger.LogWarning("Cancelling measurement as a previous measurement is still pending");
            return;
        }

        try
        {
            var measurement = await sensor.ReadMeasurementAsync(token);
            await AddMeasurementAsync(measurement, token);
        }
        finally
        {
            Interlocked.Exchange(ref _measurementInProgress, 0);
        }
    }


    private async Task AddMeasurementAsync(SensorMeasurement measurement, CancellationToken token)
    {
        logger.LogTrace("Adding new record {Record}", measurement);

        if (_measurements.Count != 0 && _measurements[0].Time.Minute != measurement.Time.Minute)
        {
            logger.LogTrace("Persisting previous minute measurement records");
            await PropagateCollectedDataAsync(_measurements, token);
            _measurements.Clear();
        }

        logger.LogTrace("Storing record for further processing");
        _measurements.Add(measurement);
    }

    private Task PropagateCollectedDataAsync(IReadOnlyCollection<SensorMeasurement> measurements, CancellationToken token)
    {
        if (measurements.Count == 0)
        {
            return Task.CompletedTask;
        }

        var averageValue = GetAverageValue(measurements);

        return measurementsWriter.StoreAsync(averageValue, token);
    }

    private static MeasurementEntry GetAverageValue(IReadOnlyCollection<SensorMeasurement> measurements)
    {
        var (totalTemperature, totalPressure, totalHumidity) =
            measurements.Aggregate(
                (temperature: 0.0m, pressure: 0.0m, humidity: 0.0m),
                (acc, entry) => (
                    temperature: acc.temperature + entry.CelsiusTemperature,
                    pressure: acc.pressure + entry.HpaPressure,
                    humidity: acc.humidity + entry.RelativeHumidity));

        var averageTemperature = totalTemperature / measurements.Count;
        var averageHumidity = totalHumidity / measurements.Count;
        var averagePressure = totalPressure / measurements.Count;

        var firstEntryTime = measurements.First().Time;
        var time = new DateTimeOffset(
            firstEntryTime.Year,
            firstEntryTime.Month,
            firstEntryTime.Day,
            firstEntryTime.Hour,
            firstEntryTime.Minute,
            second: 0,
            firstEntryTime.Offset);

        return new()
        {
            Time = time,
            CelsiusTemperature = averageTemperature,
            RelativeHumidity = averageHumidity,
            HpaPressure = averagePressure
        };
    }
}
