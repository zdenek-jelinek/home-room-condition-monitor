using System;
using System.Collections.Generic;
using System.IO;

namespace Rcm.Persistence.Files.Navigation;

public class MeasurementsFilesNavigator(IDataStorageLocation dataStorageLocation)
{
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
        var fileName = time.ToString("yyyy'-'MM'-'dd'.mst'");

        return Path.Combine(dataStorageLocation.GetDirectoryPath(), "measurements", fileName);
    }
}
