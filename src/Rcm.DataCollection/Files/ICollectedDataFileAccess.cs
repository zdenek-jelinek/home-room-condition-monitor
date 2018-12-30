using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.DataCollection.Files
{
    public interface ICollectedDataFileAccess
    {
        Task SaveAsync(MeasurementEntry entry);
        IEnumerable<MeasurementEntry> Read(DateTimeOffset start, DateTimeOffset end);
    }
}