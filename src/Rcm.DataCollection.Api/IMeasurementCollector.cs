using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rcm.DataCollection.Api
{
    public interface IMeasurementCollector
    {
        Task MeasureAsync(CancellationToken token);

        (TimeSpan nextMeasurementDelay, TimeSpan measurementPeriod) MeasurementTimings { get; }
    }
}
