using System.Threading;
using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.Sensors.Abstractions;

public interface IMeasurementProvider
{
    Task<MeasurementEntry> MeasureAsync(CancellationToken token);
}