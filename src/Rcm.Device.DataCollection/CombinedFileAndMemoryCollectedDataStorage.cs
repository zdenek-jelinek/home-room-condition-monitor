using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rcm.Common;
using Rcm.Device.DataCollection.Files;

namespace Rcm.Device.DataCollection;

public class CombinedFileAndMemoryCollectedDataStorage : ICollectedDataStorage, IDisposable
{
    private const int MeasurementsPerDay = 24 * 60;

    private readonly IClock _clock;
    private readonly ICollectedDataFileAccess _fileAccess;

    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    private List<MeasurementEntry>? _currentDayRecords;

    private bool _disposed;

    public CombinedFileAndMemoryCollectedDataStorage(
        IClock clock,
        ICollectedDataFileAccess fileAccess)
    {
        _clock = clock;
        _fileAccess = fileAccess;
    }

    public async Task StoreAsync(MeasurementEntry value, CancellationToken token)
    {
        try
        {
            _lock.EnterWriteLock();
            if (_currentDayRecords is null)
            {
                _currentDayRecords = LoadTodaysRecordsFromFile(token);
            }

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

        await _fileAccess.SaveAsync(value, token);
    }

    public IEnumerable<MeasurementEntry> GetCollectedData(DateTimeOffset start, DateTimeOffset end, CancellationToken token)
    {
        if (start > end)
        {
            throw new InvalidOperationException($"Date range {start:o}:{end:o} is not valid");
        }

        var now = _clock.Now;
        if (start > now)
        {
            return Enumerable.Empty<MeasurementEntry>();
        }
            
        start = start.ToOffset(now.Offset);
        end = end.ToOffset(now.Offset);

        var todayMidnight = new DateTimeOffset(now.Date, now.Offset);
        var startMidnight = new DateTimeOffset(start.Date, now.Offset);
        if (startMidnight > todayMidnight)
        {
            return Enumerable.Empty<MeasurementEntry>();
        }

        if (end < todayMidnight)
        {
            return _fileAccess.Read(start, end, token);
        }

        if (end >= now)
        {
            end = now;
        }

        if (start < todayMidnight)
        {
            return _fileAccess
                .Read(start, todayMidnight.AddSeconds(-1), token)
                .Concat(GetTodaysData(todayMidnight, end, token));
        }
        else
        {
            return GetTodaysData(start, end, token);
        }
    }

    private IEnumerable<MeasurementEntry> GetTodaysData(DateTimeOffset start, DateTimeOffset end, CancellationToken token)
    {
        return GetTodaysData(token).Where(e => e.Time >= start && e.Time <= end);
    }

    private IReadOnlyCollection<MeasurementEntry> GetTodaysData(CancellationToken token)
    {
        EnsureTodaysRecordsAreLoaded(token);

        try
        {
            _lock.EnterReadLock();
            return new List<MeasurementEntry>(_currentDayRecords!);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private void EnsureTodaysRecordsAreLoaded(CancellationToken token)
    {
        if (_currentDayRecords is null)
        {
            try
            {
                _lock.EnterWriteLock();
                if (_currentDayRecords is null)
                {
                    _currentDayRecords = LoadTodaysRecordsFromFile(token);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

    private List<MeasurementEntry> LoadTodaysRecordsFromFile(CancellationToken token)
    {
        var now = _clock.Now;
        var startOfToday = new DateTimeOffset(now.Date, now.Offset);
        var endOfToday = startOfToday.AddDays(1).AddTicks(-1);

        var result = new List<MeasurementEntry>(MeasurementsPerDay);
        result.AddRange(_fileAccess.Read(startOfToday, endOfToday, token));

        return result;
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