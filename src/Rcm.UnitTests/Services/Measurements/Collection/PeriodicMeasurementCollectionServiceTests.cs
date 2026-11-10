using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
        // Given
        var initialMeasurementDelay = TimeSpan.FromMilliseconds(50);
        var measurementPeriod = TimeSpan.FromMilliseconds(100);

        using var blockingMeasurementCollector = new BlockingMeasurementCollector();

        var periodicDataCollectionService = CreatePeriodicDataCollectionService(
            blockingMeasurementCollector,
            measurementTimings: new() { InitialDelay = initialMeasurementDelay, Period = measurementPeriod });

        // When
        var stopwatch = Stopwatch.StartNew();
        await periodicDataCollectionService.StartAsync(CancellationToken.None);

        await blockingMeasurementCollector.WaitForPendingMeasurementAsync();
        var measuredFirstMeasurementDelay = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        blockingMeasurementCollector.Release();

        await blockingMeasurementCollector.WaitForPendingMeasurementAsync();
        var subsequentMeasurementDelay = stopwatch.ElapsedMilliseconds;

        // Then
        Assert.AreEqual(initialMeasurementDelay.TotalMilliseconds, measuredFirstMeasurementDelay, Tolerance.TotalMilliseconds);
        Assert.AreEqual(measurementPeriod.TotalMilliseconds, subsequentMeasurementDelay, Tolerance.TotalMilliseconds);
    }

    [Test]
    public async Task DoesNotInvokeNextMeasurementIfPreviousMeasurementIsStillPending()
    {
        // Given
        var measurementPeriod = TimeSpan.FromMilliseconds(32);

        using var blockingMeasurementCollector = new BlockingMeasurementCollector();

        var periodicDataCollectionService = CreatePeriodicDataCollectionService(
            blockingMeasurementCollector,
            measurementTimings: new() { InitialDelay = TimeSpan.Zero, Period = measurementPeriod });

        // When
        await periodicDataCollectionService.StartAsync(CancellationToken.None);

        await blockingMeasurementCollector.WaitForPendingMeasurementAsync();

        var subsequentMeasurementTask = blockingMeasurementCollector.WaitForPendingMeasurementAsync();
        await subsequentMeasurementTask.TryWait(4 * measurementPeriod);

        // Then
        Assert.AreEqual(TaskStatus.WaitingForActivation, subsequentMeasurementTask.Status);
    }

    [Test]
    public async Task StoppingWithoutPendingMeasurementStopsImmediately()
    {
        // Given
        var measurementTimingsWithLargeDelay = new MeasurementCollectionTimings
        {
            InitialDelay = TimeSpan.FromDays(10),
            Period = TimeSpan.FromDays(10)
        };

        using var blockingMeasurementCollector = new BlockingMeasurementCollector();

        var periodicDataCollectionService = CreatePeriodicDataCollectionService(
            blockingMeasurementCollector,
            measurementTimingsWithLargeDelay);

        await periodicDataCollectionService.StartAsync(CancellationToken.None);

        // When
        var stoppingCompleted = await periodicDataCollectionService
            .StopAsync(CancellationToken.None)
            .TryWait(TimeSpan.FromSeconds(1));

        // Then
        Assert.IsTrue(stoppingCompleted, nameof(stoppingCompleted));
    }

    [Test]
    [Theory]
    public async Task StoppingCancelsPendingSynchronousMeasurement(bool blockedAsynchronously)
    {
        // Given
        using var blockingMeasurementCollector = new BlockingMeasurementCollector
        {
            BlocksAsynchronously = blockedAsynchronously,
            IsCancellable = true
        };

        var periodicDataCollectionService = CreatePeriodicDataCollectionService(blockingMeasurementCollector);

        await periodicDataCollectionService.StartAsync(CancellationToken.None);

        // When
        await blockingMeasurementCollector.WaitForPendingMeasurementAsync();

        var stoppingCompleted = await periodicDataCollectionService
            .StopAsync(CancellationToken.None)
            .TryWait(TimeSpan.FromSeconds(1));

        // Then
        Assert.IsTrue(stoppingCompleted, nameof(stoppingCompleted));
    }

    [Test]
    [Theory]
    public async Task StoppingCanBeCancelledImmediatelyIfMeasurementIsBlocked(bool blockedAsynchronously)
    {
        // Given
        using var cancellationTokenSource = new CancellationTokenSource();

        using var blockingMeasurementCollector = new BlockingMeasurementCollector
        {
            BlocksAsynchronously = blockedAsynchronously,
            IsCancellable = false
        };

        var periodicDataCollectionService = CreatePeriodicDataCollectionService(blockingMeasurementCollector);

        await periodicDataCollectionService.StartAsync(CancellationToken.None);

        // When
        await blockingMeasurementCollector.WaitForPendingMeasurementAsync();

        var stoppingTask = periodicDataCollectionService.StopAsync(cancellationTokenSource.Token);
        var stoppedImmediately = stoppingTask.IsCompleted;

        await cancellationTokenSource.CancelAsync();

        var stoppedAfterCancellation = await stoppingTask.TryWait(TimeSpan.FromSeconds(1));

        // Then
        Assert.IsFalse(stoppedImmediately, nameof(stoppedImmediately));
        Assert.IsTrue(stoppedAfterCancellation, nameof(stoppedAfterCancellation));
        Assert.AreEqual(TaskStatus.RanToCompletion, stoppingTask.Status);
    }

    private static PeriodicMeasurementCollectionService CreatePeriodicDataCollectionService(
        IMeasurementCollector measurementCollector,
        MeasurementCollectionTimings? measurementTimings = null)
    {
        return new(
            new StubMeasurementTimingsCalculator { Timings = measurementTimings ?? new() { InitialDelay = TimeSpan.Zero, Period = TimeSpan.FromDays(10) } },
            measurementCollector);
    }

    private class StubMeasurementTimingsCalculator : IMeasurementTimingsCalculator
    {
        public required MeasurementCollectionTimings Timings { get; init; }

        public MeasurementCollectionTimings DetermineMeasurementTimings()
        {
            return Timings;
        }
    }

    private class BlockingMeasurementCollector : IMeasurementCollector, IDisposable
    {
        private readonly SemaphoreSlim _startedSemaphore = new(initialCount: 0);
        private readonly SemaphoreSlim _blockingSemaphore = new(initialCount: 0);

        public bool IsCancellable { get; set; }
        public bool BlocksAsynchronously { get; set; }

        public async Task MeasureAsync(CancellationToken token)
        {
            _startedSemaphore.Release();

            if (BlocksAsynchronously)
            {
                await Task.Yield();
            }

            await _blockingSemaphore.WaitAsync(IsCancellable ? token : CancellationToken.None);
        }

        public Task WaitForPendingMeasurementAsync()
        {
            return _startedSemaphore.WaitAsync();
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
