using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rcm.DataCollection.Files;

namespace Rcm.DataCollection.UnitTests.Files
{
    [TestFixture]
    public class CollectedDataFilesNavigatorTests
    {
        [Test]
        public void EntryFilePathIsDataStoragePathCombinedWithMeasurementsAndEntryDateWithMstExtension()
        {
            // given
            var dataStorageLocation = new StubDataStorageLocation("dataStorage");

            var navigator = new CollectedDataFilesNavigator(dataStorageLocation);

            var entryTime = new DateTimeOffset(2018, 12, 30, 19, 30, 15, TimeSpan.FromHours(1));

            // when
            var path = navigator.GetFilePath(entryTime);

            // then
            var separator = Path.DirectorySeparatorChar;
            var expectedPath = $"dataStorage{separator}measurements{separator}2018-12-30.mst";
            Assert.AreEqual(expectedPath, path);
        }

        [Test]
        public void FilePathsOfEntriesWithinRangeAreDatesWithinThatRangeCombinedWithDataStoragePathAndMeasurementsAndMstExtension()
        {
            // given
            var dataStorageLocation = new StubDataStorageLocation("dataStorage");

            var navigator = new CollectedDataFilesNavigator(dataStorageLocation);

            var startTime = new DateTimeOffset(2018, 12, 20, 19, 0, 0, TimeSpan.FromHours(1));
            var endTime = new DateTimeOffset(2018, 12, 22, 15, 0, 0, TimeSpan.FromHours(1));

            var daysBetweenStartAndEnd = new[]
            {
                new DateTime(2018, 12, 20, 0, 0, 0),
                new DateTime(2018, 12, 21, 0, 0, 0),
                new DateTime(2018, 12, 22, 0, 0, 0)
            };

            // when
            var paths = navigator.GetFilePaths(startTime, endTime);

            // then
            var separator = Path.DirectorySeparatorChar;
            CollectionAssert.AreEquivalent(
                daysBetweenStartAndEnd.Select(d => (d.Date, $"dataStorage{separator}measurements{separator}2018-12-{d.Day:00}.mst")),
                paths);
        }

        private class StubDataStorageLocation : IDataStorageLocation
        {
            public string Path { get; }

            public StubDataStorageLocation(string path)
            {
                Path = path;
            }
        }
    }
}
