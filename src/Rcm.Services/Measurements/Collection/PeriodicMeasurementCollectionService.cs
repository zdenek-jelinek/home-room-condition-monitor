using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Rcm.Services.Measurements.Collection;

public class PeriodicMeasurementCollectionService : BackgroundService
{
    private readonly IMeasurementTimingsCalculator _measurementTimingsCalculator;
    private readonly IMeasurementCollector _measurementCollector;

    public PeriodicMeasurementCollectionService(IMeasurementTimingsCalculator measurementTimingsCalculator, IMeasurementCollector measurementCollector)
    {
        _measurementTimingsCalculator = measurementTimingsCalculator;
        _measurementCollector = measurementCollector;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timings = _measurementTimingsCalculator.DetermineMeasurementTimings();

        await Task.Delay(timings.InitialDelay, stoppingToken);

        using var timer = new PeriodicTimer(timings.Period);

        while (!stoppingToken.IsCancellationRequested)
        {
            await _measurementCollector.MeasureAsync(stoppingToken);

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
