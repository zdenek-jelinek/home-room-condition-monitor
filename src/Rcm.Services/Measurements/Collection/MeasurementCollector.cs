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
    private readonly List<MeasurementEntry> _entries = new();

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
            await AddMeasurementAsync(MapMeasurement(measurement), token);
        }
        finally
        {
            Interlocked.Exchange(ref _measurementInProgress, 0);
        }
    }

    private static MeasurementEntry MapMeasurement(SensorMeasurement measurement)
    {
        return new(measurement.Time, measurement.CelsiusTemperature, measurement.RelativeHumidity, measurement.HpaPressure);
    }

    private async Task AddMeasurementAsync(MeasurementEntry measurement, CancellationToken token)
    {
        logger.LogTrace("Adding new record {Record}", measurement);

        if (_entries.Count != 0 && _entries[0].Time.Minute != measurement.Time.Minute)
        {
            logger.LogTrace("Persisting previous minute measurement records");
            await PropagateCollectedDataAsync(_entries, token);
            _entries.Clear();
        }

        logger.LogTrace("Storing record for further processing");
        _entries.Add(measurement);
    }

    private Task PropagateCollectedDataAsync(IReadOnlyCollection<MeasurementEntry> entries, CancellationToken token)
    {
        if (entries.Count == 0)
        {
            return Task.CompletedTask;
        }

        var averageValue = GetAverageValue(entries);

        return measurementsWriter.StoreAsync(averageValue, token);
    }

    private MeasurementEntry GetAverageValue(IReadOnlyCollection<MeasurementEntry> entries)
    {
        var (totalTemperature, totalPressure, totalHumidity) =
            entries.Aggregate(
                (temperature: 0.0m, pressure: 0.0m, humidity: 0.0m),
                (acc, entry) => (
                    temperature: acc.temperature + entry.CelsiusTemperature,
                    pressure: acc.pressure + entry.HpaPressure,
                    humidity: acc.humidity + entry.RelativeHumidity));

        var averageTemperature = totalTemperature / entries.Count;
        var averageHumidity = totalHumidity / entries.Count;
        var averagePressure = totalPressure / entries.Count;

        var firstEntryTime = entries.First().Time;
        var time = new DateTimeOffset(
            firstEntryTime.Year,
            firstEntryTime.Month,
            firstEntryTime.Day,
            firstEntryTime.Hour,
            firstEntryTime.Minute,
            second: 0,
            firstEntryTime.Offset);

        return new MeasurementEntry(
            time: time,
            celsiusTemperature: averageTemperature,
            relativeHumidity: averageHumidity,
            hpaPressure: averagePressure);
    }
}
