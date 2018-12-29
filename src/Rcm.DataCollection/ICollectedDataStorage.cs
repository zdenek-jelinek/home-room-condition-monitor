using Rcm.DataCollection.Api;

namespace Rcm.DataCollection
{
    public interface ICollectedDataStorage : ICollectedDataWriter, ICollectedDataAccessor
    {
    }
}
