using System.Threading;
using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.DataCollection;

public interface IMeasurementsWriter
{
    Task StoreAsync(MeasurementEntry value, CancellationToken token);
}
