using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Rcm.Common;
using Rcm.Persistence.Abstractions;
using Rcm.Services.Aggregates;

namespace Rcm.UnitTests.Services.Aggregates;

[TestFixture]
public class MeasurementAggregatesAccessorTests
{
    [Test]
    public void CalculatesAggregatesFromMeasurementsWithinCorrespondingTimeRange()
    {
        // Given
        var startTime = new DateTimeOffset(2019, 2, 8, 12, 0, 0, TimeSpan.FromHours(1));
        var endTime = startTime + TimeSpan.FromHours(4);
        var partitionCount = 2;

        var firstPartitionEndTime = startTime + GetPartitionDuration(startTime, endTime, partitionCount);

        var firstMeasurementInFirstPartition = MakeMeasurementEntry(startTime, 20m, 30m, 900m);
        var maxTemperatureMeasurementInFirstPartition = MakeMeasurementEntry(startTime + TimeSpan.FromMinutes(17), 35m, 30m, 900m);
        var minTemperatureMeasurementInFirstPartition = MakeMeasurementEntry(startTime + TimeSpan.FromMinutes(26), 0m, 30m, 900m);
        var maxPressureMeasurementInFirstPartition = MakeMeasurementEntry(startTime + TimeSpan.FromMinutes(35), 35m, 30m, 1100m);
        var minPressureMeasurementInFirstPartition = MakeMeasurementEntry(startTime + TimeSpan.FromMinutes(42), 35m, 30m, 750m);
        var maxHumidityMeasurementInFirstPartition = MakeMeasurementEntry(startTime + TimeSpan.FromMinutes(91), 20m, 55m, 900m);
        var minHumidityMeasurementInFirstPartition = MakeMeasurementEntry(startTime + TimeSpan.FromMinutes(110), 20m, 15m, 900m);
        var lastMeasurementInFirstPartition = MakeMeasurementEntry(firstPartitionEndTime, 20m, 15m, 900m);

        var firstMeasurementInSecondPartition = MakeMeasurementEntry(firstPartitionEndTime + TimeSpan.FromMinutes(1), 20m, 30m, 900m);
        var maxTemperatureMeasurementInSecondPartition = MakeMeasurementEntry(firstPartitionEndTime + TimeSpan.FromMinutes(17), 35m, 30m, 900m);
        var minTemperatureMeasurementInSecondPartition = MakeMeasurementEntry(firstPartitionEndTime + TimeSpan.FromMinutes(26), 0m, 30m, 900m);
        var maxPressureMeasurementInSecondPartition = MakeMeasurementEntry(firstPartitionEndTime + TimeSpan.FromMinutes(35), 35m, 30m, 1100m);
        var minPressureMeasurementInSecondPartition = MakeMeasurementEntry(firstPartitionEndTime + TimeSpan.FromMinutes(42), 35m, 30m, 750m);
        var maxHumidityMeasurementInSecondPartition = MakeMeasurementEntry(firstPartitionEndTime + TimeSpan.FromMinutes(91), 20m, 55m, 900m);
        var minHumidityMeasurementInSecondPartition = MakeMeasurementEntry(firstPartitionEndTime + TimeSpan.FromMinutes(110), 20m, 15m, 900m);
        var lastMeasurementInSecondPartition = MakeMeasurementEntry(endTime, 20m, 15m, 900m);

        var measurements = new[]
        {
            firstMeasurementInFirstPartition,
            maxTemperatureMeasurementInFirstPartition,
            minTemperatureMeasurementInFirstPartition,
            maxPressureMeasurementInFirstPartition,
            minPressureMeasurementInFirstPartition,
            maxHumidityMeasurementInFirstPartition,
            minHumidityMeasurementInFirstPartition,
            lastMeasurementInFirstPartition,
            firstMeasurementInSecondPartition,
            maxTemperatureMeasurementInSecondPartition,
            minTemperatureMeasurementInSecondPartition,
            maxPressureMeasurementInSecondPartition,
            minPressureMeasurementInSecondPartition,
            maxHumidityMeasurementInSecondPartition,
            minHumidityMeasurementInSecondPartition,
            lastMeasurementInSecondPartition
        };

        // When
        var aggregates = GetMeasurementAggregates(measurements, MakeQuery(startTime, endTime, partitionCount));

        // Then
        Assert.AreEqual(partitionCount, aggregates.Count);

        Assert.AreEqual(firstMeasurementInFirstPartition.Time, aggregates[0].Temperature.First.Time);
        Assert.AreEqual(firstMeasurementInFirstPartition.Time, aggregates[0].Pressure.First.Time);
        Assert.AreEqual(firstMeasurementInFirstPartition.Time, aggregates[0].Humidity.First.Time);
        Assert.AreEqual(firstMeasurementInFirstPartition.CelsiusTemperature, aggregates[0].Temperature.First.Value);
        Assert.AreEqual(firstMeasurementInFirstPartition.HpaPressure, aggregates[0].Pressure.First.Value);
        Assert.AreEqual(firstMeasurementInFirstPartition.RelativeHumidity, aggregates[0].Humidity.First.Value);

        Assert.AreEqual(maxTemperatureMeasurementInFirstPartition.Time, aggregates[0].Temperature.Max.Time);
        Assert.AreEqual(maxTemperatureMeasurementInFirstPartition.CelsiusTemperature, aggregates[0].Temperature.Max.Value);

        Assert.AreEqual(minTemperatureMeasurementInFirstPartition.Time, aggregates[0].Temperature.Min.Time);
        Assert.AreEqual(minTemperatureMeasurementInFirstPartition.CelsiusTemperature, aggregates[0].Temperature.Min.Value);

        Assert.AreEqual(maxPressureMeasurementInFirstPartition.Time, aggregates[0].Pressure.Max.Time);
        Assert.AreEqual(maxPressureMeasurementInFirstPartition.HpaPressure, aggregates[0].Pressure.Max.Value);

        Assert.AreEqual(minPressureMeasurementInFirstPartition.Time, aggregates[0].Pressure.Min.Time);
        Assert.AreEqual(minPressureMeasurementInFirstPartition.HpaPressure, aggregates[0].Pressure.Min.Value);

        Assert.AreEqual(maxHumidityMeasurementInFirstPartition.Time, aggregates[0].Humidity.Max.Time);
        Assert.AreEqual(maxHumidityMeasurementInFirstPartition.RelativeHumidity, aggregates[0].Humidity.Max.Value);

        Assert.AreEqual(minHumidityMeasurementInFirstPartition.Time, aggregates[0].Humidity.Min.Time);
        Assert.AreEqual(minHumidityMeasurementInFirstPartition.RelativeHumidity, aggregates[0].Humidity.Min.Value);

        Assert.AreEqual(lastMeasurementInFirstPartition.Time, aggregates[0].Temperature.Last.Time);
        Assert.AreEqual(lastMeasurementInFirstPartition.Time, aggregates[0].Pressure.Last.Time);
        Assert.AreEqual(lastMeasurementInFirstPartition.Time, aggregates[0].Humidity.Last.Time);
        Assert.AreEqual(lastMeasurementInFirstPartition.CelsiusTemperature, aggregates[0].Temperature.Last.Value);
        Assert.AreEqual(lastMeasurementInFirstPartition.HpaPressure, aggregates[0].Pressure.Last.Value);
        Assert.AreEqual(lastMeasurementInFirstPartition.RelativeHumidity, aggregates[0].Humidity.Last.Value);

        Assert.AreEqual(firstMeasurementInSecondPartition.Time, aggregates[1].Temperature.First.Time);
        Assert.AreEqual(firstMeasurementInSecondPartition.Time, aggregates[1].Pressure.First.Time);
        Assert.AreEqual(firstMeasurementInSecondPartition.Time, aggregates[1].Humidity.First.Time);
        Assert.AreEqual(firstMeasurementInSecondPartition.CelsiusTemperature, aggregates[1].Temperature.First.Value);
        Assert.AreEqual(firstMeasurementInSecondPartition.HpaPressure, aggregates[1].Pressure.First.Value);
        Assert.AreEqual(firstMeasurementInSecondPartition.RelativeHumidity, aggregates[1].Humidity.First.Value);

        Assert.AreEqual(maxTemperatureMeasurementInSecondPartition.Time, aggregates[1].Temperature.Max.Time);
        Assert.AreEqual(maxTemperatureMeasurementInSecondPartition.CelsiusTemperature, aggregates[1].Temperature.Max.Value);

        Assert.AreEqual(minTemperatureMeasurementInSecondPartition.Time, aggregates[1].Temperature.Min.Time);
        Assert.AreEqual(minTemperatureMeasurementInSecondPartition.CelsiusTemperature, aggregates[1].Temperature.Min.Value);

        Assert.AreEqual(maxPressureMeasurementInSecondPartition.Time, aggregates[1].Pressure.Max.Time);
        Assert.AreEqual(maxPressureMeasurementInSecondPartition.HpaPressure, aggregates[1].Pressure.Max.Value);

        Assert.AreEqual(minPressureMeasurementInSecondPartition.Time, aggregates[1].Pressure.Min.Time);
        Assert.AreEqual(minPressureMeasurementInSecondPartition.HpaPressure, aggregates[1].Pressure.Min.Value);

        Assert.AreEqual(maxHumidityMeasurementInSecondPartition.Time, aggregates[1].Humidity.Max.Time);
        Assert.AreEqual(maxHumidityMeasurementInSecondPartition.RelativeHumidity, aggregates[1].Humidity.Max.Value);

        Assert.AreEqual(minHumidityMeasurementInSecondPartition.Time, aggregates[1].Humidity.Min.Time);
        Assert.AreEqual(minHumidityMeasurementInSecondPartition.RelativeHumidity, aggregates[1].Humidity.Min.Value);

        Assert.AreEqual(lastMeasurementInSecondPartition.Time, aggregates[1].Temperature.Last.Time);
        Assert.AreEqual(lastMeasurementInSecondPartition.Time, aggregates[1].Pressure.Last.Time);
        Assert.AreEqual(lastMeasurementInSecondPartition.Time, aggregates[1].Humidity.Last.Time);
        Assert.AreEqual(lastMeasurementInSecondPartition.CelsiusTemperature, aggregates[1].Temperature.Last.Value);
        Assert.AreEqual(lastMeasurementInSecondPartition.HpaPressure, aggregates[1].Pressure.Last.Value);
        Assert.AreEqual(lastMeasurementInSecondPartition.RelativeHumidity, aggregates[1].Humidity.Last.Value);
    }

