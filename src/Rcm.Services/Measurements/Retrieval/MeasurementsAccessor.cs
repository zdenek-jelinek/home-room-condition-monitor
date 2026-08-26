using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Rcm.Common;
using Rcm.Persistence.Abstractions;

namespace Rcm.Services.Measurements.Retrieval;

public class MeasurementsAccessor(IMeasurementsReader measurementsReader) : IMeasurementsAccessor
{
    public IReadOnlyList<MeasurementEntry> GetMeasurements(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        return measurementsReader.GetCollectedData(start, end, cancellationToken).ToArray();
    }
}
