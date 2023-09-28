using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rcm.Common;
using Rcm.Common.TestDoubles;
using Rcm.Device.DataCollection.Files;

namespace Rcm.Device.DataCollection.UnitTests;

[TestFixture]
public class CombinedFileAndMemoryCollectedDataStorageTests
{
    [Test]
    public async Task StoresAllMeasurementsToFile()
    {
        // given
        var entry = new MeasurementEntry(
            new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero),
            10m,
            45m,
            980m);

        var dummyClock = new Clock();

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess();

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            dummyClock,
            spyCollectedDataFileAccess);

        // when
        await combinedStorage.StoreAsync(entry, default);

        // then
        Assert.IsNotNull(spyCollectedDataFileAccess.SavedEntry);

        Assert.AreEqual(entry.Time, spyCollectedDataFileAccess.SavedEntry!.Time);
        Assert.AreEqual(entry.CelsiusTemperature, spyCollectedDataFileAccess.SavedEntry.CelsiusTemperature);
        Assert.AreEqual(entry.RelativeHumidity, spyCollectedDataFileAccess.SavedEntry.RelativeHumidity);
        Assert.AreEqual(entry.HpaPressure, spyCollectedDataFileAccess.SavedEntry.HpaPressure);
    }

    [Test]
    public async Task AccessFilesForDataInThePastReadsTodayDataFromMemoryAndDoesNotReadDataFromFuture()
    {
        // given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);
        var todayMidnight = new DateTimeOffset(now.Date, now.Offset);

        var startTimeBeforeToday = now.AddDays(-2);
        var endTimeInFuture = now.AddDays(2);
            
        var pastEntry = new MeasurementEntry(startTimeBeforeToday.AddMinutes(10), 15m, 40m, 980m);
        var todaysEntry = new MeasurementEntry(now.AddMinutes(-20), 25m, 45m, 1050m);

        var clock = new FixedClock(now);

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess { Entries = new[] { pastEntry } };

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            clock,
            spyCollectedDataFileAccess);

        await combinedStorage.StoreAsync(todaysEntry, default);

        // when
        var entries = combinedStorage
            .GetCollectedData(startTimeBeforeToday, endTimeInFuture, default)
            .ToList();

        // then
        Assert.IsNotNull(spyCollectedDataFileAccess.ReadRange);

        var (fileReadStart, fileReadEnd) = spyCollectedDataFileAccess.ReadRange.GetValueOrDefault();
        Assert.AreEqual(startTimeBeforeToday, fileReadStart);
        Assert.AreEqual(todayMidnight.AddSeconds(-1), fileReadEnd);

        CollectionAssert.AreEquivalent(new[] { pastEntry, todaysEntry }, entries);
    }

    [Test]
    public void ReadsDataOlderThanTodayFromFiles()
    {
        // given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var startTimeBeforeToday = now.AddDays(-2);
        var endTimeBeforeToday = now.AddDays(-1);

        var clock = new FixedClock(now);

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess();

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            clock,
            spyCollectedDataFileAccess);

        // when
        _ = combinedStorage
            .GetCollectedData(startTimeBeforeToday, endTimeBeforeToday, default)
            .ToList();

        // then
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
        // given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);
        var todayMidnight = new DateTimeOffset(now.Date, now.Offset);

        var startTimeBeforeMidnight = todayMidnight.AddMinutes(-30).ToOffset(TimeSpan.FromHours(offset));
        var endTimeBeforeMidnight = startTimeBeforeMidnight.AddMinutes(10);

        var clock = new FixedClock(now);

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess();

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            clock,
            spyCollectedDataFileAccess);

        // when
        _ = combinedStorage
            .GetCollectedData(startTimeBeforeMidnight, endTimeBeforeMidnight, default)
            .ToList();

        // then
        Assert.IsNotNull(spyCollectedDataFileAccess.ReadRange);

        var (readStart, readEnd) = spyCollectedDataFileAccess.ReadRange.GetValueOrDefault();
        Assert.AreEqual(startTimeBeforeMidnight.ToUniversalTime(), readStart);
        Assert.AreEqual(endTimeBeforeMidnight.ToUniversalTime(), readEnd);
    }

    [Test]
    public async Task DoesNotAccessFilesToReadTodaysDataAfterPreviousOperation()
    {
        // given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var startTimeOnToday = now.AddMinutes(-30);
        var endTimeOnToday = now.AddMinutes(-10);

        var clock = new FixedClock(now);

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess();

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            clock,
            spyCollectedDataFileAccess);

        var storedEntry = new MeasurementEntry(now.AddMinutes(-20), 25m, 45m, 1050m);

        await combinedStorage.StoreAsync(storedEntry, default);
        spyCollectedDataFileAccess.Reset();

        // when
        var entries = combinedStorage
            .GetCollectedData(startTimeOnToday, endTimeOnToday, default)
            .ToList();

        // then
        Assert.IsNull(spyCollectedDataFileAccess.ReadRange);

        CollectionAssert.AreEquivalent(new[] { storedEntry }, entries);
    }

    [Test]
    public async Task AccessesFilesForOlderDaysAndNotForTodayForDateRangeIncludingToday()
    {
        // given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);
        var todayMidnight = new DateTimeOffset(now.Date, now.Offset);

        var startTimeBeforeToday = now.AddDays(-2);
        var endTimeOnToday = now.AddMinutes(-10);
            
        var olderEntry = new MeasurementEntry(startTimeBeforeToday.AddMinutes(10), 15m, 40m, 980m);
        var todaysEntry = new MeasurementEntry(now.AddMinutes(-20), 25m, 45m, 1050m);

        var clock = new FixedClock(now);

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess { Entries = new[] { olderEntry } };

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            clock,
            spyCollectedDataFileAccess);

        await combinedStorage.StoreAsync(todaysEntry, default);

        // when
        var entries = combinedStorage
            .GetCollectedData(startTimeBeforeToday, endTimeOnToday, default)
            .ToList();

        // then
        Assert.IsNotNull(spyCollectedDataFileAccess.ReadRange);

        var (fileReadStart, fileReadEnd) = spyCollectedDataFileAccess.ReadRange.GetValueOrDefault();
        Assert.AreEqual(startTimeBeforeToday, fileReadStart);
        Assert.AreEqual(todayMidnight.AddSeconds(-1), fileReadEnd);

        CollectionAssert.AreEquivalent(new[] { olderEntry, todaysEntry }, entries);
    }

    [Test]
    public void TodaysDataReadWithoutAnyPrecedingStoresIncludeDataStoredInFile()
    {
        // given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var startTimeOnToday = now.AddHours(-1);
        var endTimeOnToday = now.AddHours(1);

        var clock = new FixedClock(now);

        var entryStoredInFile = new MeasurementEntry(now, 20m, 40m, 970m);

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess { Entries = new[] { entryStoredInFile } };

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            clock,
            spyCollectedDataFileAccess);

        // when
        var readEntries = combinedStorage
            .GetCollectedData(startTimeOnToday, endTimeOnToday, default)
            .ToList();

        // then
        CollectionAssert.AreEquivalent(new[] { entryStoredInFile }, readEntries);
    }

    [Test]
    public async Task TodaysDataReadAfterStoreIncludeDataAlreadyStoredInFile()
    {
        // given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var startTimeOnToday = now.AddHours(-10);
        var endTimeOnToday = now.AddHours(10);

        var clock = new FixedClock(now);

        var entryPreviouslyStoredInFile = new MeasurementEntry(now.AddHours(-2), 20m, 40m, 970m);

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess { Entries = new[] { entryPreviouslyStoredInFile } };

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            clock,
            spyCollectedDataFileAccess);

        var newEntry = new MeasurementEntry(now, 25m, 32m, 985m);

        // when
        await combinedStorage.StoreAsync(newEntry, default);

        var readEntries = combinedStorage
            .GetCollectedData(startTimeOnToday, endTimeOnToday, default)
            .ToList();

        // then
        CollectionAssert.AreEquivalent(new[] { entryPreviouslyStoredInFile, newEntry }, readEntries);
    }

    [Test]
    public async Task ReturnsTodaysEntriesWithinRangeForTodayRange()
    {
        // given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var startTimeOnToday = now.AddMinutes(-20);
        var endTimeOnToday = now.AddMinutes(-10);
            
        var entryBeforeStart = new MeasurementEntry(startTimeOnToday.AddMinutes(-10), 15m, 40m, 980m);
        var entryOnStart = new MeasurementEntry(startTimeOnToday, 20m, 47m, 990m);
        var entryInsideRange = new MeasurementEntry(startTimeOnToday.AddMinutes(5), 25m, 45m, 1050m);
        var entryOnEnd = new MeasurementEntry(endTimeOnToday, 30m, 42m, 1030m);
        var entryAfterEnd = new MeasurementEntry(endTimeOnToday.AddMinutes(5), 28m, 50m, 995m);

        var clock = new FixedClock(now);

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess();

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            clock,
            spyCollectedDataFileAccess);

        await combinedStorage.StoreAsync(entryBeforeStart, default);
        await combinedStorage.StoreAsync(entryOnStart, default);
        await combinedStorage.StoreAsync(entryInsideRange, default);
        await combinedStorage.StoreAsync(entryOnEnd, default);
        await combinedStorage.StoreAsync(entryAfterEnd, default);

        // when
        var entries = combinedStorage
            .GetCollectedData(startTimeOnToday, endTimeOnToday, default)
            .ToList();

        // then
        CollectionAssert.AreEquivalent(new[] { entryOnStart, entryInsideRange, entryOnEnd }, entries);
    }

    [Test]
    [TestCase(-2)]
    [TestCase(2)]
    public void ReturnsTodaysEntriesForTimesAtOrAfterMidnightRegardlessOfOffset(int offset)
    {
        // given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);
        var todayMidnight = new DateTimeOffset(now.Date, now.Offset);

        var startTimeOnMidnightInDifferentOffset = todayMidnight.ToOffset(TimeSpan.FromHours(offset));
        var endTimeAfterMidnightInDifferentOffset = startTimeOnMidnightInDifferentOffset.AddMinutes(30);

        var clock = new FixedClock(now);

        var entryPreviouslyStoredInFile = new MeasurementEntry(todayMidnight.AddMinutes(10), 20m, 40m, 970m);

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess { Entries = new[] { entryPreviouslyStoredInFile } };

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            clock,
            spyCollectedDataFileAccess);

        // when
        var readEntries = combinedStorage
            .GetCollectedData(startTimeOnMidnightInDifferentOffset, endTimeAfterMidnightInDifferentOffset, default)
            .ToList();

        // then
        CollectionAssert.AreEquivalent(new[] { entryPreviouslyStoredInFile }, readEntries);
    }

    [Test]
    [TestCase(-2)]
    [TestCase(2)]
    public void ReturnsTodaysEntriesForTimesBeforeNextMidnightRegardlessOfOffset(int offset)
    {
        // given
        var hourBeforeMidnight = new DateTimeOffset(2018, 12, 30, 23, 0, 0, TimeSpan.Zero);

        var startTimeBeforeNextMidnightInDifferentOffset = hourBeforeMidnight.AddMinutes(-30).ToOffset(TimeSpan.FromHours(offset));
        var endTimeBeforeNextMidnightInDifferentOffset = startTimeBeforeNextMidnightInDifferentOffset.AddMinutes(30);

        var clock = new FixedClock(hourBeforeMidnight);

        var entryPreviouslyStoredInFile = new MeasurementEntry(hourBeforeMidnight.AddMinutes(-10), 20m, 40m, 970m);

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess { Entries = new[] { entryPreviouslyStoredInFile } };

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            clock,
            spyCollectedDataFileAccess);

        // when
        var readEntries = combinedStorage
            .GetCollectedData(startTimeBeforeNextMidnightInDifferentOffset, endTimeBeforeNextMidnightInDifferentOffset, default)
            .ToList();

        // then
        CollectionAssert.AreEquivalent(new[] { entryPreviouslyStoredInFile }, readEntries);
    }

    [Test]
    public void DoesNotAccessFilesForDataBeforeQueriedRangeDueToNegativeOffset()
    {
        // given
        var midnightInUtc = new DateTimeOffset(2018, 12, 30, 0, 0, 0, TimeSpan.Zero);
        var timeEquivalentToUtcMidnightInMinusFive = midnightInUtc.ToOffset(TimeSpan.FromHours(-5));

        var startSkew = TimeSpan.FromMinutes(10);
        var rangeSize = TimeSpan.FromHours(2);
        var startTimeBeforeTodayInDifferentZone = timeEquivalentToUtcMidnightInMinusFive.Add(startSkew);
        var endTimeBeforeTodayInDifferentZone = startTimeBeforeTodayInDifferentZone.Add(rangeSize);

        var clock = new FixedClock(midnightInUtc);

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess();

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            clock,
            spyCollectedDataFileAccess);

        // when
        var entries = combinedStorage
            .GetCollectedData(startTimeBeforeTodayInDifferentZone, endTimeBeforeTodayInDifferentZone, default)
            .ToList();

        // then
        Assert.IsNull(spyCollectedDataFileAccess.ReadRange);
        Assert.IsEmpty(entries);
    }

    [Test]
    public void DoesNotAccessFilesForDataStartingInFuture()
    {
        // given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var futureStartTime = now.AddDays(2);
        var futureEndTime = now.AddDays(3);

        var clock = new FixedClock(now);

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess();

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            clock,
            spyCollectedDataFileAccess);

        // when
        var entries = combinedStorage
            .GetCollectedData(futureStartTime, futureEndTime, default)
            .ToList();

        // then
        Assert.IsNull(spyCollectedDataFileAccess.ReadRange);

        Assert.IsEmpty(entries);
    }

    [Test]
    public void DoesNotAccessFilesForDataStartingInFutureWithSeeminglyPastTimeDueToNegativeOffset()
    {
        // given
        var oneHourBeforeMidnight = new DateTimeOffset(2018, 12, 30, 23, 0, 0, TimeSpan.Zero);

        var fiveHoursOffset = TimeSpan.FromHours(-5);
        var futureStartInNegativeOFfset = oneHourBeforeMidnight.ToOffset(fiveHoursOffset).AddHours(2);
        var futureEndTimeInNegativeOffset = futureStartInNegativeOFfset.AddMinutes(30);

        var clock = new FixedClock(oneHourBeforeMidnight);

        var entryAtCurrentTime = new MeasurementEntry(oneHourBeforeMidnight, 20m, 40m, 970m);

        var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess { Entries = new[] { entryAtCurrentTime } };

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            clock,
            spyCollectedDataFileAccess);

        // when
        var entries = combinedStorage
            .GetCollectedData(futureStartInNegativeOFfset, futureEndTimeInNegativeOffset, default)
            .ToList();

        // then
        Assert.IsNull(spyCollectedDataFileAccess.ReadRange);

        Assert.IsEmpty(entries);
    }

    [Test]
    public async Task RetrievedDataOnlyContainsYesterdayOnceEvenIfNoOtherMeasurementWasSubsequentlyAdded()
    {
        // given
        var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

        var startTimeOnYesterday = now.AddDays(-1);
        var endTimeOnToday = now.AddMinutes(-30);

        var fixedClock = new FixedClock(now);

        var entryOnYesterday = new MeasurementEntry(startTimeOnYesterday.AddMinutes(30), 10m, 45m, 980m);

        var stubCollectedDataFileAccess = new StubCollectedDataFileAccess { Entries = new[] { entryOnYesterday } };

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            fixedClock,
            stubCollectedDataFileAccess);
            
        await combinedStorage.StoreAsync(entryOnYesterday, default);

        // when
        var entries = combinedStorage.GetCollectedData(startTimeOnYesterday, endTimeOnToday, default);

        // then
        CollectionAssert.AreEquivalent(new[] { entryOnYesterday }, entries);
    }

    [Test]
    public void ThrowsForDateRangeWhereStartIsAfterEnd()
    {
        // given
        var startTime = new DateTimeOffset(2018, 12, 30, 11, 30, 0, 0, TimeSpan.Zero);
        var endTime = startTime - TimeSpan.FromHours(1);

        var dummyClock = new Clock();

        using var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
            dummyClock,
            new DummyCollectedDataFileAccess());

        // when
        void RetrieveDataForInvalidTimeRange() => combinedStorage.GetCollectedData(startTime, endTime, default);

        // then
        _ = Assert.Catch(RetrieveDataForInvalidTimeRange);
    }

    public class SpyCollectedDataFileAccess : ICollectedDataFileAccess
    {
        public MeasurementEntry? SavedEntry { get; private set; }
        public (DateTimeOffset start, DateTimeOffset end)? ReadRange { get; private set; }

        public IEnumerable<MeasurementEntry> Entries { get; set; } = Enumerable.Empty<MeasurementEntry>();

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

    public class DummyCollectedDataFileAccess : ICollectedDataFileAccess
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

    private class StubCollectedDataFileAccess : ICollectedDataFileAccess
    {
        public IEnumerable<MeasurementEntry> Entries { get; set; } = Enumerable.Empty<MeasurementEntry>();

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