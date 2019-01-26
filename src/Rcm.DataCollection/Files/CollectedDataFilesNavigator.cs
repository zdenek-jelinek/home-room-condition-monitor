using System;
using System.Collections.Generic;
using System.IO;

namespace Rcm.DataCollection.Files
{
    public class CollectedDataFilesNavigator
    {
        private readonly IDataStorageLocation _dataStorageLocation;

        public CollectedDataFilesNavigator(IDataStorageLocation dataStorageLocation)
        {
            _dataStorageLocation = dataStorageLocation;
        }

        public IEnumerable<(DateTime date, string path)> GetFilePaths(DateTimeOffset start, DateTimeOffset end)
        {
            var startDate = new DateTimeOffset(start.Date, start.Offset);
            for (var date = startDate; date <= end; date = date.AddDays(1))
            {
                yield return (date.Date, GetFilePath(date));
            }
        }

        public string GetFilePath(DateTimeOffset time)
        {
            return Path.Combine(_dataStorageLocation.Path, "measurements", time.ToString("yyyy'-'MM'-'dd'.mst'"));
        }
    }
}
