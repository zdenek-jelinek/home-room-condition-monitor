using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Rcm.Common;
using Rcm.DataCollection.Api;

namespace Rcm.Services.Measurements.Retrieval;

public class MeasurementsAccessor(ICollectedDataAccessor collectedDataAccessor) : IMeasurementsAccessor
{
    public IReadOnlyList<MeasurementEntry> GetMeasurements(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        return collectedDataAccessor.GetCollectedData(start, end, cancellationToken).ToArray();
    }
}
