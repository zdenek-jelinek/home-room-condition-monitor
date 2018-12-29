using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rcm.Common;
using Rcm.DataCollection.Api;
using Rcm.Measurement.Api;

namespace Rcm.DataCollection
{
    public class MeasurementCollector : IMeasurementCollector
    {
        private static readonly TimeSpan MeasurementPeriod = TimeSpan.FromSeconds(6);

        private static readonly int MeasurementsPerMinute =
            (int)Math.Ceiling(TimeSpan.FromMinutes(1) / MeasurementPeriod);

        private readonly ILogger<MeasurementCollector> _logger;
        private readonly IClock _clock;
        private readonly IMeasurementProvider _measurementProvider;
        private readonly ICollectedDataStorage _collectedDataStorage;

        private readonly List<MeasurementEntry> _entries = new List<MeasurementEntry>(MeasurementsPerMinute);

        private int _measurementInProgress;

        public MeasurementCollector(
            ILogger<MeasurementCollector> logger,
            IClock clock,
            IMeasurementProvider measurementProvider,
            ICollectedDataStorage collectedDataStorage)
        {
            _logger = logger;
            _clock = clock;
            _measurementProvider = measurementProvider;
            _collectedDataStorage = collectedDataStorage;
        }

        public async Task MeasureAsync()
        {
            if (Interlocked.CompareExchange(ref _measurementInProgress, 1, 0) == 1)
            {
                _logger.LogWarning("Cancelling measurement as a previous measurement is still pending");
                return;
            }

            try
            {
                var measurement = await _measurementProvider.MeasureAsync();
                await AddMeasurementAsync(measurement);
            }
            finally
            {
                Interlocked.Exchange(ref _measurementInProgress, 0);
            }
        }

        private async Task AddMeasurementAsync(MeasurementEntry measurement)
        {
            if (_entries.Count != 0 && _entries[0].Time.Minute != measurement.Time.Minute)
            {
                await PropagateCollectedDataAsync(_entries);
                _entries.Clear();
            }

            _entries.Add(measurement);
        }

        private Task PropagateCollectedDataAsync(IReadOnlyCollection<MeasurementEntry> entries)
        {
            if (entries.Count == 0)
            {
                return Task.CompletedTask;
            }

            var averageValue = GetAverageValue(entries);

            return _collectedDataStorage.StoreAsync(averageValue);
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

        public (TimeSpan nextMeasurementDelay, TimeSpan measurementPeriod) MeasurementTimings
        {
            get
            {
                var now = _clock.Now;

                var nextMeasurementDelay = now.Second == 0 ? 0 : 60 - now.Second;

                return (
                    nextMeasurementDelay: TimeSpan.FromSeconds(nextMeasurementDelay),
                    measurementPeriod: MeasurementPeriod);
            }
        }
    }
}