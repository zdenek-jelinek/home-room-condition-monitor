using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.DataCollection
{
    public interface ICollectedDataStorage
    {
        Task StoreAsync(MeasurementEntry value);
    }
}