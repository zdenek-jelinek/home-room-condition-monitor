using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Rcm.Services.Measurements.Collection;
using Rcm.Testing.Threading.Tasks;

namespace Rcm.UnitTests.Services.Measurements.Collection;

[TestFixture]
public class PeriodicMeasurementCollectionServiceTests
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
            MeasurementTimings = new() { InitialDelay = firstMeasurementDelay, Period = measurementPeriod }
        };

        await using var periodicDataCollectionService = CreatePeriodicDataCollectionService(blockingMeasurementCollector);

        // when
        var stopwatch = Stopwatch.StartNew();
        await periodicDataCollectionService.StartAsync(CancellationToken.None);

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
            MeasurementTimings = new() { InitialDelay = TimeSpan.Zero, Period = measurementPeriod }
        };

        await using var periodicDataCollectionService = CreatePeriodicDataCollectionService(blockingMeasurementCollector);

        // when
        await periodicDataCollectionService.StartAsync(CancellationToken.None);

        await blockingMeasurementCollector.MeasurementStarted;

        var subsequentMeasurementIssued = await blockingMeasurementCollector.MeasurementStarted.TryWait(4 * measurementPeriod);

        // then
        Assert.IsFalse(subsequentMeasurementIssued, nameof(subsequentMeasurementIssued));
    }

    [Test]
    public async Task StoppingWithoutPendingMeasurementStopsImmediately()
    {
        // given
        using var measurementCollectorWithLargeDelay = new BlockingMeasurementCollector
        {
            MeasurementTimings = new() { InitialDelay = TimeSpan.FromDays(10), Period = TimeSpan.FromDays(10) }
        };

        await using var periodicDataCollectionService = CreatePeriodicDataCollectionService(measurementCollectorWithLargeDelay);

        await periodicDataCollectionService.StartAsync(CancellationToken.None);

        // when
        var stoppingCompleted = await periodicDataCollectionService
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
        using var blockingMeasurementCollector = new BlockingMeasurementCollector
        {
            BlocksAsynchronously = blockedAsynchronously,
            IsCancellable = true
        };

        await using var periodicDataCollectionService = CreatePeriodicDataCollectionService(blockingMeasurementCollector);

        await periodicDataCollectionService.StartAsync(CancellationToken.None);

        // when
        await blockingMeasurementCollector.MeasurementStarted;

        var stoppingCompleted = await periodicDataCollectionService
            .StopAsync(CancellationToken.None)
            .TryWait(TimeSpan.FromSeconds(1));

        // then
        Assert.IsTrue(stoppingCompleted, nameof(stoppingCompleted));
    }

    [Test]
    [Theory]
    public async Task StoppingCanBeCancelledImmediatelyIfMeasurementIsBlocked(bool blockedAsynchronously)
    {
        // given
        using var cancellationTokenSource = new CancellationTokenSource();

        using var blockingMeasurementCollector = new BlockingMeasurementCollector
        {
            BlocksAsynchronously = blockedAsynchronously,
            IsCancellable = false
        };

        await using var periodicDataCollectionService = CreatePeriodicDataCollectionService(blockingMeasurementCollector);

        await periodicDataCollectionService.StartAsync(CancellationToken.None);

        // when
        await blockingMeasurementCollector.MeasurementStarted;

        var stoppingTask = periodicDataCollectionService.StopAsync(cancellationTokenSource.Token);
        var stoppedImmediately = stoppingTask.IsCompleted;

        cancellationTokenSource.Cancel();

        var stoppedAfterCancellation = await stoppingTask.TryWait(TimeSpan.FromSeconds(1));

        // then
        Assert.IsFalse(stoppedImmediately, nameof(stoppedImmediately));
        Assert.IsTrue(stoppedAfterCancellation, nameof(stoppedAfterCancellation));
        Assert.AreEqual(TaskStatus.RanToCompletion, stoppingTask.Status);
    }

    private static PeriodicMeasurementCollectionService CreatePeriodicDataCollectionService(
        IMeasurementCollector measurementCollector)
    {
        return new PeriodicMeasurementCollectionService(
            NullLogger<PeriodicMeasurementCollectionService>.Instance,
            measurementCollector);
    }

    private class BlockingMeasurementCollector : IMeasurementCollector, IDisposable
    {
        private readonly SemaphoreSlim _startedSemaphore = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _blockingSemaphore = new SemaphoreSlim(0);

        public bool IsCancellable { get; set; }
        public bool BlocksAsynchronously { get; set; }

        public Task MeasurementStarted => _startedSemaphore.WaitAsync();

        public MeasurementCollectionTimings MeasurementTimings { get; set; } = new() { InitialDelay = TimeSpan.Zero, Period = TimeSpan.FromDays(10) };

        public MeasurementCollectionTimings DetermineMeasurementTimings()
        {
            return MeasurementTimings;
        }

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