    [Test]
    public void ReturnsSingleAggregationOfSelectedRangeForCountEqualToOne()
    {
        // Given
        var dummyStartTime = new DateTimeOffset(2019, 2, 7, 21, 48, 15, TimeSpan.FromHours(1));
        var dummyEndTime = dummyStartTime + TimeSpan.FromDays(1);

        var count = 1;
        var measurements = new[]
        {
            MakeMeasurementEntry(time: dummyStartTime + TimeSpan.FromHours(1)),
            MakeMeasurementEntry(time: dummyStartTime + TimeSpan.FromHours(6)),
            MakeMeasurementEntry(time: dummyStartTime + TimeSpan.FromHours(12)),
            MakeMeasurementEntry(time: dummyStartTime + TimeSpan.FromHours(16)),
            MakeMeasurementEntry(time: dummyStartTime + TimeSpan.FromHours(19))
        };

        // When
        var aggregates = GetMeasurementAggregates(measurements, MakeQuery(dummyStartTime, dummyEndTime, count));

        // Then
        var aggregate = aggregates.Single();
        var measurementsByTime = measurements.OrderBy(m => m.Time);
        var measurementsByTemperature = measurements.OrderBy(m => m.CelsiusTemperature);
        var measurementsByPressure = measurements.OrderBy(m => m.HpaPressure);
        var measurementsByHumidity = measurements.OrderBy(m => m.RelativeHumidity);

        Assert.AreEqual(measurementsByTime.First().Time, aggregate.Temperature.First.Time);
        Assert.AreEqual(measurementsByTime.First().CelsiusTemperature, aggregate.Temperature.First.Value);
        Assert.AreEqual(measurementsByTemperature.First().Time, aggregate.Temperature.Min.Time);
        Assert.AreEqual(measurementsByTemperature.First().CelsiusTemperature, aggregate.Temperature.Min.Value);
        Assert.AreEqual(measurementsByTemperature.Last().Time, aggregate.Temperature.Max.Time);
        Assert.AreEqual(measurementsByTemperature.Last().CelsiusTemperature, aggregate.Temperature.Max.Value);
        Assert.AreEqual(measurementsByTime.Last().Time, aggregate.Temperature.Last.Time);
        Assert.AreEqual(measurementsByTime.Last().CelsiusTemperature, aggregate.Temperature.Last.Value);

        Assert.AreEqual(measurementsByTime.First().Time, aggregate.Pressure.First.Time);
        Assert.AreEqual(measurementsByTime.First().HpaPressure, aggregate.Pressure.First.Value);
        Assert.AreEqual(measurementsByPressure.First().Time, aggregate.Pressure.Min.Time);
        Assert.AreEqual(measurementsByPressure.First().HpaPressure, aggregate.Pressure.Min.Value);
        Assert.AreEqual(measurementsByPressure.Last().Time, aggregate.Pressure.Max.Time);
        Assert.AreEqual(measurementsByPressure.Last().HpaPressure, aggregate.Pressure.Max.Value);
        Assert.AreEqual(measurementsByTime.Last().Time, aggregate.Pressure.Last.Time);
        Assert.AreEqual(measurementsByTime.Last().HpaPressure, aggregate.Pressure.Last.Value);

        Assert.AreEqual(measurementsByTime.First().Time, aggregate.Humidity.First.Time);
        Assert.AreEqual(measurementsByTime.First().RelativeHumidity, aggregate.Humidity.First.Value);
        Assert.AreEqual(measurementsByHumidity.First().Time, aggregate.Humidity.Min.Time);
        Assert.AreEqual(measurementsByHumidity.First().RelativeHumidity, aggregate.Humidity.Min.Value);
        Assert.AreEqual(measurementsByHumidity.Last().Time, aggregate.Humidity.Max.Time);
        Assert.AreEqual(measurementsByHumidity.Last().RelativeHumidity, aggregate.Humidity.Max.Value);
        Assert.AreEqual(measurementsByTime.Last().Time, aggregate.Humidity.Last.Time);
        Assert.AreEqual(measurementsByTime.Last().RelativeHumidity, aggregate.Humidity.Last.Value);
    }

