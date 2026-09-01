using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Rcm.Common;
using Rcm.Common.IO;
using Rcm.Persistence.Files;
using Rcm.Persistence.Files.Navigation;
using Rcm.Testing.Common.IO;
using Rcm.Testing.Threading.Tasks;
using static System.Globalization.CultureInfo;

namespace Rcm.UnitTests.Persistence.Files;

[TestFixture]
public class MeasurementsFileAccessTests
{
    private const string DataPath = "data";
    private static readonly string StoragePath = Path.Combine(DataPath, "measurements");

    [Test]
    public async Task WritesDataToDataStorageLocationUnderFilenameMatchingEntryDate()
    {
        // Given
        var fakeFileAccess = new FakeFileAccess();

        var collectedDataFileAccess = CreateCollectedDataFileAccess(fakeFileAccess);

        var firstEntry = new MeasurementEntry
        {
            Time = new DateTimeOffset(2018, 12, 30, 15, 10, 30, TimeSpan.FromHours(2)),
            CelsiusTemperature = 30m,
            RelativeHumidity = 41.2m,
            HpaPressure = 985.47m
        };

        var secondEntry = new MeasurementEntry
        {
            Time = new DateTimeOffset(2018, 12, 31, 10, 45, 15, TimeSpan.FromHours(1)),
            CelsiusTemperature = 33m,
            RelativeHumidity = 47.1m,
            HpaPressure = 994.36m
        };

        // When
        await collectedDataFileAccess.SaveAsync(firstEntry, CancellationToken.None);
        await collectedDataFileAccess.SaveAsync(secondEntry, CancellationToken.None);

        // Then
        var firstEntryPath = GetEntryFilePath(firstEntry.Time);
        Assert.True(fakeFileAccess.Exists(firstEntryPath));
        Assert.AreEqual(
            GetEntryRecord(firstEntry) + Environment.NewLine,
            fakeFileAccess.ReadAllText(firstEntryPath));

        var secondEntryPath = GetEntryFilePath(secondEntry.Time);
        Assert.True(fakeFileAccess.Exists(secondEntryPath));
        Assert.AreEqual(
            GetEntryRecord(secondEntry) + Environment.NewLine,
            fakeFileAccess.ReadAllText(secondEntryPath));
    }

    [Test]
    public void ReadsDataFromStorageLocationFilesBasedOnSuppliedRange()
    {
        // Given
        var fakeFileAccess = new FakeFileAccess();

        var collectedDataFileAccess = CreateCollectedDataFileAccess(fakeFileAccess);

        var startTime = new DateTimeOffset(2018, 12, 25, 15, 0, 0, TimeSpan.FromHours(1));
        var endTime = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.FromHours(-1));

        var entryDayBeforeStart = MakeMeasurementEntry(time: startTime - TimeSpan.FromDays(1));
        var entryHourBeforeStart = MakeMeasurementEntry(time: startTime - TimeSpan.FromHours(1));
        var entryOnStart = MakeMeasurementEntry(time: startTime);
        var firstEntryInMiddle = MakeMeasurementEntry(time: startTime + TimeSpan.FromDays(days: 2, hours: 10));
        var secondEntryInMiddle = MakeMeasurementEntry(time: startTime + TimeSpan.FromDays(days: 2, hours: 11));
        var entryOnEnd = MakeMeasurementEntry(time: endTime);
        var entryHourAfterEnd = MakeMeasurementEntry(time: endTime + TimeSpan.FromHours(1));
        var entryDayAfterEnd = MakeMeasurementEntry(time: endTime + TimeSpan.FromDays(1));

        StoreEntriesToFiles(
            fakeFileAccess,
            [
                entryDayBeforeStart,
                entryHourBeforeStart,
                entryOnStart,
                firstEntryInMiddle,
                secondEntryInMiddle,
                entryOnEnd,
                entryHourAfterEnd,
                entryDayAfterEnd
            ]);

        // When
        var readEntries = collectedDataFileAccess.Read(startTime, endTime, CancellationToken.None);

