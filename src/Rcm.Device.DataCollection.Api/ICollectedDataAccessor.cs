using System;
using System.Collections.Generic;
using System.Threading;
using Rcm.Common;

namespace Rcm.Device.DataCollection.Api;

public interface ICollectedDataAccessor
{
    IEnumerable<MeasurementEntry> GetCollectedData(DateTimeOffset start, DateTimeOffset end, CancellationToken token);
}