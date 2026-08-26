using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Rcm.Common;
using Rcm.Common.Temporal;
using Rcm.DataCollection;
using Rcm.DataCollection.Api;
using Rcm.Sensors.Abstractions;
using Rcm.Services.Measurements.Collection;
using Rcm.Testing.Common.Temporal;

namespace Rcm.UnitTests.Persistence;

[TestFixture]
public class MeasurementCollectorTests
{
    public class MeasurementExecution
    {
        [Test]
        public void SubsequentMeasurementIsSkippedIfPreviousMeasurementIsStillInProgress()
        {
            // given
            var blockingSpyMeasurementProvider = new BlockingSpySensor();

            var measurementCollector = new MeasurementCollector(
                NullLogger<MeasurementCollector>.Instance,
                new Clock(),
                blockingSpyMeasurementProvider,
                new DummyMeasurementsWriter());

            // when
            var firstMeasurementTask = measurementCollector.MeasureAsync(default);
            var secondMeasurementTask = measurementCollector.MeasureAsync(default);

            // then
            Assert.AreEqual(1, blockingSpyMeasurementProvider.InvocationCount);
            Assert.IsTrue(secondMeasurementTask.IsCompleted);

            // clean-up
            blockingSpyMeasurementProvider.Release();
            Task.WaitAll(firstMeasurementTask, secondMeasurementTask);
        }

        [Test]
        public async Task SubsequentMeasurementIsCarriedOutEvenIfPreviousMeasurementHasThrown()
        {
            // given
            var throwingSpyMeasurementProvider = new ThrowingSpySensor();

            var measurementCollector = new MeasurementCollector(
                NullLogger<MeasurementCollector>.Instance,
                new Clock(),
                throwingSpyMeasurementProvider,
                new DummyMeasurementsWriter());

            // when
            await IgnoreExceptions(() => measurementCollector.MeasureAsync(default));
            await IgnoreExceptions(() => measurementCollector.MeasureAsync(default));

            // then
            Assert.AreEqual(2, throwingSpyMeasurementProvider.InvocationCount);
        }

        [Test]
        public async Task AverageOfPreviousMeasurementsIsStoredIfNewMeasurementDiffersInTimeMinutes()
        {
            // given
            var firstMeasurementTime = new DateTimeOffset(2018, 12, 28, 19, 50, 10, TimeSpan.Zero);
            var secondMeasurementTimeWithinSameMinute = firstMeasurementTime.AddSeconds(30);
            var measurementTimeInNextMinute = firstMeasurementTime.AddMinutes(1);

            var firstMeasurement = new SensorMeasurement { Time = firstMeasurementTime, CelsiusTemperature = 30m, RelativeHumidity = 45m, HpaPressure = 950m };
            var secondMeasurementWithinSameMinute = new SensorMeasurement { Time = secondMeasurementTimeWithinSameMinute, CelsiusTemperature = 20m, RelativeHumidity = 40m, HpaPressure = 1050m };
            var measurementInNextMinute = new SensorMeasurement { Time = measurementTimeInNextMinute, CelsiusTemperature = 35m, RelativeHumidity = 35m, HpaPressure = 970m };

            var spyCollectedDataStorage = new SpyMeasurementsWriter();

            var measurementCollector = new MeasurementCollector(
                NullLogger<MeasurementCollector>.Instance,
                new Clock(),
                new FakeSensor(new[] { firstMeasurement, secondMeasurementWithinSameMinute, measurementInNextMinute }),
                spyCollectedDataStorage);

            // when
            await measurementCollector.MeasureAsync(default);
            await measurementCollector.MeasureAsync(default);
            await measurementCollector.MeasureAsync(default);

            // then
            Assert.IsNotNull(spyCollectedDataStorage.StoredEntry);

            Assert.AreEqual(firstMeasurementTime.Offset, spyCollectedDataStorage.StoredEntry!.Time.Offset);
            Assert.AreEqual(firstMeasurementTime.Year, spyCollectedDataStorage.StoredEntry.Time.Year);
            Assert.AreEqual(firstMeasurementTime.Month, spyCollectedDataStorage.StoredEntry.Time.Month);
            Assert.AreEqual(firstMeasurementTime.Day, spyCollectedDataStorage.StoredEntry.Time.Day);
            Assert.AreEqual(firstMeasurementTime.Hour, spyCollectedDataStorage.StoredEntry.Time.Hour);
            Assert.AreEqual(firstMeasurementTime.Minute, spyCollectedDataStorage.StoredEntry.Time.Minute);
            Assert.AreEqual(0, spyCollectedDataStorage.StoredEntry.Time.Second);

            Assert.AreEqual(
                (firstMeasurement.CelsiusTemperature + secondMeasurementWithinSameMinute.CelsiusTemperature) / 2,
                spyCollectedDataStorage.StoredEntry.CelsiusTemperature);

            Assert.AreEqual(
                (firstMeasurement.HpaPressure + secondMeasurementWithinSameMinute.HpaPressure) / 2,
                spyCollectedDataStorage.StoredEntry.HpaPressure);

            Assert.AreEqual(
                (firstMeasurement.RelativeHumidity + secondMeasurementWithinSameMinute.RelativeHumidity) / 2,
                spyCollectedDataStorage.StoredEntry.RelativeHumidity);
        }

