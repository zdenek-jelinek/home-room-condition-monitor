using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rcm.Device.DataCollection.Api;

namespace Rcm.Device.Web.Services;

public class PeriodicDataCollectionService : IHostedService, IAsyncDisposable
{
    private readonly ILogger<PeriodicDataCollectionService> _logger;
    private readonly IMeasurementCollector _measurementCollector;

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly CancellationTokenSource _stoppingSource = new CancellationTokenSource();

    private Task? _pendingMeasurement;
    private Timer? _timer;

    public PeriodicDataCollectionService(
        ILogger<PeriodicDataCollectionService> logger,
        IMeasurementCollector measurementCollector)
    {
        _logger = logger;
        _measurementCollector = measurementCollector;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var (nextMeasurementDelay, measurementPeriod) = _measurementCollector.MeasurementTimings;
        _timer = new Timer(_ => RunMeasurement(), null, nextMeasurementDelay, measurementPeriod);
            
        _logger.LogInformation("Periodic measurement started");

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling periodic measurements");

        _stoppingSource.Cancel();
        _ = _timer?.Change(Timeout.Infinite, Timeout.Infinite);

        try
        {
            await _semaphore.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Graceful periodic measurement cancellation failed: "
                + "Could not enter pending measurement lock within shutdown timeout.");
            throw;
        }

        try
        {
            if (_pendingMeasurement is null || _pendingMeasurement.IsCompleted)
            {
                _logger.LogInformation("Periodic measurement cancelled: No measurement in progress.");
                return;
            }

            _logger.LogInformation("Periodic measurement cancelled: Waiting for last measurement to finish.");
            await Task.WhenAny(_pendingMeasurement, Task.Delay(Timeout.Infinite, cancellationToken));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Graceful periodic measurement cancellation failed");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _semaphore.Dispose();
        _stoppingSource.Dispose();
        return _timer?.DisposeAsync() ?? default;
    }

    private void RunMeasurement()
    {
        _logger.LogDebug("Initiating periodic measurement");

        if (_semaphore.CurrentCount == 0)
        {
            _logger.LogWarning("Skipping measurement: Previous measurement is still in progress");
            return;
        }

        _semaphore.Wait();
        try
        {
            if (_pendingMeasurement != null && !_pendingMeasurement.IsCompleted)
            {
                _logger.LogWarning("Skipping measurement: Previous measurement is still in progress");
                return;
            }

            _pendingMeasurement = _measurementCollector
                .MeasureAsync(_stoppingSource.Token)
                .ContinueWith(t => 
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        _logger.LogDebug("Finished periodic measurement");
                    }
                    else if (t.IsFaulted)
                    {
                        _logger.LogError(t.Exception, "Measurement failed.");
                    }
                });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Measurement failed.");
        }
        finally
        {
            _semaphore.Release();
        }
    }
}