    [Test]
    public void ConsidersAllMeasurementsForUnevenPartitionSizes()
    {
        // Given
        var startTime = new DateTimeOffset(2019, 2, 9, 10, 0, 0, TimeSpan.FromHours(1));
        var endTime = startTime + TimeSpan.FromHours(1);
        var partitionCount = 2;
        var secondPartitionStart = startTime + GetPartitionDuration(startTime, endTime, partitionCount);

        var measurementInFirstPartition = MakeMeasurementEntry(startTime, 10m, 20m, 900m);
        var measurementOnBorderOfPartitions = MakeMeasurementEntry(secondPartitionStart, 20m, 30m, 950m);
        var measurementInSecondPartition = MakeMeasurementEntry(endTime, 30m, 40m, 1000m);

        // When
        var aggregates = GetMeasurementAggregates(
            measurements: [measurementInFirstPartition, measurementOnBorderOfPartitions, measurementInSecondPartition],
            query: MakeQuery(startTime, endTime, partitionCount));

        // Then
        var firstPartitionAggregates = MakeAggregates(
            first: measurementInFirstPartition,
            min: measurementInFirstPartition,
            max: measurementOnBorderOfPartitions,
            last: measurementOnBorderOfPartitions);

        var secondPartitionAggregates = MakeSingletonAggregates(measurementInSecondPartition);

        Assert.That(
            aggregates,
            Is.EquivalentTo(new[] { firstPartitionAggregates, secondPartitionAggregates })
                .Using(new MeasurementAggregatesEqualityComparer()));
    }

