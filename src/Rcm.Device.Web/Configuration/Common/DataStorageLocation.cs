using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using Rcm.Device.Common;

namespace Rcm.Device.Web.Configuration.Common;

internal class DataStorageLocation : IDataStorageLocation
{
    private readonly Lazy<string> _fullPath;

    [Required(AllowEmptyStrings = false)]
    public string? Path { get; set; }

    public DataStorageLocation()
    {
        _fullPath = new Lazy<string>(GetFullPath, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string GetDirectoryPath() => _fullPath.Value;

    private string GetFullPath()
    {
        var path = Path
            ?? throw new InvalidOperationException($"{nameof(Path)} is unexpectedly null.");

        var fullPath = System.IO.Path.GetFullPath(path);

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