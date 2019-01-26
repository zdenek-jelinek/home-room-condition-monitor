using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rcm.Common;
using Rcm.Common.IO;
using Rcm.DataCollection.Files;
using Rcm.TestDoubles.Common;
using Rcm.TestDoubles.IO;

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
            var dataStorageLocation = new DataStorageLocation(DataPath);

            var fakeFileAccess = new FakeFileAccess();

            var collectedDataFileAccess = new CollectedDataFileAccess(
                new DummyLogger<CollectedDataFileAccess>(),
                dataStorageLocation,
                fakeFileAccess);

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
            await collectedDataFileAccess.SaveAsync(firstEntry);
            await collectedDataFileAccess.SaveAsync(secondEntry);

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
            var dataStorageLocation = new DataStorageLocation(DataPath);

            var fakeFileAccess = new FakeFileAccess();

            var collectedDataFileAccess = new CollectedDataFileAccess(
                new DummyLogger<CollectedDataFileAccess>(),
                dataStorageLocation,
                fakeFileAccess);

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
            var readEntries = collectedDataFileAccess.Read(startTime, endTime);

            // then
            Assert.That(
                readEntries,
                Is.EquivalentTo(new[] { entryOnStart, firstEntryInMiddle, secondEntryInMiddle, entryOnEnd })
                    .Using(new MeasurementEntryEqualityComparer()));
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
            return $"{entry.Time:HH':'mmK} {entry.CelsiusTemperature} {entry.RelativeHumidity} {entry.HpaPressure}";
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
                unchecked
                {
                    var hashCode = obj.Time.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.CelsiusTemperature.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.RelativeHumidity.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.HpaPressure.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}