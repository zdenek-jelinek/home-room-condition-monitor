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
        var entry = new MeasurementEntry(
            time: new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero),
            celsiusTemperature: 10m,
            relativeHumidity: 45m,
            hpaPressure: 980m);

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

        var startTimeBeforeToday = now.AddDays(-2);
        var endTimeInFuture = now.AddDays(2);

        var pastEntry = new MeasurementEntry(startTimeBeforeToday.AddMinutes(10), celsiusTemperature: 15m, relativeHumidity: 40m, hpaPressure: 980m);
        var todaysEntry = new MeasurementEntry(now.AddMinutes(-20), celsiusTemperature: 25m, relativeHumidity: 45m, hpaPressure: 1050m);

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

        var startTimeBeforeToday = now.AddDays(-2);
        var endTimeBeforeToday = now.AddDays(-1);

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

        var startTimeBeforeMidnight = todayMidnight.AddMinutes(-30).ToOffset(TimeSpan.FromHours(offset));
        var endTimeBeforeMidnight = startTimeBeforeMidnight.AddMinutes(10);

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

        var startTimeOnToday = now.AddMinutes(-30);
        var endTimeOnToday = now.AddMinutes(-10);

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess();

        using var combinedStorage = MakeCombinedDataStorage(now, spyCollectedDataFileAccess);

        var storedEntry = new MeasurementEntry(now.AddMinutes(-20), 25m, 45m, 1050m);

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

        var startTimeBeforeToday = now.AddDays(-2);
        var endTimeOnToday = now.AddMinutes(-10);

        var olderEntry = new MeasurementEntry(startTimeBeforeToday.AddMinutes(10), 15m, 40m, 980m);
        var todaysEntry = new MeasurementEntry(now.AddMinutes(-20), 25m, 45m, 1050m);

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

        var startTimeOnToday = now.AddHours(-1);
        var endTimeOnToday = now.AddHours(1);

        var entryStoredInFile = new MeasurementEntry(now, 20m, 40m, 970m);

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

        var startTimeOnToday = now.AddHours(-10);
        var endTimeOnToday = now.AddHours(10);

        var entryPreviouslyStoredInFile = new MeasurementEntry(now.AddHours(-2), 20m, 40m, 970m);

        var spyCollectedDataFileAccess = new SpyMeasurementsFileAccess { Entries = [entryPreviouslyStoredInFile] };

        using var combinedStorage = MakeCombinedDataStorage(now, spyCollectedDataFileAccess);

        var newEntry = new MeasurementEntry(now, 25m, 32m, 985m);

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

        var startTimeOnToday = now.AddMinutes(-20);
        var endTimeOnToday = now.AddMinutes(-10);

        var entryBeforeStart = new MeasurementEntry(startTimeOnToday.AddMinutes(-10), 15m, 40m, 980m);
        var entryOnStart = new MeasurementEntry(startTimeOnToday, 20m, 47m, 990m);
        var entryInsideRange = new MeasurementEntry(startTimeOnToday.AddMinutes(5), 25m, 45m, 1050m);
        var entryOnEnd = new MeasurementEntry(endTimeOnToday, 30m, 42m, 1030m);
        var entryAfterEnd = new MeasurementEntry(endTimeOnToday.AddMinutes(5), 28m, 50m, 995m);

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
        var endTimeAfterMidnightInDifferentOffset = startTimeOnMidnightInDifferentOffset.AddMinutes(30);

        var entryPreviouslyStoredInFile = new MeasurementEntry(todayMidnight.AddMinutes(10), 20m, 40m, 970m);

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

        var startTimeBeforeNextMidnightInDifferentOffset = hourBeforeMidnight.AddMinutes(-30).ToOffset(TimeSpan.FromHours(offset));
        var endTimeBeforeNextMidnightInDifferentOffset = startTimeBeforeNextMidnightInDifferentOffset.AddMinutes(30);

        var entryPreviouslyStoredInFile = new MeasurementEntry(hourBeforeMidnight.AddMinutes(-10), 20m, 40m, 970m);

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
        var startTimeBeforeTodayInDifferentZone = timeEquivalentToUtcMidnightInMinusFive.Add(startSkew);
        var endTimeBeforeTodayInDifferentZone = startTimeBeforeTodayInDifferentZone.Add(rangeSize);

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

        var futureStartTime = now.AddDays(2);
        var futureEndTime = now.AddDays(3);

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
        var futureStartInNegativeOffset = oneHourBeforeMidnight.ToOffset(fiveHoursOffset).AddHours(2);
        var futureEndTimeInNegativeOffset = futureStartInNegativeOffset.AddMinutes(30);

        var entryAtCurrentTime = new MeasurementEntry(oneHourBeforeMidnight, 20m, 40m, 970m);

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

        var startTimeOnYesterday = now.AddDays(-1);
        var endTimeOnToday = now.AddMinutes(-30);

        var entryOnYesterday = new MeasurementEntry(startTimeOnYesterday.AddMinutes(30), 10m, 45m, 980m);

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
