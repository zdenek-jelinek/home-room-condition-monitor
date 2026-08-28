using System;
using System.Collections.Generic;
using System.Threading;
using Rcm.Common;
using Rcm.Persistence.Abstractions;

namespace Rcm.Services.Aggregates;

public class MeasurementAggregatesAccessor(IMeasurementsReader measurementsReader) : IMeasurementAggregatesAccessor
{
    public IEnumerable<MeasurementAggregates> GetMeasurementAggregates(MeasurementAggregatesQuery query, CancellationToken cancellationToken)
    {
        var measurements = measurementsReader.GetCollectedData(query.StartTime, query.EndTime, cancellationToken);

        var partitionSize = (query.EndTime - query.StartTime).Ticks / (double)query.PartitionCount;

        var previousMeasurement = (MeasurementEntry?)null;

        var currentPartitionEndOffset = partitionSize;
        var currentPartitionEndTime = query.StartTime.AddTicks((long)Math.Round(currentPartitionEndOffset));

        var currentAggregate = new AggregateAccumulator();

        foreach (var measurement in measurements)
        {
            if (previousMeasurement != null && measurement.Time < previousMeasurement.Time)
            {
                throw new NotSupportedException($"Non-monotonous measurement times are not supported for partitioning. " +
                    $"Got measurement on {previousMeasurement.Time} followed by measurement on {measurement.Time}");
            }

            while (measurement.Time > currentPartitionEndTime)
            {
                if (!currentAggregate.IsEmpty)
                {
                    yield return currentAggregate.ExtractResult();
                    currentAggregate = new AggregateAccumulator();
                }

                currentPartitionEndOffset += partitionSize;
                currentPartitionEndTime = query.StartTime.AddTicks((long)Math.Round(currentPartitionEndOffset));
            }

            currentAggregate.Add(measurement);

            previousMeasurement = measurement;
        }

        if (!currentAggregate.IsEmpty)
        {
            yield return currentAggregate.ExtractResult();
        }
    }

    private class AggregateAccumulator
    {
        public bool IsEmpty { get; private set; } = true;

        private readonly SubAccumulator _temperatureAccumulator = new(e => e.CelsiusTemperature);
        private readonly SubAccumulator _pressureAccumulator = new(e => e.HpaPressure);
        private readonly SubAccumulator _humidityAccumulator = new(e => e.RelativeHumidity);

        public void Add(MeasurementEntry entry)
        {
            IsEmpty = false;

            _temperatureAccumulator.Add(entry);
            _pressureAccumulator.Add(entry);
            _humidityAccumulator.Add(entry);
        }

        public MeasurementAggregates ExtractResult()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Cannot extract result of empty aggregate");
            }

            return new MeasurementAggregates(
                _temperatureAccumulator.ExtractResult(),
                _pressureAccumulator.ExtractResult(),
                _humidityAccumulator.ExtractResult());
        }

        private class SubAccumulator(Func<MeasurementEntry, decimal> selector)
        {
            private DateTimeOffset _minTime = DateTimeOffset.MaxValue;
            private decimal _minTimeValue;
            private decimal _minValue = Decimal.MaxValue;
            private DateTimeOffset _minValueTime;
            private decimal _maxValue = Decimal.MinValue;
            private DateTimeOffset _maxValueTime;
            private DateTimeOffset _maxTime = DateTimeOffset.MinValue;
            private decimal _maxTimeValue;

            public void Add(MeasurementEntry entry)
            {
                var value = selector.Invoke(entry);
                if (entry.Time < _minTime)
                {
                    _minTime = entry.Time;
                    _minTimeValue = value;
                }

                if (entry.Time > _maxTime)
                {
                    _maxTime = entry.Time;
                    _maxTimeValue = value;
                }

                if (value < _minValue)
                {
                    _minValueTime = entry.Time;
                    _minValue = value;
                }

                if (value > _maxValue)
                {
                    _maxValueTime = entry.Time;
                    _maxValue = value;
                }
            }

            public Aggregates ExtractResult()
            {
                return new Aggregates(
                    new AggregateEntry(_minTime, _minTimeValue),
                    new AggregateEntry(_minValueTime, _minValue),
                    new AggregateEntry(_maxValueTime, _maxValue),
                    new AggregateEntry(_maxTime, _maxTimeValue));
            }
        }
    }
}
