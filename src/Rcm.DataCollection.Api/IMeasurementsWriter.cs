using System.Threading;
using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.DataCollection.Api;

public interface IMeasurementsWriter
{
    Task StoreAsync(MeasurementEntry value, CancellationToken token);
}
