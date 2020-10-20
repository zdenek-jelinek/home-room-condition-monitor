using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.Device.DataCollection.Files
{
    public interface ICollectedDataFileAccess
    {
        Task SaveAsync(MeasurementEntry entry, CancellationToken token);
        IEnumerable<MeasurementEntry> Read(DateTimeOffset start, DateTimeOffset end, CancellationToken token);
    }
}