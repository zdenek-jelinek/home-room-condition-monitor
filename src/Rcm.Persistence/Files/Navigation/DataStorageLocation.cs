using System.IO;
using Microsoft.Extensions.Options;

namespace Rcm.Persistence.Files.Navigation;

public class DataStorageLocation(IOptionsMonitor<DataStorageOptions> dataStorageOptions) : IDataStorageLocation
{
    public string GetDirectoryPath()
    {
        var fullPath = Path.GetFullPath(dataStorageOptions.CurrentValue.Path);

        EnsureDirectoryExists(fullPath);

        return fullPath;
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
