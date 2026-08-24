using System.Threading;
using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.Device.Measurement.Api;

public interface IMeasurementProvider
{
    Task<MeasurementEntry> MeasureAsync(CancellationToken token);
}