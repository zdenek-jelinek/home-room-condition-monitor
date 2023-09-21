namespace Rcm.Device.Common;

public interface IDataStorageLocation
{
    /// <summary>
    /// Provides full path to storage location and ensures this location exists.
    /// Subsequent components are expected to create their individual directories to store the data.
    /// </summary>
    /// <returns>Full path to storage location.</returns>
    string GetDirectoryPath();
}