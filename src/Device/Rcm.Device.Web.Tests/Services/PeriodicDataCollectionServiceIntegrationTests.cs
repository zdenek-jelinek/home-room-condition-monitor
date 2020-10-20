using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rcm.Common.Tasks;
using Rcm.Common.TestDoubles;
using Rcm.Device.DataCollection.Api;
using Rcm.Device.Web.Services;

namespace Rcm.Device.Web.Tests.Services
{
    [TestFixture]
    public class PeriodicDataCollectionServiceIntegrationTests
    {
        private static TimeSpan Tolerance => TimeSpan.FromMilliseconds(32);

        [Test]
        public async Task InvokesMeasurementWithSpecifiedTimeoutAndPeriodAfterStarting()
        {
            // given
            var firstMeasurementDelay = TimeSpan.FromMilliseconds(50);
            var measurementPeriod = TimeSpan.FromMilliseconds(100);

            using var blockingMeasurementCollector = new BlockingMeasurementCollector
            {
                MeasurementTimings = (firstMeasurementDelay, measurementPeriod)
            };

            await using var periodicDataCollecitonService = CreatePeriodicDataCollectionService(blockingMeasurementCollector);

            // when
            var stopwatch = Stopwatch.StartNew();
            await periodicDataCollecitonService.StartAsync(CancellationToken.None);

            await blockingMeasurementCollector.MeasurementStarted;
            var measuredFirstMeasurementDelay = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            blockingMeasurementCollector.Release();

            await blockingMeasurementCollector.MeasurementStarted;
            var subsequentMeasurementDelay = stopwatch.ElapsedMilliseconds;

            // then
            Assert.AreEqual(firstMeasurementDelay.TotalMilliseconds, measuredFirstMeasurementDelay, Tolerance.TotalMilliseconds);
            Assert.AreEqual(measurementPeriod.TotalMilliseconds, subsequentMeasurementDelay, Tolerance.TotalMilliseconds);
        }

        [Test]
        public async Task DoesNotInvokeNextMeasurementIfPreviousMeasurementIsStillPending()
        {
            // given
            var measurementPeriod = TimeSpan.FromMilliseconds(32);

            using var blockingMeasurementCollector = new BlockingMeasurementCollector
            {
                MeasurementTimings = (TimeSpan.Zero, measurementPeriod)
            };

            await using var periodicDataCollecitonService = CreatePeriodicDataCollectionService(blockingMeasurementCollector);

            // when
            await periodicDataCollecitonService.StartAsync(CancellationToken.None);

            await blockingMeasurementCollector.MeasurementStarted;
            
            var subsequentMeasurementIssued = await blockingMeasurementCollector.MeasurementStarted.TryWait(4 * measurementPeriod);

            // then
            Assert.IsFalse(subsequentMeasurementIssued, nameof(subsequentMeasurementIssued));
        }

        [Test]
        public async Task StoppingWithoutPendingMeasurementStopsImmediatelly()
        {
            // given
            using var measurementCollectorWithLargeDelay = new BlockingMeasurementCollector
            {
                MeasurementTimings = (TimeSpan.FromHours(4), TimeSpan.FromHours(1))
            };

            await using var periodicDataCollecitonService = CreatePeriodicDataCollectionService(measurementCollectorWithLargeDelay);

            await periodicDataCollecitonService.StartAsync(CancellationToken.None);

            // when
            var stoppingCompleted = await periodicDataCollecitonService
                .StopAsync(CancellationToken.None)
                .TryWait(TimeSpan.FromSeconds(1));

            // then
            Assert.IsTrue(stoppingCompleted, nameof(stoppingCompleted));
        }

        [Test]
        [Theory]
        public async Task StoppingCancelsPendingSynchronousMeasurement(bool blockedAsynchronously)
        {
            // given
            using var blockingmeasurementCollector = new BlockingMeasurementCollector
            {
                BlocksAsynchronously = blockedAsynchronously,
                IsCancellable = true,
                MeasurementTimings = (TimeSpan.Zero, TimeSpan.FromHours(1))
            };

            await using var periodicDataCollecitonService = CreatePeriodicDataCollectionService(blockingmeasurementCollector);

            await periodicDataCollecitonService.StartAsync(CancellationToken.None);

            // when
            await blockingmeasurementCollector.MeasurementStarted;

            var stoppingCompleted = await periodicDataCollecitonService
                .StopAsync(CancellationToken.None)
                .TryWait(TimeSpan.FromSeconds(1));

            // then
            Assert.IsTrue(stoppingCompleted, nameof(stoppingCompleted));
        }

        [Test]
        [Theory]
        public async Task StoppingCanBeCancelledImmediatellyIfMeasurementIsBlocked(bool blockedAsynchronously)
        {
            // given
            using var cancellationTokenSource = new CancellationTokenSource();

            using var blockingmeasurementCollector = new BlockingMeasurementCollector
            {
                BlocksAsynchronously = blockedAsynchronously,
                IsCancellable = false,
                MeasurementTimings = (TimeSpan.Zero, TimeSpan.FromHours(1))
            };

            await using var periodicDataCollecitonService = CreatePeriodicDataCollectionService(blockingmeasurementCollector);

            await periodicDataCollecitonService.StartAsync(CancellationToken.None);

            // when
            await blockingmeasurementCollector.MeasurementStarted;

            var stoppingTask = periodicDataCollecitonService.StopAsync(cancellationTokenSource.Token);
            var stoppedImmediatelly = stoppingTask.IsCompleted;

            cancellationTokenSource.Cancel();

            var stoppedAfterCancellation = await stoppingTask.TryWait(TimeSpan.FromSeconds(1));

            // then
            Assert.IsFalse(stoppedImmediatelly, nameof(stoppedImmediatelly));
            Assert.IsTrue(stoppedAfterCancellation, nameof(stoppedAfterCancellation));
            Assert.AreEqual(TaskStatus.RanToCompletion, stoppingTask.Status);
        }

        private static PeriodicDataCollectionService CreatePeriodicDataCollectionService(
            IMeasurementCollector measurementCollector)
        {
            return new PeriodicDataCollectionService(
                new DummyLogger<PeriodicDataCollectionService>(),
                measurementCollector);
        }

        private class BlockingMeasurementCollector : IMeasurementCollector, IDisposable
        {
            private readonly SemaphoreSlim _startedSemaphore = new SemaphoreSlim(0);
            private readonly SemaphoreSlim _blockingSemaphore = new SemaphoreSlim(0);

            public bool IsCancellable { get; set; }
            public bool BlocksAsynchronously { get; set; }

            public (TimeSpan nextMeasurementDelay, TimeSpan measurementPeriod) MeasurementTimings { get; set; }

            public Task MeasurementStarted => _startedSemaphore.WaitAsync();

            public async Task MeasureAsync(CancellationToken token)
            {
                _startedSemaphore.Release();

                if (BlocksAsynchronously)
                {
                    await Task.Yield();
                }

                await _blockingSemaphore.WaitAsync(IsCancellable ? token : CancellationToken.None);
            }

            public void Release()
            {
                _blockingSemaphore.Release();
            }

            public void Dispose()
            {
                _startedSemaphore.Dispose();
                _blockingSemaphore.Dispose();
            }
        }
    }
}