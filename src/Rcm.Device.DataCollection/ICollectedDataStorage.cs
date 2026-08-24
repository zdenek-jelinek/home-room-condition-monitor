using Rcm.Device.DataCollection.Api;

namespace Rcm.Device.DataCollection;

public interface ICollectedDataStorage : ICollectedDataWriter, ICollectedDataAccessor
{
}