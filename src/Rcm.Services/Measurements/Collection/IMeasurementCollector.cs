using System.Threading;
using System.Threading.Tasks;

namespace Rcm.Services.Measurements.Collection;

public interface IMeasurementCollector
{
    MeasurementCollectionTimings DetermineMeasurementTimings();
    Task MeasureAsync(CancellationToken token);
}
