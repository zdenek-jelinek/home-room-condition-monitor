using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rcm.Common.Temporal;

namespace Rcm.Persistence.Files.Navigation;

public class MeasurementsFilesNavigator(IDataStorageLocation dataStorageLocation)
{
    public IEnumerable<(DateOnly Date, string Path)> GetFilePaths(DateTimeOffset start, DateTimeOffset end)
    {
        return new DateRange(start.ToDateOnly(), end.ToDateOnly())
            .EnumerateDates()
            .Select(date => (date, GetFilePath(date)));
    }

    public string GetFilePath(DateTimeOffset time)
    {
        return GetFilePath(time.ToDateOnly());
    }

    private string GetFilePath(DateOnly date)
    {
        var fileName = date.ToString("yyyy'-'MM'-'dd'.mst'");

        return Path.Combine(dataStorageLocation.GetDirectoryPath(), "measurements", fileName);
    }
}
