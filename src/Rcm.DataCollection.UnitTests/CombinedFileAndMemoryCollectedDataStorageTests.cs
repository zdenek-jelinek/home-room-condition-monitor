using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rcm.Common;
using Rcm.DataCollection.Files;
using Rcm.TestDoubles.Common;

namespace Rcm.DataCollection.UnitTests
{
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

            var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
                new DummyLogger<CombinedFileAndMemoryCollectedDataStorage>(),
                dummyClock,
                spyCollectedDataFileAccess);

            // when
            await combinedStorage.StoreAsync(entry);

            // then
            Assert.IsNotNull(spyCollectedDataFileAccess.SavedEntry);

#pragma warning disable CS8602 // Possible dereference of a null reference.
            Assert.AreEqual(entry.Time, spyCollectedDataFileAccess.SavedEntry.Time);
            Assert.AreEqual(entry.CelsiusTemperature, spyCollectedDataFileAccess.SavedEntry.CelsiusTemperature);
            Assert.AreEqual(entry.RelativeHumidity, spyCollectedDataFileAccess.SavedEntry.RelativeHumidity);
            Assert.AreEqual(entry.HpaPressure, spyCollectedDataFileAccess.SavedEntry.HpaPressure);
#pragma warning restore CS8602 // Possible dereference of a null reference.
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

            var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
                new DummyLogger<CombinedFileAndMemoryCollectedDataStorage>(),
                clock,
                spyCollectedDataFileAccess);

            await combinedStorage.StoreAsync(todaysEntry);

            // when
            var entries = combinedStorage.GetCollectedDataAsync(startTimeBeforeToday, endTimeInFuture).ToList();

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

            var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
                new DummyLogger<CombinedFileAndMemoryCollectedDataStorage>(),
                clock,
                spyCollectedDataFileAccess);

            // when
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            combinedStorage.GetCollectedDataAsync(startTimeBeforeToday, endTimeBeforeToday).ToList();

            // then
            Assert.IsNotNull(spyCollectedDataFileAccess.ReadRange);

            var (readStart, readEnd) = spyCollectedDataFileAccess.ReadRange.GetValueOrDefault();
            Assert.AreEqual(startTimeBeforeToday, readStart);
            Assert.AreEqual(endTimeBeforeToday, readEnd);
        }

        [Test]
        public async Task DoesNotAccessFilesToReadTodaysData()
        {
            // given
            var now = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.Zero);

            var startTimeOnToday = now.AddMinutes(-30);
            var endTimeOnToday = now.AddMinutes(-10);

            var clock = new FixedClock(now);

            var spyCollectedDataFileAccess = new SpyCollectedDataFileAccess();

            var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
                new DummyLogger<CombinedFileAndMemoryCollectedDataStorage>(),
                clock,
                spyCollectedDataFileAccess);

            var storedEntry = new MeasurementEntry(now.AddMinutes(-20), 25m, 45m, 1050m);

            await combinedStorage.StoreAsync(storedEntry);

            // when
            var entries = combinedStorage.GetCollectedDataAsync(startTimeOnToday, endTimeOnToday).ToList();

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

            var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
                new DummyLogger<CombinedFileAndMemoryCollectedDataStorage>(),
                clock,
                spyCollectedDataFileAccess);

            await combinedStorage.StoreAsync(todaysEntry);

            // when
            var entries = combinedStorage.GetCollectedDataAsync(startTimeBeforeToday, endTimeOnToday).ToList();

            // then
            Assert.IsNotNull(spyCollectedDataFileAccess.ReadRange);

            var (fileReadStart, fileReadEnd) = spyCollectedDataFileAccess.ReadRange.GetValueOrDefault();
            Assert.AreEqual(startTimeBeforeToday, fileReadStart);
            Assert.AreEqual(todayMidnight.AddSeconds(-1), fileReadEnd);

            CollectionAssert.AreEquivalent(new[] { olderEntry, todaysEntry }, entries);
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

            var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
                new DummyLogger<CombinedFileAndMemoryCollectedDataStorage>(),
                clock,
                spyCollectedDataFileAccess);

            await combinedStorage.StoreAsync(entryBeforeStart);
            await combinedStorage.StoreAsync(entryOnStart);
            await combinedStorage.StoreAsync(entryInsideRange);
            await combinedStorage.StoreAsync(entryOnEnd);
            await combinedStorage.StoreAsync(entryAfterEnd);

            // when
            var entries = combinedStorage.GetCollectedDataAsync(startTimeOnToday, endTimeOnToday).ToList();

            // then
            CollectionAssert.AreEquivalent(new[] { entryOnStart, entryInsideRange, entryOnEnd }, entries);
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

            var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
                new DummyLogger<CombinedFileAndMemoryCollectedDataStorage>(),
                clock,
                spyCollectedDataFileAccess);

            // when
            var entries = combinedStorage.GetCollectedDataAsync(futureStartTime, futureEndTime).ToList();

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

            var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
                new DummyLogger<CombinedFileAndMemoryCollectedDataStorage>(),
                fixedClock,
                stubCollectedDataFileAccess);
            
            await combinedStorage.StoreAsync(entryOnYesterday);

            // when
            var entries = combinedStorage.GetCollectedDataAsync(startTimeOnYesterday, endTimeOnToday);

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

            var combinedStorage = new CombinedFileAndMemoryCollectedDataStorage(
                new DummyLogger<CombinedFileAndMemoryCollectedDataStorage>(),
                dummyClock,
                new DummyCollectedDataFileAccess());

            // when
            void RetrieveDataForInvalidTimeRange()
            {
                combinedStorage.GetCollectedDataAsync(startTime, endTime);
            }

            // then
            Assert.Catch(RetrieveDataForInvalidTimeRange);
        }

        public class SpyCollectedDataFileAccess : ICollectedDataFileAccess
        {
            // TODO: Make nullable
            public MeasurementEntry SavedEntry { get; private set; }
            public (DateTimeOffset start, DateTimeOffset end)? ReadRange { get; private set; }

            public IEnumerable<MeasurementEntry> Entries { get; set; } = Enumerable.Empty<MeasurementEntry>();

            public IEnumerable<MeasurementEntry> Read(DateTimeOffset start, DateTimeOffset end)
            {
                ReadRange = (start, end);

                return Entries;
            }

            public Task SaveAsync(MeasurementEntry entry)
            {
                SavedEntry = entry;

                return Task.CompletedTask;
            }
        }

        public class DummyCollectedDataFileAccess : ICollectedDataFileAccess
        {
            public IEnumerable<MeasurementEntry> Read(DateTimeOffset start, DateTimeOffset end)
            {
                yield break;
            }

            public Task SaveAsync(MeasurementEntry entry)
            {
                return Task.CompletedTask;
            }
        }

        private class StubCollectedDataFileAccess : ICollectedDataFileAccess
        {
            public IEnumerable<MeasurementEntry> Entries { get; set; } = Enumerable.Empty<MeasurementEntry>();

            public IEnumerable<MeasurementEntry> Read(DateTimeOffset start, DateTimeOffset end)
            {
                return Entries;
            }

            public Task SaveAsync(MeasurementEntry entry)
            {
                return Task.CompletedTask;
            }
        }
    }
}