    [Test]
    public void NoAggregatesAreReturnedForAPartitionIfThereAreNoMeasurementsInThePartition()
    {
        // Given
        var startTime = new DateTimeOffset(2019, 2, 9, 10, 0, 0, TimeSpan.FromHours(1));
        var endTime = startTime + TimeSpan.FromHours(1);
        var partitionCount = 2;
        var secondPartitionStart = startTime + GetPartitionDuration(startTime, endTime, partitionCount);

        var measurementInSecondPartition = MakeMeasurementEntry(secondPartitionStart + TimeSpan.FromMinutes(5));

        var query = MakeQuery(startTime, endTime, partitionCount);

        // When
        var aggregates = GetMeasurementAggregates([measurementInSecondPartition], query);

        // Then
        Assert.That(
            aggregates,
            Is.EquivalentTo(new[] { MakeSingletonAggregates(measurementInSecondPartition) })
                .Using(new MeasurementAggregatesEqualityComparer()));
    }

    [Test]
    public void NoAggregatesAreReturnedIfThereAreNoMeasurements()
    {
        // When
        var aggregates = GetMeasurementAggregates(measurements: [], MakeDummyQuery());

        // Then
        CollectionAssert.IsEmpty(aggregates);
    }

    [Test]
    public void ThrowsOnEvaluationForDecreasingMeasurementTimes()
    {
        // Given
        var query = MakeDummyQuery();

        var decreasingTimeMeasurements = new[]
        {
            MakeMeasurementEntry(query.StartTime + TimeSpan.FromMinutes(10)),
            MakeMeasurementEntry(query.StartTime)
        };

        // When
        void GetAggregatesForDecreasingMeasurementTimes()
        {
            _ = GetMeasurementAggregates(decreasingTimeMeasurements, query);
        }

        // Then
        _ = Assert.Catch(GetAggregatesForDecreasingMeasurementTimes);
    }

