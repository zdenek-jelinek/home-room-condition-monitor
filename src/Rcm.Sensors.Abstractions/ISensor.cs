using System.Threading;
using System.Threading.Tasks;

namespace Rcm.Sensors.Abstractions;

public interface ISensor
{
    Task<SensorMeasurement> ReadMeasurementAsync(CancellationToken token);
}
