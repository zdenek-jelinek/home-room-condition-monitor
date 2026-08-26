using System.Threading;
using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.Persistence.Abstractions;

public interface IMeasurementsWriter
{
    Task StoreAsync(MeasurementEntry value, CancellationToken token);
}
