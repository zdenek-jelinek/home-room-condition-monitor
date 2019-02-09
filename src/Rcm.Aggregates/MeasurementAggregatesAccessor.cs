using System;
using System.Collections.Generic;
using Rcm.Aggregates.Api;
using Rcm.Common;
using Rcm.DataCollection.Api;

namespace Rcm.Aggregates
{
    public class MeasurementAggregatesAccessor : IMeasurementAggregatesAccessor
    {
        private readonly ICollectedDataAccessor _collectedDataAccessor;

        public MeasurementAggregatesAccessor(ICollectedDataAccessor collectedDataAccessor)
        {
            _collectedDataAccessor = collectedDataAccessor;
        }

        public IEnumerable<MeasurementAggregates> GetMeasurementAggregates(DateTimeOffset startTime, DateTimeOffset endTime, int aggregatesCount)
        {
            var measurements = _collectedDataAccessor.GetCollectedData(startTime, endTime);
            
            var partitionSize = (endTime - startTime).Ticks / (double)aggregatesCount;
            
            var previousMeasurement = (MeasurementEntry)null;
            
            var currentPartitionEndOffset = partitionSize;
            var currentPartitionEndTime = startTime.AddTicks((long)Math.Round(currentPartitionEndOffset));

            var currentAggregate = new AggregateAccumulator();

            foreach (var measurement in measurements)
            {
                if (!(previousMeasurement is null) && measurement.Time < previousMeasurement.Time)
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
                    currentPartitionEndTime = startTime.AddTicks((long)Math.Round(currentPartitionEndOffset));
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

            private readonly SubAccumulator _temperatureAccumulator = new SubAccumulator(e => e.CelsiusTemperature);
            private readonly SubAccumulator _pressureAccumulator = new SubAccumulator(e => e.HpaPressure);
            private readonly SubAccumulator _humidityAccumulator = new SubAccumulator(e => e.RelativeHumidity);

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
                    return null;
                }

                return new MeasurementAggregates(
                    _temperatureAccumulator.ExtractResult(),
                    _pressureAccumulator.ExtractResult(),
                    _humidityAccumulator.ExtractResult());
            }

            private class SubAccumulator
            {
                private readonly Func<MeasurementEntry, decimal> _selector;

                private DateTimeOffset _minTime = DateTimeOffset.MaxValue;
                private decimal _minTimeValue;
                private decimal _minValue = Decimal.MaxValue;
                private DateTimeOffset _minValueTime;
                private decimal _maxValue = Decimal.MinValue;
                private DateTimeOffset _maxValueTime;
                private DateTimeOffset _maxTime = DateTimeOffset.MinValue;
                private decimal _maxTimeValue;

                public SubAccumulator(Func<MeasurementEntry, decimal> selector)
                {
                    _selector = selector;
                }

                public void Add(MeasurementEntry entry)
                {
                    var value = _selector.Invoke(entry);
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

                public Api.Aggregates ExtractResult()
                {
                    return new Api.Aggregates(
                        new AggregateEntry(_minTime, _minTimeValue),
                        new AggregateEntry(_minValueTime, _minValue),
                        new AggregateEntry(_maxValueTime, _maxValue),
                        new AggregateEntry(_maxTime, _maxTimeValue));
                }
            }
        }
    }
}
