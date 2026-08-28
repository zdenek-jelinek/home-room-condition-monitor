using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rcm.Persistence.Files.Navigation;

namespace Rcm.UnitTests.Persistence.Files;

[TestFixture]
public class MeasurementsFilesNavigatorTests
{
    [Test]
    public void EntryFilePathIsDataStoragePathCombinedWithMeasurementsAndEntryDateWithMstExtension()
    {
        // Given
        var dataStorageLocation = new StubDataStorageLocation("dataStorage");

        var navigator = new MeasurementsFilesNavigator(dataStorageLocation);

        var entryTime = new DateTimeOffset(2018, 12, 30, 19, 30, 15, TimeSpan.FromHours(1));

        // When
        var path = navigator.GetFilePath(entryTime);

        // Then
        var separator = Path.DirectorySeparatorChar;
        var expectedPath = $"dataStorage{separator}measurements{separator}2018-12-30.mst";
        Assert.AreEqual(expectedPath, path);
    }

    [Test]
    public void FilePathsOfEntriesWithinRangeAreDatesWithinThatRangeCombinedWithDataStoragePathAndMeasurementsAndMstExtension()
    {
        // Given
        var dataStorageLocation = new StubDataStorageLocation("dataStorage");

        var navigator = new MeasurementsFilesNavigator(dataStorageLocation);

        var startTime = new DateTimeOffset(2018, 12, 20, 19, 0, 0, TimeSpan.FromHours(1));
        var endTime = new DateTimeOffset(2018, 12, 22, 15, 0, 0, TimeSpan.FromHours(1));

        var datesBetweenStartAndEnd = new[]
        {
            new DateOnly(2018, 12, 20),
            new DateOnly(2018, 12, 21),
            new DateOnly(2018, 12, 22)
        };

        // When
        var paths = navigator.GetFilePaths(startTime, endTime);

        // Then
        var separator = Path.DirectorySeparatorChar;
        CollectionAssert.AreEquivalent(
            datesBetweenStartAndEnd.Select(date => (date, $"dataStorage{separator}measurements{separator}{date:yyyy'-'MM'-'dd}.mst")),
            paths);
    }

    private class StubDataStorageLocation(string path) : IDataStorageLocation
    {
        public string GetDirectoryPath() => path;
    }
}
