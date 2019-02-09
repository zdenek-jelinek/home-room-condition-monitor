using System;
using System.Collections.Generic;

namespace Rcm.Aggregates.Api
{
    public interface IMeasurementAggregatesAccessor
    {
        IEnumerable<MeasurementAggregates> GetMeasurementAggregates(DateTimeOffset startTime, DateTimeOffset endTime, int count);
    }
}
