using System;
using System.IO;
using System.Threading;
using Rcm.Device.Common;

namespace Rcm.Connector.Configuration
{
    public class FileBackendStorageLocation : IFileBackendStorageLocation
    {
        private const string BackendConfigurationDirectoryName = "backend";

        private readonly IDataStorageLocation _dataStorageLocation;

        private readonly Lazy<string> _pathToExistingDirectory;

        public string GetDirectoryPath() => _pathToExistingDirectory.Value;

        public FileBackendStorageLocation(IDataStorageLocation dataStorageLocation)
        {
            _dataStorageLocation = dataStorageLocation;
            _pathToExistingDirectory = new Lazy<string>(
                ComposeDirectoryPathAndEnsureExistence,
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private string ComposeDirectoryPathAndEnsureExistence()
        {
            var path = Path.Combine(_dataStorageLocation.GetDirectoryPath(), BackendConfigurationDirectoryName);
            EnsureDirectoryExists(path);
            return path;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
