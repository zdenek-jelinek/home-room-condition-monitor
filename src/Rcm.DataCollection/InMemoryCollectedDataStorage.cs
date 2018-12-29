using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.DataCollection
{
    public class InMemoryCollectedDataStorage : ICollectedDataStorage
    {
        private readonly ICollection<MeasurementEntry> _entries = new List<MeasurementEntry>();

        public Task StoreAsync(MeasurementEntry value)
        {
            _entries.Add(value);
            return Task.CompletedTask;
        }

        public IEnumerable<MeasurementEntry> GetCollectedDataAsync(DateTimeOffset start, DateTimeOffset end)
        {
            return _entries.Where(e => e.Time >= start && e.Time <= end);
        }
    }
}