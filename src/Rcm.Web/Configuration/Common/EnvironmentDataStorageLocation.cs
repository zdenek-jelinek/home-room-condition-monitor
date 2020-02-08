using System;
using System.IO;
using Rcm.Device.Common;

namespace Rcm.Web.Configuration.Common
{
    internal class EnvironmentDataStorageLocation : IDataStorageLocation
    {
        public string Path { get; }

        public EnvironmentDataStorageLocation()
        {
            var path = Environment.GetEnvironmentVariable("DATA_PATH");
            if (String.IsNullOrEmpty(path))
            {
                Path = Directory.GetCurrentDirectory();
            }
            else
            {
                Path = System.IO.Path.GetFullPath(path);
            }
        }
    }
}