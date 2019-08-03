using NUnit.Framework;
using Rcm.Aggregates.Api;
using Rcm.Common;
using Rcm.DataCollection.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rcm.Aggregates.UnitTests
{
    [TestFixture]
    public class MeasurementAggregatesAccessorTests
    {
        [Test]
        public void CalculatesAggregatesFromMeasurementsWithinCorresponsingTimeRange()
        {
            // given
            var offset = TimeSpan.FromHours(1);
            var startTime = new DateTimeOffset(2019, 2, 8, 12, 0, 0, offset);
            var endTime = startTime.AddHours(4);
            var partitionCount = 2;

            var firstPartitionEndTime = new DateTimeOffset((startTime.Ticks + endTime.Ticks) / partitionCount, offset);

            var firstMeasurementInFirstPartition = new MeasurementEntry(startTime, 20m, 30m, 900m);
            var maxTemperatureMeasurementInFirstPartition = new MeasurementEntry(startTime.AddMinutes(17), 35m, 30m, 900m);
            var minTemperatureMeasurementInFirstPartition = new MeasurementEntry(startTime.AddMinutes(26), 0m, 30m, 900m);
            var maxPressureMeasurementInFirstPartition = new MeasurementEntry(startTime.AddMinutes(35), 35m, 30m, 1100m);
            var minPressureMeasurementInFirstPartition = new MeasurementEntry(startTime.AddMinutes(42), 35m, 30m, 750m);
            var maxHumidityMeasurementInFirstPartition = new MeasurementEntry(startTime.AddMinutes(91), 20m, 55m, 900m);
            var minHumidityMeasurementInFirstPartition = new MeasurementEntry(startTime.AddMinutes(110), 20m, 15m, 900m);
            var lastMeasurementInFirstPartition = new MeasurementEntry(firstPartitionEndTime, 20m, 15m, 900m);

            var firstMeasurementInSecondPartition = new MeasurementEntry(firstPartitionEndTime.AddMinutes(1), 20m, 30m, 900m);
            var maxTemperatureMeasurementInSecondPartition = new MeasurementEntry(firstPartitionEndTime.AddMinutes(17), 35m, 30m, 900m);
            var minTemperatureMeasurementInSecondPartition = new MeasurementEntry(firstPartitionEndTime.AddMinutes(26), 0m, 30m, 900m);
            var maxPressureMeasurementInSecondPartition = new MeasurementEntry(firstPartitionEndTime.AddMinutes(35), 35m, 30m, 1100m);
            var minPressureMeasurementInSecondPartition = new MeasurementEntry(firstPartitionEndTime.AddMinutes(42), 35m, 30m, 750m);
            var maxHumidityMeasurementInSecondPartition = new MeasurementEntry(firstPartitionEndTime.AddMinutes(91), 20m, 55m, 900m);
            var minHumidityMeasurementInSecondPartition = new MeasurementEntry(firstPartitionEndTime.AddMinutes(110), 20m, 15m, 900m);
            var lastMeasurementInSecondPartition = new MeasurementEntry(endTime, 20m, 15m, 900m);

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

            var aggregatesAccessor = new MeasurementAggregatesAccessor(new StubCollectedDataAccessor { Data = measurements });

            // when
            var aggregates = aggregatesAccessor.GetMeasurementAggregates(startTime, endTime, partitionCount).ToList();

            // then
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
            // given
            var dummyStartTime = new DateTimeOffset(2019, 2, 7, 21, 48, 15, TimeSpan.FromHours(1));
            var dummyEndTime = dummyStartTime.AddDays(1);

            var count = 1;
            var measurements = new[]
            {
                new MeasurementEntry(dummyStartTime.AddHours(1), 25m, 30m, 950),
                new MeasurementEntry(dummyStartTime.AddHours(6), 18m, 37m, 945),
                new MeasurementEntry(dummyStartTime.AddHours(12), 21m, 32m, 1010),
                new MeasurementEntry(dummyStartTime.AddHours(16), 22m, 28m, 985),
                new MeasurementEntry(dummyStartTime.AddHours(19), 19m, 31m, 995)
            };

            var aggregatesAccessor = new MeasurementAggregatesAccessor(new StubCollectedDataAccessor { Data = measurements });

            // when
            var aggregates = aggregatesAccessor.GetMeasurementAggregates(dummyStartTime, dummyEndTime, count);

            // then
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
            // given
            var offset = TimeSpan.FromHours(1);
            var startTime = new DateTimeOffset(2019, 2, 9, 10, 0, 0, offset);
            var endTime = startTime.AddHours(1);
            var partitionCount = 2;
            var secondPartitionStart = new DateTimeOffset((startTime.Ticks + endTime.Ticks) / partitionCount, offset);

            var measurementInFirstPartition = new MeasurementEntry(startTime, 10m, 20m, 900m);
            var measurementOnBorderOfPartitions = new MeasurementEntry(secondPartitionStart, 20m, 30m, 950m);
            var measurementInSecondPartition = new MeasurementEntry(endTime, 30m, 40m, 1000m);

            var aggregatesAccessor = new MeasurementAggregatesAccessor(
                new StubCollectedDataAccessor 
                {
                    Data = new[] { measurementInFirstPartition, measurementOnBorderOfPartitions, measurementInSecondPartition }
                });

            // when
            var aggregates = aggregatesAccessor.GetMeasurementAggregates(startTime, endTime, partitionCount);

            // then
            var minTemperature = new AggregateEntry(measurementInFirstPartition.Time, measurementInFirstPartition.CelsiusTemperature);
            var maxTemperature = new AggregateEntry(measurementOnBorderOfPartitions.Time, measurementOnBorderOfPartitions.CelsiusTemperature);
            var minPressure = new AggregateEntry(measurementInFirstPartition.Time, measurementInFirstPartition.HpaPressure);
            var maxPressure = new AggregateEntry(measurementOnBorderOfPartitions.Time, measurementOnBorderOfPartitions.HpaPressure);
            var minHumidity = new AggregateEntry(measurementInFirstPartition.Time, measurementInFirstPartition.RelativeHumidity);
            var maxHumidity = new AggregateEntry(measurementOnBorderOfPartitions.Time, measurementOnBorderOfPartitions.RelativeHumidity);
            var firstPartitionAggregates = new MeasurementAggregates(
                new Api.Aggregates(minTemperature, minTemperature, maxTemperature, maxTemperature),
                new Api.Aggregates(minPressure, minPressure, maxPressure, maxPressure),
                new Api.Aggregates(minHumidity, minHumidity, maxHumidity, maxHumidity));

            var secondPartitionTemperature = new AggregateEntry(measurementInSecondPartition.Time, measurementInSecondPartition.CelsiusTemperature);
            var secondPartitionPressure = new AggregateEntry(measurementInSecondPartition.Time, measurementInSecondPartition.HpaPressure);
            var secondPartitionHumidity = new AggregateEntry(measurementInSecondPartition.Time, measurementInSecondPartition.RelativeHumidity);
            var secondPartitionAggregates = new MeasurementAggregates(
                new Api.Aggregates(secondPartitionTemperature, secondPartitionTemperature, secondPartitionTemperature, secondPartitionTemperature),
                new Api.Aggregates(secondPartitionPressure, secondPartitionPressure, secondPartitionPressure, secondPartitionPressure),
                new Api.Aggregates(secondPartitionHumidity, secondPartitionHumidity, secondPartitionHumidity, secondPartitionHumidity));

            Assert.That(
                aggregates,
                Is.EquivalentTo(
                    new[]
                    {
                        firstPartitionAggregates,
                        secondPartitionAggregates
                    })
                .Using(new MeasurementAggregatesEqualityComparer()));
        }

        [Test]
        public void NoAggregatesAreReturnedForAPartitionIfThereAreNoMeasurementsInThePartition()
        {
            // given
            var offset = TimeSpan.FromHours(1);
            var startTime = new DateTimeOffset(2019, 2, 9, 10, 0, 0, offset);
            var endTime = startTime.AddHours(1);
            var partitionCount = 2;
            var secondPartitionStart = new DateTimeOffset((startTime.Ticks + endTime.Ticks) / partitionCount, offset);

            var measurementsInFirstPartition = new MeasurementEntry[0];
            var actualMeasurementTime = secondPartitionStart.AddMinutes(5);
            var measurementsInSecondPartition = new[] { new MeasurementEntry(actualMeasurementTime, 25m, 37m, 975m) };

            var aggregatesAccessor = new MeasurementAggregatesAccessor(
                new StubCollectedDataAccessor
                {
                    Data = measurementsInFirstPartition.Concat(measurementsInSecondPartition).ToList()
                });

            // when
            var aggregates = aggregatesAccessor.GetMeasurementAggregates(startTime, endTime, partitionCount);

            // then
            var temperature = new AggregateEntry(actualMeasurementTime, 25m);
            var humidity = new AggregateEntry(actualMeasurementTime, 37m);
            var pressure = new AggregateEntry(actualMeasurementTime, 975m);

            Assert.That(
                aggregates,
                Is.EquivalentTo(
                    new[]
                    {
                        new MeasurementAggregates(
                            new Api.Aggregates(temperature, temperature, temperature, temperature),
                            new Api.Aggregates(pressure, pressure, pressure, pressure),
                            new Api.Aggregates(humidity, humidity, humidity, humidity))
                    })
                .Using(new MeasurementAggregatesEqualityComparer()));
        }

        [Test]
        public void NoAggregatesAreReturnedIfThereAreNoMeasurements()
        {
            // given
            var dummyStartTime = new DateTimeOffset(2019, 2, 7, 21, 48, 15, TimeSpan.FromHours(1));
            var dummyEndTime = dummyStartTime.AddHours(12);

            var dummyCount = 3;
            var emptyMeasurements = new MeasurementEntry[0];

            var aggregatesAccessor = new MeasurementAggregatesAccessor(new StubCollectedDataAccessor { Data = emptyMeasurements });

            // when
            var aggregates = aggregatesAccessor.GetMeasurementAggregates(dummyStartTime, dummyEndTime, dummyCount);

            // then
            CollectionAssert.IsEmpty(aggregates);
        }

        [Test]
        public void ThrowsOnEvaluationForNonMonotonousMeasurementTimes()
        {
            // given
            var dummyStartTime = new DateTimeOffset(2019, 2, 7, 12, 0, 0, TimeSpan.FromHours(1));
            var dummyEndTime = dummyStartTime.AddDays(1);
            var dummyCount = 3;

            var nonMonotonousMeasurements = new[]
            {
                new MeasurementEntry(dummyStartTime.AddMinutes(10), 10m, 20m, 900m),
                new MeasurementEntry(dummyStartTime, 10m, 20m, 900m),
            };

            var aggregatesAccessor = new MeasurementAggregatesAccessor(new StubCollectedDataAccessor { Data = nonMonotonousMeasurements });

            // when
            void GetAggregatesForNonMonotonousMeasurementTimes() =>
                aggregatesAccessor
                    .GetMeasurementAggregates(dummyStartTime, dummyEndTime, dummyCount)
                    .ToList();

            // then
            Assert.Catch(GetAggregatesForNonMonotonousMeasurementTimes);
        }

        private class StubCollectedDataAccessor : ICollectedDataAccessor
        {
            public ICollection<MeasurementEntry>? Data { get; set; }

            public IEnumerable<MeasurementEntry> GetCollectedData(DateTimeOffset start, DateTimeOffset end) =>
                Data?.Select(x => x) ?? Enumerable.Empty<MeasurementEntry>();
        }

        private class MeasurementAggregatesEqualityComparer : IEqualityComparer<MeasurementAggregates>
        {
            private readonly IEqualityComparer<Api.Aggregates> _aggregatesComparer = new AggregatesEqualityComparer();

            public bool Equals(MeasurementAggregates x, MeasurementAggregates y) =>
                _aggregatesComparer.Equals(x.Temperature, y.Temperature)
                    && _aggregatesComparer.Equals(x.Pressure, y.Pressure)
                    && _aggregatesComparer.Equals(x.Humidity, y.Humidity);

            public int GetHashCode(MeasurementAggregates obj) =>
                HashCode.Combine(
                    _aggregatesComparer.GetHashCode(obj.Temperature),
                    _aggregatesComparer.GetHashCode(obj.Pressure),
                    _aggregatesComparer.GetHashCode(obj.Humidity));
        }

        private class AggregatesEqualityComparer : IEqualityComparer<Api.Aggregates>
        {
            private readonly IEqualityComparer<AggregateEntry> _entryComparer = new AggregateEntryEqualityComparer();

            public bool Equals(Api.Aggregates x, Api.Aggregates y) => 
                _entryComparer.Equals(x.First, y.First)
                    && _entryComparer.Equals(x.Min, y.Min)
                    && _entryComparer.Equals(x.Max, y.Max)
                    && _entryComparer.Equals(x.Last, y.Last);

            public int GetHashCode(Api.Aggregates obj) =>
                HashCode.Combine(
                    _entryComparer.GetHashCode(obj.First),
                    _entryComparer.GetHashCode(obj.Min),
                    _entryComparer.GetHashCode(obj.Max),
                    _entryComparer.GetHashCode(obj.Last));
        }

        private class AggregateEntryEqualityComparer : IEqualityComparer<AggregateEntry>
        {
            public bool Equals(AggregateEntry x, AggregateEntry y) =>
                x.Time == y.Time && x.Value == y.Value;

            public int GetHashCode(AggregateEntry obj) =>
                HashCode.Combine(obj.Time, obj.Value);
        }
    }
}