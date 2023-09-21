using System.IO;
using NUnit.Framework;
using Rcm.Common.TestFramework.IO;
using Rcm.Device.Common;
using Rcm.Device.Connector.Configuration;

namespace Rcm.Device.Connector.Tests.Configuration;

[TestFixture]
public class FileBackendStorageLocationIntegrationTests
{
    private static string DataStorageDirectoryPath => Path.GetFullPath("data");

    private static TestDirectory DataStorageDirectory { get; } = new TestDirectory(DataStorageDirectoryPath);

    [SetUp]
    public void PrepareCleanDataStorageDirectory()
    {
        DataStorageDirectory.PrepareClean();
    }

    [TearDown]
    public void DeleteDataStorageDirectory()
    {
        DataStorageDirectory.Delete();
    }

    [Test]
    public void FileBackendStorageLocationPathIsEqualToDataStorageLocationPathPlusBackend()
    {
        // given
        var dataStorageLocation = new StubDataStorageLocation { Path = DataStorageDirectoryPath };

        var backendStorageLocation = new FileBackendStorageLocation(dataStorageLocation);

        // when
        var backendStorageDirectoryPath = backendStorageLocation.GetDirectoryPath();

        // then
        Assert.AreEqual(Path.Combine(DataStorageDirectoryPath, "backend"), backendStorageDirectoryPath);
    }

    [Test]
    public void EnsuresBackendDataStorageDirectoryExistsWhenQueryingItsPath()
    {
        // given
        var dataStorageLocation = new StubDataStorageLocation { Path = DataStorageDirectoryPath };

        var backendStorageLocation = new FileBackendStorageLocation(dataStorageLocation);

        // when
        var backendStorageDirectoryPath = backendStorageLocation.GetDirectoryPath();

        // then
        DirectoryAssert.Exists(Path.Combine(backendStorageDirectoryPath));
    }

    private class StubDataStorageLocation : IDataStorageLocation
    {
        public string Path { get; set; } = null!;

        public string GetDirectoryPath() => Path;
    }
}