using System;
using System.IO;
using NUnit.Framework;
using Rcm.Connector.Configuration;
using Rcm.Device.Common;
using Rcm.TestFramework.IO;

namespace Rcm.Connector.Tests.Configuration
{
    [TestFixture]
    public class FileLatestUploadedMeasurementGatewayIntegrationTests
    {
        private static TestDirectory DataDirectory { get; } = new TestDirectory(Path.GetFullPath("data"));
        private static string LatestUploadedMeasuremenFilePath => Path.Combine(DataDirectory.Path, "latest.txt");

        [SetUp]
        public void EnsureCleanDataDirectory()
        {
            DataDirectory.PrepareClean();
        }

        [TearDown]
        public void ClearDataDirectory()
        {
            DataDirectory.Delete();
        }

        [Test]
        public void PersistsLatestUploadedMeasurementTime()
        {
            // given
            var originalWrittenValue = new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));

            var writer = CreateFileLatestUploadedMeasurementGateway();
            var reader = CreateFileLatestUploadedMeasurementGateway();

            // when
            writer.SetLatestMeasurementUploadTime(originalWrittenValue);
            var roundtripValue = reader.GetLatestUploadedMeasurementTime();

            // then
            Assert.AreEqual(originalWrittenValue, roundtripValue);
        }

        [Test]
        public void DoesNotReplaceLatestUploadedMeasurementTimestampWithOlderValue()
        {
            // given
            var originalStoredTimestamp = "2000-01-01T12:00:00+02:00";

            File.WriteAllText(LatestUploadedMeasuremenFilePath, originalStoredTimestamp);

            var olderTimestampThanStored = new DateTimeOffset(2000, 1, 1, 10, 0, 0, TimeSpan.FromHours(2));

            var gateway = CreateFileLatestUploadedMeasurementGateway();

            // when
            gateway.SetLatestMeasurementUploadTime(olderTimestampThanStored);

            // then
            Assert.AreEqual(originalStoredTimestamp, File.ReadAllText(LatestUploadedMeasuremenFilePath));
        }

        [Test]
        public void PersistsLatestUploadededMeasurementTimeIntoFileInDataStorageLocation()
        {
            // given
            var time = new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));

            var writer = CreateFileLatestUploadedMeasurementGateway();

            // when
            writer.SetLatestMeasurementUploadTime(time);

            // then
            Assert.AreEqual("2000-01-01T12:00:00+02:00", File.ReadAllText(LatestUploadedMeasuremenFilePath));
        }

        [Test]
        public void PersistsNullLatestUploadedMeasurement()
        {
            // given
            File.WriteAllText(LatestUploadedMeasuremenFilePath, "2000-01-01T12:00:00+02:00");

            var writer = CreateFileLatestUploadedMeasurementGateway();
            var reader = CreateFileLatestUploadedMeasurementGateway();

            // when
            writer.SetLatestMeasurementUploadTime(null);
            var roundtripValue = reader.GetLatestUploadedMeasurementTime();

            // then
            Assert.IsNull(roundtripValue);
        }

        [Test]
        public void CreatesDataStorageDirectoryOnWriteIfItDidNotExist()
        {
            // given
            DataDirectory.Delete();

            var time = new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));

            var writer = CreateFileLatestUploadedMeasurementGateway();

            // when
            writer.SetLatestMeasurementUploadTime(time);

            // then
            Assert.AreEqual("2000-01-01T12:00:00+02:00", File.ReadAllText(LatestUploadedMeasuremenFilePath));
        }

        [Test]
        public void ReturnsNullIfLatestUploadedMeasurementFileDoesNotContainValidIso8601DateTime()
        {
            // given
            File.WriteAllText(LatestUploadedMeasuremenFilePath, "garbage");

            var gateway = CreateFileLatestUploadedMeasurementGateway();

            // when
            var latestUploadedMeasurement = gateway.GetLatestUploadedMeasurementTime();

            // then
            Assert.IsNull(latestUploadedMeasurement);
        }

        [Test]
        public void ReturnsNullIfLatestUploadedMeasurementFileDoesNotExist()
        {
            // given
            EnsureLatestUploadedMeasurementFileDoesNotExist();

            var gateway = CreateFileLatestUploadedMeasurementGateway();

            // when
            var latestUploadedMeasurement = gateway.GetLatestUploadedMeasurementTime();

            // then
            Assert.IsNull(latestUploadedMeasurement);
        }

        [Test]
        public void ThrowsWhenLatestUploadedMeasurementTimeCannotBeWrittenDueToFileLock()
        {
            // given
            using var fileLock = LockLatestUploadedMeasurementFile();

            var gateway = CreateFileLatestUploadedMeasurementGateway();

            // when
            void WriteLockedLatestUploadedMeasurementFile() => gateway.SetLatestMeasurementUploadTime(null);

            // then
            Assert.Catch<IOException>(WriteLockedLatestUploadedMeasurementFile);
        }

        private static FileLatestUploadedMeasurementGateway CreateFileLatestUploadedMeasurementGateway()
        {
            return new FileLatestUploadedMeasurementGateway(new StubDataStorageLocation());
        }

        private static void EnsureLatestUploadedMeasurementFileDoesNotExist()
        {
            if (!File.Exists(LatestUploadedMeasuremenFilePath))
            {
                return;
            }

            File.Delete(LatestUploadedMeasuremenFilePath);
        }

        public static IDisposable LockLatestUploadedMeasurementFile()
        {
            return File.OpenWrite(LatestUploadedMeasuremenFilePath);
        }

        private class StubDataStorageLocation : IDataStorageLocation
        {
            public string Path { get; set; } = DataDirectory.Path;
        }
    }
}
