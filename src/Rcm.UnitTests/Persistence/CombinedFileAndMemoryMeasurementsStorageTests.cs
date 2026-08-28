using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rcm.Common;
using Rcm.Persistence;
using Rcm.Persistence.Files;
using Rcm.Testing.Common.Temporal;

namespace Rcm.UnitTests.Persistence;

[TestFixture]
public class CombinedFileAndMemoryMeasurementsStorageTests
{
    [Test]
    public async Task StoresAllMeasurementsToFile()
    {
        // Given
        var entry = new MeasurementEntry
        {
            Time = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero),
            CelsiusTemperature = 10m,
            RelativeHumidity = 45m,
            HpaPressure = 980m
        };

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess();

        using var combinedStorage = MakeCombinedDataStorage(fileAccess: spyCollectedDataFileAccess);

        // When
        await combinedStorage.StoreAsync(entry, CancellationToken.None);

        // Then
        Assert.IsNotNull(spyCollectedDataFileAccess.SavedEntry);

        Assert.AreEqual(entry.Time, spyCollectedDataFileAccess.SavedEntry!.Time);
        Assert.AreEqual(entry.CelsiusTemperature, spyCollectedDataFileAccess.SavedEntry.CelsiusTemperature);
        Assert.AreEqual(entry.RelativeHumidity, spyCollectedDataFileAccess.SavedEntry.RelativeHumidity);
        Assert.AreEqual(entry.HpaPressure, spyCollectedDataFileAccess.SavedEntry.HpaPressure);
    }

    [Test]
    public async Task AccessFilesForDataInThePastReadsTodayDataFromMemoryAndDoesNotReadDataFromFuture()
    {
        // Given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);
        var todayMidnight = new DateTimeOffset(now.Date, now.Offset);

        var startTimeBeforeToday = now - TimeSpan.FromDays(2);
        var endTimeInFuture = now + TimeSpan.FromDays(2);

        var pastEntry = MakeMeasurementEntry(time: startTimeBeforeToday + TimeSpan.FromMinutes(10));
        var todaysEntry = MakeMeasurementEntry(time: now - TimeSpan.FromMinutes(20));

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess { Entries = [pastEntry] };

        using var combinedStorage = MakeCombinedDataStorage(now, spyCollectedDataFileAccess);

        await combinedStorage.StoreAsync(todaysEntry, CancellationToken.None);

        // When
        var entries = combinedStorage
            .GetCollectedData(startTimeBeforeToday, endTimeInFuture, CancellationToken.None)
            .ToList();

        // Then
        Assert.IsNotNull(spyCollectedDataFileAccess.ReadRange);

        var (fileReadStart, fileReadEnd) = spyCollectedDataFileAccess.ReadRange.GetValueOrDefault();
        Assert.AreEqual(startTimeBeforeToday, fileReadStart);
        Assert.AreEqual(todayMidnight.AddSeconds(-1), fileReadEnd);

        CollectionAssert.AreEquivalent(new[] { pastEntry, todaysEntry }, entries);
    }

    [Test]
    public void ReadsDataOlderThanTodayFromFiles()
    {
        // Given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var startTimeBeforeToday = now - TimeSpan.FromDays(2);
        var endTimeBeforeToday = now - TimeSpan.FromDays(1);

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess();

        using var combinedStorage = MakeCombinedDataStorage(now, spyCollectedDataFileAccess);

        // When
        _ = combinedStorage
            .GetCollectedData(startTimeBeforeToday, endTimeBeforeToday, CancellationToken.None)
            .ToList();

        // Then
        Assert.IsNotNull(spyCollectedDataFileAccess.ReadRange);

        var (readStart, readEnd) = spyCollectedDataFileAccess.ReadRange.GetValueOrDefault();
        Assert.AreEqual(startTimeBeforeToday, readStart);
        Assert.AreEqual(endTimeBeforeToday, readEnd);
    }

    [Test]
    [TestCase(-2)]
    [TestCase(2)]
    public void ReadsDataJustBeforeTodayFromFilesRegardlessOfOffset(int offset)
    {
        // Given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);
        var todayMidnight = new DateTimeOffset(now.Date, now.Offset);

        var startTimeBeforeMidnight = todayMidnight.ToOffset(TimeSpan.FromHours(offset)) - TimeSpan.FromMinutes(30);
        var endTimeBeforeMidnight = startTimeBeforeMidnight + TimeSpan.FromMinutes(10);

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess();

        using var combinedStorage = MakeCombinedDataStorage(now, spyCollectedDataFileAccess);

        // When
        _ = combinedStorage
            .GetCollectedData(startTimeBeforeMidnight, endTimeBeforeMidnight, CancellationToken.None)
            .ToList();

        // Then
        Assert.IsNotNull(spyCollectedDataFileAccess.ReadRange);

        var (readStart, readEnd) = spyCollectedDataFileAccess.ReadRange.GetValueOrDefault();
        Assert.AreEqual(startTimeBeforeMidnight.ToUniversalTime(), readStart);
        Assert.AreEqual(endTimeBeforeMidnight.ToUniversalTime(), readEnd);
    }

    [Test]
    public async Task DoesNotAccessFilesToReadTodaysDataAfterPreviousOperation()
    {
        // Given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var startTimeOnToday = now - TimeSpan.FromMinutes(30);
        var endTimeOnToday = now - TimeSpan.FromMinutes(10);

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess();

        using var combinedStorage = MakeCombinedDataStorage(now, spyCollectedDataFileAccess);

        var storedEntry = MakeMeasurementEntry(time: now - TimeSpan.FromMinutes(20));

        await combinedStorage.StoreAsync(storedEntry, CancellationToken.None);
        spyCollectedDataFileAccess.Reset();

        // When
        var entries = combinedStorage
            .GetCollectedData(startTimeOnToday, endTimeOnToday, CancellationToken.None)
            .ToList();

        // Then
        Assert.IsNull(spyCollectedDataFileAccess.ReadRange);

        CollectionAssert.AreEquivalent(new[] { storedEntry }, entries);
    }

    [Test]
    public async Task AccessesFilesForOlderDaysAndNotForTodayForDateRangeIncludingToday()
    {
        // Given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);
        var todayMidnight = new DateTimeOffset(now.Date, now.Offset);

        var startTimeBeforeToday = now - TimeSpan.FromDays(2);
        var endTimeOnToday = now - TimeSpan.FromMinutes(10);

        var olderEntry = MakeMeasurementEntry(time: startTimeBeforeToday + TimeSpan.FromMinutes(10));
        var todaysEntry = MakeMeasurementEntry(time: now - TimeSpan.FromMinutes(20));

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess { Entries = [olderEntry] };

        using var combinedStorage = MakeCombinedDataStorage(now, spyCollectedDataFileAccess);

        await combinedStorage.StoreAsync(todaysEntry, CancellationToken.None);

        // When
        var entries = combinedStorage
            .GetCollectedData(startTimeBeforeToday, endTimeOnToday, CancellationToken.None)
            .ToList();

        // Then
        Assert.IsNotNull(spyCollectedDataFileAccess.ReadRange);

        var (fileReadStart, fileReadEnd) = spyCollectedDataFileAccess.ReadRange.GetValueOrDefault();
        Assert.AreEqual(startTimeBeforeToday, fileReadStart);
        Assert.AreEqual(todayMidnight.AddSeconds(-1), fileReadEnd);

        CollectionAssert.AreEquivalent(new[] { olderEntry, todaysEntry }, entries);
    }

    [Test]
    public void TodaysDataReadWithoutAnyPrecedingStoresIncludeDataStoredInFile()
    {
        // Given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var startTimeOnToday = now - TimeSpan.FromHours(1);
        var endTimeOnToday = now + TimeSpan.FromHours(1);

        var entryStoredInFile = MakeMeasurementEntry(time: now);

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess { Entries = [entryStoredInFile] };

        using var combinedStorage = MakeCombinedDataStorage(now, spyCollectedDataFileAccess);

        // When
        var readEntries = combinedStorage
            .GetCollectedData(startTimeOnToday, endTimeOnToday, CancellationToken.None)
            .ToList();

        // Then
        CollectionAssert.AreEquivalent(new[] { entryStoredInFile }, readEntries);
    }

    [Test]
    public async Task TodaysDataReadAfterStoreIncludeDataAlreadyStoredInFile()
    {
        // Given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var startTimeOnToday = now - TimeSpan.FromHours(10);
        var endTimeOnToday = now + TimeSpan.FromHours(10);

        var entryPreviouslyStoredInFile = MakeMeasurementEntry(time: now - TimeSpan.FromHours(2));

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess { Entries = [entryPreviouslyStoredInFile] };

        using var combinedStorage = MakeCombinedDataStorage(now, spyCollectedDataFileAccess);

        var newEntry = new MeasurementEntry { Time = now, CelsiusTemperature = 25m, RelativeHumidity = 32m, HpaPressure = 985m };

        // When
        await combinedStorage.StoreAsync(newEntry, CancellationToken.None);

        var readEntries = combinedStorage
            .GetCollectedData(startTimeOnToday, endTimeOnToday, CancellationToken.None)
            .ToList();

        // Then
        CollectionAssert.AreEquivalent(new[] { entryPreviouslyStoredInFile, newEntry }, readEntries);
    }

    [Test]
    public async Task ReturnsTodaysEntriesWithinRangeForTodayRange()
    {
        // Given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var startTimeOnToday = now - TimeSpan.FromMinutes(20);
        var endTimeOnToday = now - TimeSpan.FromMinutes(10);

        var entryBeforeStart = MakeMeasurementEntry(time: startTimeOnToday - TimeSpan.FromMinutes(10));
        var entryOnStart = MakeMeasurementEntry(time: startTimeOnToday);
        var entryInsideRange = MakeMeasurementEntry(time: startTimeOnToday + TimeSpan.FromMinutes(5));
        var entryOnEnd = MakeMeasurementEntry(time: endTimeOnToday);
        var entryAfterEnd = MakeMeasurementEntry(time: endTimeOnToday + TimeSpan.FromMinutes(5));

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess();

        using var combinedStorage = MakeCombinedDataStorage(now, spyCollectedDataFileAccess);

        await combinedStorage.StoreAsync(entryBeforeStart, CancellationToken.None);
        await combinedStorage.StoreAsync(entryOnStart, CancellationToken.None);
        await combinedStorage.StoreAsync(entryInsideRange, CancellationToken.None);
        await combinedStorage.StoreAsync(entryOnEnd, CancellationToken.None);
        await combinedStorage.StoreAsync(entryAfterEnd, CancellationToken.None);

        // When
        var entries = combinedStorage
            .GetCollectedData(startTimeOnToday, endTimeOnToday, CancellationToken.None)
            .ToList();

        // Then
        CollectionAssert.AreEquivalent(new[] { entryOnStart, entryInsideRange, entryOnEnd }, entries);
    }

    [Test]
    [TestCase(-2)]
    [TestCase(2)]
    public void ReturnsTodaysEntriesForTimesAtOrAfterMidnightRegardlessOfOffset(int offset)
    {
        // Given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);
        var todayMidnight = new DateTimeOffset(now.Date, now.Offset);

        var startTimeOnMidnightInDifferentOffset = todayMidnight.ToOffset(TimeSpan.FromHours(offset));
        var endTimeAfterMidnightInDifferentOffset = startTimeOnMidnightInDifferentOffset + TimeSpan.FromMinutes(30);

        var entryPreviouslyStoredInFile = MakeMeasurementEntry(time: todayMidnight + TimeSpan.FromMinutes(10));

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess { Entries = [entryPreviouslyStoredInFile] };

        using var combinedStorage = MakeCombinedDataStorage(now, spyCollectedDataFileAccess);

        // When
        var readEntries = combinedStorage
            .GetCollectedData(startTimeOnMidnightInDifferentOffset, endTimeAfterMidnightInDifferentOffset, CancellationToken.None)
            .ToList();

        // Then
        CollectionAssert.AreEquivalent(new[] { entryPreviouslyStoredInFile }, readEntries);
    }

    [Test]
    [TestCase(-2)]
    [TestCase(2)]
    public void ReturnsTodaysEntriesForTimesBeforeNextMidnightRegardlessOfOffset(int offset)
    {
        // Given
        var hourBeforeMidnight = new DateTimeOffset(2018, 12, 30, 23, 0, 0, TimeSpan.Zero);

        var startTimeBeforeNextMidnightInDifferentOffset = hourBeforeMidnight.ToOffset(TimeSpan.FromHours(offset)) - TimeSpan.FromMinutes(30);
        var endTimeBeforeNextMidnightInDifferentOffset = startTimeBeforeNextMidnightInDifferentOffset + TimeSpan.FromMinutes(30);

        var entryPreviouslyStoredInFile = MakeMeasurementEntry(time: hourBeforeMidnight - TimeSpan.FromMinutes(10));

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess { Entries = [entryPreviouslyStoredInFile] };

        using var combinedStorage = MakeCombinedDataStorage(hourBeforeMidnight, spyCollectedDataFileAccess);

        // When
        var readEntries = combinedStorage
            .GetCollectedData(startTimeBeforeNextMidnightInDifferentOffset, endTimeBeforeNextMidnightInDifferentOffset, CancellationToken.None)
            .ToList();

        // Then
        CollectionAssert.AreEquivalent(new[] { entryPreviouslyStoredInFile }, readEntries);
    }

    [Test]
    public void DoesNotAccessFilesForDataBeforeQueriedRangeDueToNegativeOffset()
    {
        // Given
        var midnightInUtc = new DateTimeOffset(2018, 12, 30, 0, 0, 0, TimeSpan.Zero);
        var timeEquivalentToUtcMidnightInMinusFive = midnightInUtc.ToOffset(TimeSpan.FromHours(-5));

        var startSkew = TimeSpan.FromMinutes(10);
        var rangeSize = TimeSpan.FromHours(2);
        var startTimeBeforeTodayInDifferentZone = timeEquivalentToUtcMidnightInMinusFive + startSkew;
        var endTimeBeforeTodayInDifferentZone = startTimeBeforeTodayInDifferentZone + rangeSize;

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess();

        using var combinedStorage = MakeCombinedDataStorage(midnightInUtc, spyCollectedDataFileAccess);

        // When
        var entries = combinedStorage
            .GetCollectedData(startTimeBeforeTodayInDifferentZone, endTimeBeforeTodayInDifferentZone, CancellationToken.None)
            .ToList();

        // Then
        Assert.IsNull(spyCollectedDataFileAccess.ReadRange);
        Assert.IsEmpty(entries);
    }

    [Test]
    public void DoesNotAccessFilesForDataStartingInFuture()
    {
        // Given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var futureStartTime = now + TimeSpan.FromDays(2);
        var futureEndTime = now + TimeSpan.FromDays(3);

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess();

        using var combinedStorage = MakeCombinedDataStorage(now, spyCollectedDataFileAccess);

        // When
        var entries = combinedStorage
            .GetCollectedData(futureStartTime, futureEndTime, CancellationToken.None)
            .ToList();

        // Then
        Assert.IsNull(spyCollectedDataFileAccess.ReadRange);

        Assert.IsEmpty(entries);
    }

    [Test]
    public void DoesNotAccessFilesForDataStartingInFutureWithSeeminglyPastTimeDueToNegativeOffset()
    {
        // Given
        var oneHourBeforeMidnight = new DateTimeOffset(2018, 12, 30, 23, 0, 0, TimeSpan.Zero);

        var fiveHoursOffset = TimeSpan.FromHours(-5);
        var futureStartInNegativeOffset = oneHourBeforeMidnight.ToOffset(fiveHoursOffset) + TimeSpan.FromHours(2);
        var futureEndTimeInNegativeOffset = futureStartInNegativeOffset + TimeSpan.FromMinutes(30);

        var entryAtCurrentTime = MakeMeasurementEntry(time: oneHourBeforeMidnight);

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess { Entries = [entryAtCurrentTime] };

        using var combinedStorage = MakeCombinedDataStorage(oneHourBeforeMidnight, spyCollectedDataFileAccess);

        // When
        var entries = combinedStorage
            .GetCollectedData(futureStartInNegativeOffset, futureEndTimeInNegativeOffset, CancellationToken.None)
            .ToList();

        // Then
        Assert.IsNull(spyCollectedDataFileAccess.ReadRange);

        Assert.IsEmpty(entries);
    }

    [Test]
    public async Task RetrievedDataOnlyContainsYesterdayOnceEvenIfNoOtherMeasurementWasSubsequentlyAdded()
    {
        // Given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var startTimeOnYesterday = now - TimeSpan.FromDays(1);
        var endTimeOnToday = now - TimeSpan.FromMinutes(30);

        var entryOnYesterday = MakeMeasurementEntry(time: startTimeOnYesterday + TimeSpan.FromMinutes(30));

        var stubCollectedDataFileAccess = new StubMeasurementsFileAccess { Entries = [entryOnYesterday] };

        using var combinedStorage = MakeCombinedDataStorage(now, stubCollectedDataFileAccess);

        await combinedStorage.StoreAsync(entryOnYesterday, CancellationToken.None);

        // When
        var entries = combinedStorage.GetCollectedData(startTimeOnYesterday, endTimeOnToday, CancellationToken.None);

        // Then
        CollectionAssert.AreEquivalent(new[] { entryOnYesterday }, entries);
    }

    [Test]
    public void ThrowsForDateRangeWhereStartIsAfterEnd()
    {
        // Given
        var startTime = new DateTimeOffset(2018, 12, 30, 11, 30, 0, 0, TimeSpan.Zero);
        var endTime = startTime - TimeSpan.FromHours(1);

        using var combinedStorage = MakeCombinedDataStorage();

        // When
        void RetrieveDataForInvalidTimeRange() => combinedStorage.GetCollectedData(startTime, endTime, CancellationToken.None);

        // Then
        _ = Assert.Catch(RetrieveDataForInvalidTimeRange);
    }

    private static CombinedFileAndMemoryMeasurementsStorage MakeCombinedDataStorage(DateTimeOffset? now = null, IMeasurementsFileAccess? fileAccess = null)
    {
        return new(
            new FixedClock { Now = now ?? DateTimeOffset.UnixEpoch },
            fileAccess ?? new DummyMeasurementsFileAccess());
    }

    private static MeasurementEntry MakeMeasurementEntry(DateTimeOffset time)
    {
        return MeasurementEntryFactory.Make(time, temperature: null, humidity: null, pressure: null);
    }

    private class SpyMeasurementsFileAccess : IMeasurementsFileAccess
    {
        public MeasurementEntry? SavedEntry { get; private set; }
        public (DateTimeOffset start, DateTimeOffset end)? ReadRange { get; private set; }

        public IEnumerable<MeasurementEntry> Entries { get; init; } = [];

        public IEnumerable<MeasurementEntry> Read(DateTimeOffset start, DateTimeOffset end, CancellationToken token)
        {
            ReadRange = (start, end);

            return Entries;
        }

        public Task SaveAsync(MeasurementEntry entry, CancellationToken token)
        {
            SavedEntry = entry;

            return Task.CompletedTask;
        }

        public void Reset()
        {
            SavedEntry = null;
            ReadRange = null;
        }
    }

    private class DummyMeasurementsFileAccess : IMeasurementsFileAccess
    {
        public IEnumerable<MeasurementEntry> Read(DateTimeOffset start, DateTimeOffset end, CancellationToken token)
        {
            yield break;
        }

        public Task SaveAsync(MeasurementEntry entry, CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }

    private class StubMeasurementsFileAccess : IMeasurementsFileAccess
    {
        public IEnumerable<MeasurementEntry> Entries { get; init; } = [];

        public IEnumerable<MeasurementEntry> Read(DateTimeOffset start, DateTimeOffset end, CancellationToken token)
        {
            return Entries;
        }

        public Task SaveAsync(MeasurementEntry entry, CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}
