using System.Threading;
using System.Threading.Tasks;

namespace Rcm.Services.Measurements.Collection;

public interface IMeasurementCollector
{
    Task MeasureAsync(CancellationToken token);
}
