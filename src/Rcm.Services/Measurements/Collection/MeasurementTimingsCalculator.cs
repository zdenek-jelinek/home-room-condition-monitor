using System;
using Rcm.Common.Temporal;

namespace Rcm.Services.Measurements.Collection;

public class MeasurementTimingsCalculator(IClock clock) : IMeasurementTimingsCalculator
{
    private static readonly TimeSpan MeasurementPeriod = TimeSpan.FromSeconds(6);

    public MeasurementCollectionTimings DetermineMeasurementTimings()
    {
        var now = clock.Now;

        var nextMeasurementDelay = now.Second == 0 ? 0 : 60 - now.Second;

        return new()
        {
            InitialDelay = TimeSpan.FromSeconds(nextMeasurementDelay),
            Period = MeasurementPeriod
        };
    }
}
