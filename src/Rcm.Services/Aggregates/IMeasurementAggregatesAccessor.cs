using System;
using System.Collections.Generic;
using System.Threading;

namespace Rcm.Services.Aggregates;

public interface IMeasurementAggregatesAccessor
{
    IEnumerable<MeasurementAggregates> GetMeasurementAggregates(
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        int count,
        CancellationToken token);
}
