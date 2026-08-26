using System;
using System.Collections.Generic;
using System.Threading;
using Rcm.Common;

namespace Rcm.DataCollection.Api;

public interface IMeasurementsReader
{
    IEnumerable<MeasurementEntry> GetCollectedData(DateTimeOffset start, DateTimeOffset end, CancellationToken token);
}
