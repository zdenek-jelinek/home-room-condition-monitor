using System;
using System.Collections.Generic;
using System.Threading;
using Rcm.Common;

namespace Rcm.Services.Abstractions.Measurements.Retrieval;

public interface IMeasurementsAccessor
{
    IReadOnlyList<MeasurementEntry> GetMeasurements(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken);
}
