using System.Threading;
using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.Measurement.Api
{
    public interface IMeasurementProvider
    {
        Task<MeasurementEntry> MeasureAsync(CancellationToken token);
    }
}