        // Then
        Assert.That(
            readEntries,
            Is.EquivalentTo(new[] { entryOnStart, firstEntryInMiddle, secondEntryInMiddle, entryOnEnd })
                .Using(new MeasurementEntryEqualityComparer()));
    }

    [Test]
    public void SkipsInvalidLinesInFiles()
    {
        // Given
        var time = new DateTimeOffset(2020, 1, 28, 18, 45, 0, TimeSpan.FromHours(1));

        var fakeFileAccess = new FakeFileAccess();

        var collectedDataFileAccess = CreateCollectedDataFileAccess(fakeFileAccess);

        var validEntries = new[] { MakeMeasurementEntry(time), MakeMeasurementEntry(time  + TimeSpan.FromHours(1)) };

        var measurementFileLines = new[]
        {
            GetEntryRecord(validEntries[0]),
            "Invalid line",
            GetEntryRecord(validEntries[1])
        };

        var path = GetEntryFilePath(time);
        fakeFileAccess.WriteAllLines(path, measurementFileLines);

        // When
        var readEntries = collectedDataFileAccess.Read(time - TimeSpan.FromHours(2), time + TimeSpan.FromHours(2), CancellationToken.None);

        // Then
        Assert.That(readEntries, Is.EquivalentTo(validEntries).Using(new MeasurementEntryEqualityComparer()));
    }

    [Test]
    public async Task AbortsWriteIfCancellationIsSignaledBeforeOrDuringFileOpening()
    {
        // Given
        using var cancellationTokenSource = new CancellationTokenSource();

        var dummyEntry = MakeMeasurementEntry();

        using var blockingFileAccess = new BlockingFileAccess();

        var collectedDataFileAccess = CreateCollectedDataFileAccess(blockingFileAccess);

        // When
        var savingTask = Task.Run(() => collectedDataFileAccess.SaveAsync(dummyEntry, cancellationTokenSource.Token));

        await blockingFileAccess.OpeningStarted;
        await cancellationTokenSource.CancelAsync();
        blockingFileAccess.Release();

        var savingCompleted = await savingTask.TryWait(TimeSpan.FromSeconds(1));

        // Then
        Assert.IsTrue(savingCompleted, nameof(savingCompleted));
        Assert.AreEqual(TaskStatus.Canceled, savingTask.Status);
    }

    [Test]
    public async Task AbortsReadWhenCancelledBetweenReadingFiles()
    {
        // Given
        using var cancellationTokenSource = new CancellationTokenSource();

        var dummyStart = new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));
        var dummyEnd = new DateTimeOffset(2000, 1, 10, 12, 0, 0, TimeSpan.FromHours(2));

        using var blockingFileAccess = new BlockingFileAccess();

        var collectedDataFileAccess = CreateCollectedDataFileAccess(blockingFileAccess);

        CreateDummyMeasurementFiles(blockingFileAccess.UnderlyingFileAccess, dummyStart, dummyEnd);

        // When
        var readIterator = collectedDataFileAccess.Read(dummyStart, dummyEnd, cancellationTokenSource.Token);
        var readingTask = Task.Run(() => readIterator.ToList(), cancellationTokenSource.Token);

        await blockingFileAccess.OpeningStarted;
        await cancellationTokenSource.CancelAsync();
        blockingFileAccess.Release();

        var readingCompleted = await readingTask.TryWait(TimeSpan.FromSeconds(1));

        // Then
        Assert.IsTrue(readingCompleted, nameof(readingCompleted));
        Assert.AreEqual(TaskStatus.Canceled, readingTask.Status);
    }

    private static MeasurementsFileAccess CreateCollectedDataFileAccess(
        IFileAccess fileAccess,
        IDataStorageLocation? dataStorageLocation = null)
    {
        return new MeasurementsFileAccess(
            NullLogger<MeasurementsFileAccess>.Instance,
            new MeasurementsFilesNavigator(dataStorageLocation ?? new DataStorageLocation(DataPath)),
            fileAccess);
    }

    private static void CreateDummyMeasurementFiles(IFileAccess file, DateTimeOffset start, DateTimeOffset end)
    {
        for (var day = start; day <= end; day += TimeSpan.FromDays(1))
        {
            var filePath = GetEntryFilePath(day);
            var dummyEntry = MakeMeasurementEntry(day);

            using (var measurementFile = file.AppendText(filePath))
            {
                measurementFile.WriteLine(GetEntryRecord(dummyEntry));
            }
        }
    }

    private static MeasurementEntry MakeMeasurementEntry(DateTimeOffset? time = null)
    {
        return MeasurementEntryFactory.Make(time, temperature: null, humidity: null, pressure: null);
    }

    private static void StoreEntriesToFiles(IFileAccess file, IEnumerable<MeasurementEntry> entries)
    {
        foreach (var entriesByDay in entries.GroupBy(e => new DateTimeOffset(e.Time.Date, e.Time.Offset)))
        {
            var filePath = GetEntryFilePath(entriesByDay.Key);
            file.WriteAllLines(filePath, entriesByDay.Select(GetEntryRecord));
        }
    }

    private static string GetEntryRecord(MeasurementEntry entry)
    {
        FormattableString output = $"{entry.Time:HH':'mmK} {entry.CelsiusTemperature} {entry.RelativeHumidity} {entry.HpaPressure}";
        return output.ToString(InvariantCulture);
    }

    private static string GetEntryFilePath(DateTimeOffset entryTime)
    {
        return Path.Combine(StoragePath, $"{entryTime:yyyy'-'MM'-'dd}.mst");
    }

    private class DataStorageLocation(string path) : IDataStorageLocation
    {
        public string GetDirectoryPath() => path;
    }

    private sealed class MeasurementEntryEqualityComparer : IEqualityComparer<MeasurementEntry>
    {
        public bool Equals(MeasurementEntry? x, MeasurementEntry? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.GetType() != y.GetType())
            {
                return false;
            }

            return x.Time.Equals(y.Time)
                && x.CelsiusTemperature == y.CelsiusTemperature
                && x.RelativeHumidity == y.RelativeHumidity
                && x.HpaPressure == y.HpaPressure;
        }

        public int GetHashCode(MeasurementEntry obj)
        {
            return HashCode.Combine(obj.Time, obj.CelsiusTemperature, obj.RelativeHumidity, obj.HpaPressure);
        }
    }

    private class BlockingFileAccess : IFileAccess, IDisposable
    {
        private readonly SemaphoreSlim _openingSemaphore = new(initialCount: 0);
        private readonly SemaphoreSlim _blockingSemaphore = new(initialCount: 0);

        public IFileAccess UnderlyingFileAccess { get; set; } = new FakeFileAccess();

        public Task OpeningStarted => _openingSemaphore.WaitAsync();

        public bool Exists(string path) => UnderlyingFileAccess.Exists(path);

        public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            _openingSemaphore.Release();
            _blockingSemaphore.Wait();

            return UnderlyingFileAccess.Open(path, mode, access, share);
        }

        public void Release()
        {
            _blockingSemaphore.Release();
        }

        public void Dispose()
        {
            _openingSemaphore.Dispose();
            _blockingSemaphore.Dispose();
        }
    }
}
