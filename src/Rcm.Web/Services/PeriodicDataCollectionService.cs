using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rcm.DataCollection.Api;

namespace Rcm.Web.Services
{
    public class PeriodicDataCollectionService : IHostedService, IDisposable
    {
        private readonly ILogger<PeriodicDataCollectionService> _logger;
        private readonly IMeasurementCollector _measurementCollector;

        private readonly object _sync = new object();

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

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cancelling periodic measurements");

            _ = _timer?.Change(Timeout.Infinite, Timeout.Infinite);

            var lockTaken = false;
            try
            {
                Monitor.TryEnter(_sync, Program.ShutdownTimeout, ref lockTaken);
                if (!lockTaken)
                {
                    _logger.LogWarning(
                        "Graceful periodic measurement cancellation failed: "
                        + $"Could not enter pending measurement lock within shutdown timeout of {Program.ShutdownTimeout}.");
                    return Task.CompletedTask;
                }

                var measurement = _pendingMeasurement;
                if (measurement is null)
                {
                    _logger.LogInformation("Periodic measurement cancelled: No measurement in progress.");
                    return Task.CompletedTask;
                }

                _logger.LogInformation("Periodic measurement cancelled: Waiting for last measurement to finish.");
                return measurement;
            }
            catch (Exception e)
            {
                _logger.LogWarning("Graceful periodic measurement cancellation failed", e);
                return Task.CompletedTask;
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(_sync);
                }
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private void RunMeasurement()
        {
            _logger.LogDebug("Initiating periodic measurement");

            if (!(Volatile.Read(ref _pendingMeasurement) is null))
            {
                _logger.LogWarning("Skipping measurement: Previous measurement is still in progress");
                return;
            }

            try
            {
                Task measurement;
                lock (_sync)
                {
                    if (!(_pendingMeasurement is null))
                    {
                        _logger.LogWarning("Skipping measurement: Previous measurement is still in progress");
                        return;
                    }

                    measurement = _pendingMeasurement = _measurementCollector.MeasureAsync();
                }

                // wait outside of locks
                measurement.Wait();
            }
            catch (Exception e)
            {
                _logger.LogError("Measurement failed.", e);
            }
            finally
            {
                lock (_sync)
                {
                    _pendingMeasurement = null;
                }
            }

            _logger.LogDebug("Finished periodic measurement");
        }
    }
}