using System.IO;
using Microsoft.Extensions.Options;
using Rcm.Persistence.Files.Navigation;

namespace Rcm.Web.Configuration.Common;

internal class DataStorageLocation(IOptionsMonitor<DataStorageOptions> dataStorageOptions) : IDataStorageLocation
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
