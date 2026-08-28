using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.Persistence;

public class InMemoryMeasurementsStorage : IMeasurementsStorage
{
    private readonly List<MeasurementEntry> _entries = new();

    public Task StoreAsync(MeasurementEntry value, CancellationToken token)
    {
        _entries.Add(value);
        return Task.CompletedTask;
    }

    public IEnumerable<MeasurementEntry> GetCollectedData(DateTimeOffset start, DateTimeOffset end, CancellationToken token)
    {
        return _entries.Where(e => e.Time >= start && e.Time <= end);
    }
}