        private async Task IgnoreExceptions(Func<Task> f)
        {
            try
            {
                await f.Invoke();
            }
            catch
            {
                // ignored
            }
        }

        public class FakeSensor : ISensor
        {
            private readonly IReadOnlyList<SensorMeasurement> _measurements;

            private int _currentMeasurementIndex;

            public FakeSensor(IReadOnlyList<SensorMeasurement> measurements)
            {
                _measurements = measurements;
            }

            public Task<SensorMeasurement> ReadMeasurementAsync(CancellationToken token)
            {
                var measurement = _measurements[_currentMeasurementIndex];

                _currentMeasurementIndex += 1;
                if (_currentMeasurementIndex >= _measurements.Count)
                {
                    _currentMeasurementIndex = 0;
                }

                return Task.FromResult(measurement);
            }
        }

        public class SpyMeasurementsWriter : IMeasurementsWriter
        {
            public MeasurementEntry? StoredEntry { get; private set; }

            public Task StoreAsync(MeasurementEntry value, CancellationToken token)
            {
                StoredEntry = value;
                return Task.CompletedTask;
            }
        }

        public class ThrowingSpySensor : ISensor
        {
            private int _invocationCount;
            public int InvocationCount => _invocationCount;

            public Task<SensorMeasurement> ReadMeasurementAsync(CancellationToken token)
            {
                _ = Interlocked.Increment(ref _invocationCount);
                throw new Exception();
            }
        }

        public class BlockingSpySensor : ISensor
        {
            private readonly Task<SensorMeasurement> _task = new(() => new() { Time = DateTimeOffset.Now, CelsiusTemperature = 0m, RelativeHumidity = 0m, HpaPressure = 0m });

            private int _invocationCount;
            public int InvocationCount => _invocationCount;

            public Task<SensorMeasurement> ReadMeasurementAsync(CancellationToken token)
            {
                _ = Interlocked.Increment(ref _invocationCount);
                return _task;
            }

            public void Release()
            {
                _task.RunSynchronously();
            }
        }
    }

    public class MeasurementTimings
    {
        [Test]
        public void MeasurementStartsOnNextMinuteWithSixSecondsPeriodForNonZeroSecondsTime()
        {
            // given
            var nonZeroSecondsTime = new DateTimeOffset(2018, 12, 27, 13, 28, 10, TimeSpan.Zero);

            var measurementCollector = new MeasurementCollector(
                NullLogger<MeasurementCollector>.Instance,
                new FixedClock { Now = nonZeroSecondsTime },
                new DummySensor(),
                new DummyMeasurementsWriter());

            // when
            var (nextMeasurementDelay, measurementPeriod) = measurementCollector.MeasurementTimings;

            // then
            Assert.AreEqual(TimeSpan.FromSeconds(60 - nonZeroSecondsTime.Second), nextMeasurementDelay);
            Assert.AreEqual(TimeSpan.FromSeconds(6), measurementPeriod);
        }

        [Test]
        public void MeasurementStartsNowWithSixSecondsPeriodForZeroSecondsTime()
        {
            // given
            var zeroSecondsTime = new DateTimeOffset(2018, 12, 27, 13, 28, 0, TimeSpan.Zero);

            var measurementCollector = new MeasurementCollector(
                NullLogger<MeasurementCollector>.Instance,
                new FixedClock { Now = zeroSecondsTime },
                new DummySensor(),
                new DummyMeasurementsWriter());

            // when
            var (nextMeasurementDelay, measurementPeriod) = measurementCollector.MeasurementTimings;

            // then
            Assert.AreEqual(TimeSpan.Zero, nextMeasurementDelay);
            Assert.AreEqual(TimeSpan.FromSeconds(6), measurementPeriod);
        }
    }

    public class DummySensor : ISensor
    {
        public Task<SensorMeasurement> ReadMeasurementAsync(CancellationToken token) => throw new NotImplementedException();
    }

    public class DummyMeasurementsWriter : IMeasurementsWriter
    {
        public Task StoreAsync(MeasurementEntry value, CancellationToken token) => Task.CompletedTask;
    }
}
