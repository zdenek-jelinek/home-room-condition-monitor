using System;
using System.Collections.Generic;
using System.Threading;

namespace Rcm.Backend.Persistence.Measurements;

public interface IMeasurementsReader
{
    IAsyncEnumerable<Measurement> GetMeasurementsAsync(string deviceId, DateTime start, DateTime end, CancellationToken token);
}