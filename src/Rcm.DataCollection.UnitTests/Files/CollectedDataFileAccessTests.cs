using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rcm.Common;
using Rcm.Common.IO;
using Rcm.Common.Tasks;
using Rcm.DataCollection.Files;
using Rcm.Device.Common;
using Rcm.TestDoubles.Common;
using Rcm.TestDoubles.Common.IO;
using static System.Globalization.CultureInfo;

namespace Rcm.DataCollection.UnitTests.Files
{
    [TestFixture]
    public class CollectedDataFileAccessTests
    {
        private const string DataPath = "data";
        private static readonly string StoragePath = Path.Combine(DataPath, "measurements");

        [Test]
        public async Task WritesDataToDataStorageLocationUnderFilenameMatchingEntryDate()
        {
            // given
            var fakeFileAccess = new FakeFileAccess();

            var collectedDataFileAccess = CreateCollectedDataFileAccess(fakeFileAccess);

            var firstEntry = new MeasurementEntry(
                new DateTimeOffset(2018, 12, 30, 15, 10, 30, TimeSpan.FromHours(2)),
                30m,
                41.2m,
                985.47m);

            var secondEntry = new MeasurementEntry(
                new DateTimeOffset(2018, 12, 31, 10, 45, 15, TimeSpan.FromHours(1)),
                33m,
                47.1m,
                994.36m);

            // when
            await collectedDataFileAccess.SaveAsync(firstEntry, default);
            await collectedDataFileAccess.SaveAsync(secondEntry, default);

            // then
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
            // given
            var fakeFileAccess = new FakeFileAccess();

            var collectedDataFileAccess = CreateCollectedDataFileAccess(fakeFileAccess);

            var startTime = new DateTimeOffset(2018, 12, 25, 15, 0, 0, TimeSpan.FromHours(1));
            var endTime = new DateTimeOffset(2018, 12, 30, 12, 0, 0, TimeSpan.FromHours(-1));

            var entryDayBeforeStart = new MeasurementEntry(startTime.AddDays(-1), 28m, 47.6m, 974.59m);
            var entryHourBeforeStart = new MeasurementEntry(startTime.AddHours(-1), 29m, 42.6m, 987m);
            var entryOnStart = new MeasurementEntry(startTime, 30m, 41.2m, 985.47m);
            var firstEntryInMiddle = new MeasurementEntry(startTime.AddDays(2).AddHours(10), 28m, 39.7m, 994.12m);
            var secondEntryInMiddle = new MeasurementEntry(startTime.AddDays(2).AddHours(11), 27m, 39.8m, 993.68m);
            var entryOnEnd = new MeasurementEntry(endTime, 22m, 43m, 1000m);
            var entryHourAfterEnd = new MeasurementEntry(endTime.AddHours(1), 23m, 41m, 998m);
            var entryDayAfterEnd = new MeasurementEntry(endTime.AddDays(1), 27m, 48m, 1014.3m);

            StoreEntriesToFiles(
                fakeFileAccess,
                new[]
                {
                    entryDayBeforeStart,
                    entryHourBeforeStart,
                    entryOnStart,
                    firstEntryInMiddle,
                    secondEntryInMiddle,
                    entryOnEnd,
                    entryHourAfterEnd,
                    entryDayAfterEnd
                });

            // when
            var readEntries = collectedDataFileAccess.Read(startTime, endTime, default);

            // then
            Assert.That(
                readEntries,
                Is.EquivalentTo(new[] { entryOnStart, firstEntryInMiddle, secondEntryInMiddle, entryOnEnd })
                    .Using(new MeasurementEntryEqualityComparer()));
        }

        [Test]
        public void SkipsInvalidLinesInFiles()
        {
            // given
            var time = new DateTimeOffset(2020, 1, 28, 18, 45, 0, TimeSpan.FromHours(1));

            var fakeFileAccess = new FakeFileAccess();

            var collectedDataFileAccess = CreateCollectedDataFileAccess(fakeFileAccess);

            var validEntries = new[]
            {
                new MeasurementEntry(time, 28m, 47.6m, 974.59m),
                new MeasurementEntry(time.AddHours(1), 29m, 42.6m, 987m)
            };

            var measurementFileLines = new[]
            {
                GetEntryRecord(validEntries[0]),
                "Invalid line",
                GetEntryRecord(validEntries[1])
            };

            var path = GetEntryFilePath(time);
            fakeFileAccess.WriteAllLines(path, measurementFileLines);

            // when
            var readEntries = collectedDataFileAccess.Read(time.AddHours(-2), time.AddHours(2), default);

            // then
            Assert.That(readEntries, Is.EquivalentTo(validEntries).Using(new MeasurementEntryEqualityComparer()));
        }

