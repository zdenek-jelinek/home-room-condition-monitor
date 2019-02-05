using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rcm.Common;
using Rcm.DataCollection.Files;

namespace Rcm.DataCollection
{
    public class CombinedFileAndMemoryCollectedDataStorage : ICollectedDataStorage, IDisposable
    {
        private readonly ILogger<CombinedFileAndMemoryCollectedDataStorage> _logger;
        private readonly IClock _clock;
        private readonly ICollectedDataFileAccess _fileAccess;

        private readonly List<MeasurementEntry> _currentDayRecords = new List<MeasurementEntry>(24 * 60);
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        private bool _disposed;

        public CombinedFileAndMemoryCollectedDataStorage(
            ILogger<CombinedFileAndMemoryCollectedDataStorage> logger,
            IClock clock,
            ICollectedDataFileAccess fileAccess)
        {
            _clock = clock;
            _logger = logger;
            _fileAccess = fileAccess;
        }

        public async Task StoreAsync(MeasurementEntry value)
        {
            try
            {
                _lock.EnterWriteLock();
                if (_currentDayRecords.Count != 0 && _currentDayRecords[0].Time.Date < value.Time.Date)
                {
                    _currentDayRecords.Clear();
                }
                    
                _currentDayRecords.Add(value);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            await _fileAccess.SaveAsync(value);
        }

        public IEnumerable<MeasurementEntry> GetCollectedData(DateTimeOffset start, DateTimeOffset end)
        {
            if (start > end)
            {
                throw new InvalidOperationException($"Date range {start:o}:{end:o} is not valid");
            }

            var now = _clock.Now;
            var todayMidnight = new DateTimeOffset(now.Date, now.Offset);
            var startMidnight = new DateTimeOffset(start.Date, start.Offset);
            if (startMidnight > todayMidnight)
            {
                return Enumerable.Empty<MeasurementEntry>();
            }

            if (end < todayMidnight)
            {
                return _fileAccess.Read(start, end);
            }

            if (end >= now)
            {
                end = now;
            }

            if (start < todayMidnight)
            {
                return _fileAccess
                    .Read(start, todayMidnight.AddSeconds(-1))
                    .Concat(GetTodaysData(todayMidnight, end));
            }
            else
            {
                return GetTodaysData(start, end);
            }
        }

        private IEnumerable<MeasurementEntry> GetTodaysData(DateTimeOffset start, DateTimeOffset end)
        {
            return GetTodaysData().Where(e => e.Time >= start && e.Time <= end);
        }

        private IReadOnlyCollection<MeasurementEntry> GetTodaysData()
        {
            try
            {
                _lock.EnterReadLock();
                return new List<MeasurementEntry>(_currentDayRecords);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _lock.Dispose();
            }
        }
    }
}