    private static TimeSpan GetPartitionDuration(DateTimeOffset startTime, DateTimeOffset endTime, int partitionCount)
    {
        return (endTime - startTime) / partitionCount;
    }

    private static MeasurementAggregatesQuery MakeDummyQuery()
    {
        var dummyStartTime = new DateTimeOffset(2019, 2, 7, 12, 0, 0, TimeSpan.FromHours(1));

        return MakeQuery(dummyStartTime, dummyStartTime + TimeSpan.FromDays(1), 3);
    }

    private static MeasurementAggregatesQuery MakeQuery(DateTimeOffset startTime, DateTimeOffset endTime, int partitionCount)
    {
        return new() { StartTime = startTime, EndTime = endTime, PartitionCount = partitionCount };
    }

    private static MeasurementEntry MakeMeasurementEntry(
        DateTimeOffset? time = null,
        decimal? temperature = null,
        decimal? humidity = null,
        decimal? pressure = null)
    {
        return MeasurementEntryFactory.Make(time, temperature, humidity, pressure);
    }

    private static MeasurementAggregates MakeSingletonAggregates(MeasurementEntry measurement)
    {
        return MakeAggregates(first: measurement, min: measurement, max: measurement, last: measurement);
    }

    private static MeasurementAggregates MakeAggregates(MeasurementEntry first, MeasurementEntry min, MeasurementEntry max, MeasurementEntry last)
    {
        return new()
        {
            Temperature = MakeDimension(m => m.CelsiusTemperature),
            Humidity = MakeDimension(m => m.RelativeHumidity),
            Pressure = MakeDimension(m => m.HpaPressure)
        };

        Rcm.Services.Aggregates.Aggregates MakeDimension(Func<MeasurementEntry, decimal> propertySelector)
        {
            return new() { First = MakeComponent(first), Min = MakeComponent(min), Max = MakeComponent(max), Last = MakeComponent(last) };

            AggregateEntry MakeComponent(MeasurementEntry measurement)
            {
                return new() { Time = measurement.Time, Value = propertySelector.Invoke(measurement) };
            }
        }
    }

