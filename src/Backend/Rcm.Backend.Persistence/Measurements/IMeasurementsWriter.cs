using System.Threading;
using System.Threading.Tasks;

namespace Rcm.Backend.Persistence.Measurements;

public interface IMeasurementsWriter
{
    Task StoreAsync(DeviceMeasurements measurements, CancellationToken token);
}