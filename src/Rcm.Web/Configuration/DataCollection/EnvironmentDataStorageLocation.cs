using System;
using Rcm.DataCollection.Files;

namespace Rcm.Web.Configuration.DataCollection
{
    internal class EnvironmentDataStorageLocation : IDataStorageLocation
    {
        public string Path { get; }

        public EnvironmentDataStorageLocation()
        {
            Path = Environment.GetEnvironmentVariable("DATA_PATH") ?? "";
        }
    }
}