    private static IReadOnlyList<MeasurementAggregates> GetMeasurementAggregates(IEnumerable<MeasurementEntry> measurements, MeasurementAggregatesQuery query)
    {
        var aggregatesAccessor = new MeasurementAggregatesAccessor(new StubMeasurementsReader { Data = measurements.ToArray() });

        return aggregatesAccessor
            .GetMeasurementAggregates(query, CancellationToken.None)
            .ToArray();
    }

    private class StubMeasurementsReader : IMeasurementsReader
    {
        public ICollection<MeasurementEntry>? Data { get; set; }

        public IEnumerable<MeasurementEntry> GetCollectedData(DateTimeOffset start, DateTimeOffset end, CancellationToken token)
        {
            return Data?.Select(x => x) ?? [];
        }
    }

    private class MeasurementAggregatesEqualityComparer : IEqualityComparer<MeasurementAggregates>
    {
        private readonly AggregatesEqualityComparer _aggregatesComparer = new();

        public bool Equals(MeasurementAggregates? x, MeasurementAggregates? y)
        {
            return _aggregatesComparer.Equals(x?.Temperature, y?.Temperature)
                && _aggregatesComparer.Equals(x?.Pressure, y?.Pressure)
                && _aggregatesComparer.Equals(x?.Humidity, y?.Humidity);
        }

        public int GetHashCode(MeasurementAggregates obj)
        {
            return HashCode.Combine(
                _aggregatesComparer.GetHashCode(obj.Temperature),
                _aggregatesComparer.GetHashCode(obj.Pressure),
                _aggregatesComparer.GetHashCode(obj.Humidity));
        }
    }

    private class AggregatesEqualityComparer : IEqualityComparer<Rcm.Services.Aggregates.Aggregates>
    {
        private readonly AggregateEntryEqualityComparer _entryComparer = new();

        public bool Equals(Rcm.Services.Aggregates.Aggregates? x, Rcm.Services.Aggregates.Aggregates? y)
        {
            return _entryComparer.Equals(x?.First, y?.First)
                && _entryComparer.Equals(x?.Min, y?.Min)
                && _entryComparer.Equals(x?.Max, y?.Max)
                && _entryComparer.Equals(x?.Last, y?.Last);
        }

        public int GetHashCode(Rcm.Services.Aggregates.Aggregates obj)
        {
            return HashCode.Combine(
                _entryComparer.GetHashCode(obj.First),
                _entryComparer.GetHashCode(obj.Min),
                _entryComparer.GetHashCode(obj.Max),
                _entryComparer.GetHashCode(obj.Last));
        }
    }

    private class AggregateEntryEqualityComparer : IEqualityComparer<AggregateEntry>
    {
        public bool Equals(AggregateEntry? x, AggregateEntry? y)
        {
            return x?.Time == y?.Time && x?.Value == y?.Value;
        }

        public int GetHashCode(AggregateEntry obj)
        {
            return HashCode.Combine(obj.Time, obj.Value);
        }
    }
}