        [Test]
        public async Task AbortsWriteIfCancellationIsSignaledBeforeOrDuringFileOpening()
        {
            // given
            using var cancellationTokenSource = new CancellationTokenSource();

            var dummyEntry = new MeasurementEntry(new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), 25m, 42m, 1010m);

            using var blockingFileAccess = new BlockingFileAccess();

            var collectedDataFileAccess = CreateCollectedDataFileAccess(blockingFileAccess);

            // when
            var savingTask = Task.Run(() => collectedDataFileAccess.SaveAsync(dummyEntry, cancellationTokenSource.Token));
            
            await blockingFileAccess.OpeningStarted;
            cancellationTokenSource.Cancel();
            blockingFileAccess.Release();

            var savingCompleted = await savingTask.TryWait(TimeSpan.FromSeconds(1));

            // then
            Assert.IsTrue(savingCompleted, nameof(savingCompleted));
            Assert.AreEqual(TaskStatus.Canceled, savingTask.Status);
        }

        [Test]
        public async Task AbortsReadWhenCancelledBetweenReadingFiles()
        {
            // given
            using var cancellationTokenSource = new CancellationTokenSource();

            var dummyStart = new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));
            var dummyEnd = new DateTimeOffset(2000, 1, 10, 12, 0, 0, TimeSpan.FromHours(2));

            using var blockingFileAccess = new BlockingFileAccess();

            var collectedDataFileAccess = CreateCollectedDataFileAccess(blockingFileAccess);

            CreateDummyMeasurementFiles(blockingFileAccess.UnderlyingFileAcces, dummyStart, dummyEnd);

            // when
            var readIterator = collectedDataFileAccess.Read(dummyStart, dummyEnd, cancellationTokenSource.Token);
            var readingTask = Task.Run(() => readIterator.ToList(), cancellationTokenSource.Token);

            await blockingFileAccess.OpeningStarted;
            cancellationTokenSource.Cancel();
            blockingFileAccess.Release();

            var readingCompleted = await readingTask.TryWait(TimeSpan.FromSeconds(1));

            // then
            Assert.IsTrue(readingCompleted, nameof(readingCompleted));
            Assert.AreEqual(TaskStatus.Canceled, readingTask.Status);
        }

        private static CollectedDataFileAccess CreateCollectedDataFileAccess(
            IFileAccess fileAccess,
            IDataStorageLocation? dataStorageLocation = null)
        {
            return new CollectedDataFileAccess(
                new DummyLogger<CollectedDataFileAccess>(),
                dataStorageLocation ?? new DataStorageLocation(DataPath),
                fileAccess);
        }

        private static void CreateDummyMeasurementFiles(IFileAccess file, DateTimeOffset start, DateTimeOffset end)
        {
            for (var day = start; day <= end; day = day.AddDays(1))
            {
                var filePath = GetEntryFilePath(day);
                var dummyEntry = new MeasurementEntry(day, 25m, 85m, 1010m);

                using (var measurementFile = file.AppendText(filePath))
                {
                    measurementFile.WriteLine(GetEntryRecord(dummyEntry));
                }
            }
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

        private class DataStorageLocation : IDataStorageLocation
        {
            public string Path { get; }

            public DataStorageLocation(string path)
            {
                Path = path;
            }
        }

        private sealed class MeasurementEntryEqualityComparer : IEqualityComparer<MeasurementEntry>
        {
            public bool Equals(MeasurementEntry x, MeasurementEntry y)
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
            private readonly SemaphoreSlim _openingSemaphore = new SemaphoreSlim(0);
            private readonly SemaphoreSlim _blockingSemaphore = new SemaphoreSlim(0);

            public IFileAccess UnderlyingFileAcces { get; set; } = new FakeFileAccess();

            public Task OpeningStarted => _openingSemaphore.WaitAsync();

            public bool Exists(string path) => UnderlyingFileAcces.Exists(path);

            public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
            {
                _openingSemaphore.Release();
                _blockingSemaphore.Wait();

                return UnderlyingFileAcces.Open(path, mode, access, share);
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
}