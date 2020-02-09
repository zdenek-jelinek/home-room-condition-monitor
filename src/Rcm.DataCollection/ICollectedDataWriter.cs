using System.Threading;
using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.DataCollection
{
    public interface ICollectedDataWriter
    {
        Task StoreAsync(MeasurementEntry value, CancellationToken token);
    }
}