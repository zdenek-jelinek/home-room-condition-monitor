using System;
using System.Threading.Tasks;

namespace Rcm.DataCollection.Api
{
    public interface IMeasurementCollector
    {
        Task MeasureAsync();

        (TimeSpan nextMeasurementDelay, TimeSpan measurementPeriod) MeasurementTimings { get; }
    }
}
