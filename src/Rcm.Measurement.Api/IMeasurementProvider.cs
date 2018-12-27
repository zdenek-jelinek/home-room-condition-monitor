using System.Threading.Tasks;

namespace Rcm.Measurement.Api
{
    public interface IMeasurementProvider
    {
        Task<MeasurementEntry> MeasureAsync();
    }
}
