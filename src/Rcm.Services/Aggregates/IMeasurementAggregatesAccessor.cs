using System.Collections.Generic;
using System.Threading;

namespace Rcm.Services.Aggregates;

public interface IMeasurementAggregatesAccessor
{
    IEnumerable<MeasurementAggregates> GetMeasurementAggregates(MeasurementAggregatesQuery query, CancellationToken cancellationToken);
}
