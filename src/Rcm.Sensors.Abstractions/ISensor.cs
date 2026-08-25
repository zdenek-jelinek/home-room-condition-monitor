using System.Threading;
using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.Sensors.Abstractions;

public interface ISensor
{
    Task<MeasurementEntry> MeasureAsync(CancellationToken token);
}