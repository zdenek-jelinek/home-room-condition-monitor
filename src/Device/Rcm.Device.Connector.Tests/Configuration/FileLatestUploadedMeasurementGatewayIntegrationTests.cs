using System;
using System.IO;
using NUnit.Framework;
using Rcm.Common.TestFramework.IO;
using Rcm.Device.Connector.Configuration;

namespace Rcm.Device.Connector.Tests.Configuration
{
    [TestFixture]
    public class FileLatestUploadedMeasurementGatewayIntegrationTests
    {
        private static string BackendStorageDirectoryPath => Path.Combine(Path.GetFullPath("data"), "backend");

        private static string LatestUploadedMeasurementFilePath =>
            Path.Combine(BackendStorageDirectoryPath, "latest.txt");

        private static TestDirectory BackendStorageDirectory { get; } = new TestDirectory(BackendStorageDirectoryPath);

        [SetUp]
        public void EnsureCleanDataDirectory()
        {
            BackendStorageDirectory.PrepareClean();
        }

        [TearDown]
        public void ClearDataDirectory()
        {
            BackendStorageDirectory.Delete();
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

            File.WriteAllText(LatestUploadedMeasurementFilePath, originalStoredTimestamp);

            var olderTimestampThanStored = new DateTimeOffset(2000, 1, 1, 10, 0, 0, TimeSpan.FromHours(2));

            var gateway = CreateFileLatestUploadedMeasurementGateway();

            // when
            gateway.SetLatestMeasurementUploadTime(olderTimestampThanStored);

            // then
            Assert.AreEqual(originalStoredTimestamp, File.ReadAllText(LatestUploadedMeasurementFilePath));
        }

        [Test]
        public void PersistsLatestUploadedMeasurementTimeIntoFileInBackendStorageLocation()
        {
            // given
            var time = new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));

            var writer = CreateFileLatestUploadedMeasurementGateway();

            // when
            writer.SetLatestMeasurementUploadTime(time);

            // then
            Assert.AreEqual("2000-01-01T12:00:00+02:00", File.ReadAllText(LatestUploadedMeasurementFilePath));
        }

        [Test]
        public void PersistsLatestUploadedMeasurementInRoundtripSupportingFormat()
        {
            // given
            File.WriteAllText(LatestUploadedMeasurementFilePath, "2000-01-01T12:00:00+02:00");

            var writer = CreateFileLatestUploadedMeasurementGateway();
            var reader = CreateFileLatestUploadedMeasurementGateway();

            // when
            writer.SetLatestMeasurementUploadTime(null);
            var roundtripValue = reader.GetLatestUploadedMeasurementTime();

            // then
            Assert.IsNull(roundtripValue);
        }

        [Test]
        public void ThrowsForNonExtantBackendStorageDirectory()
        {
            // given
            BackendStorageDirectory.Delete();

            var dummyTime = new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));

            var writer = CreateFileLatestUploadedMeasurementGateway();

            // when
            void WriteIntoNonExtantBackendStorageDirectory()
            {
                writer.SetLatestMeasurementUploadTime(dummyTime);
            }

            // then
            Assert.Catch<DirectoryNotFoundException>(WriteIntoNonExtantBackendStorageDirectory);
        }

        [Test]
        public void ReturnsNullIfLatestUploadedMeasurementFileDoesNotContainValidIso8601DateTime()
        {
            // given
            File.WriteAllText(LatestUploadedMeasurementFilePath, "garbage");

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
        public void ReturnsNullIfBackendStorageDirectoryDoesNotExist()
        {
            // given
            BackendStorageDirectory.Delete();

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
            return new FileLatestUploadedMeasurementGateway(new StubFileBackendStorageLocation());
        }

        private static void EnsureLatestUploadedMeasurementFileDoesNotExist()
        {
            if (!File.Exists(LatestUploadedMeasurementFilePath))
            {
                return;
            }

            File.Delete(LatestUploadedMeasurementFilePath);
        }

        public static IDisposable LockLatestUploadedMeasurementFile()
        {
            return File.OpenWrite(LatestUploadedMeasurementFilePath);
        }

        private class StubFileBackendStorageLocation : IFileBackendStorageLocation
        {
            public string GetDirectoryPath()
            {
                return BackendStorageDirectory.Path;
            }
        }
    }
}
