using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Rcm.Common.TestFramework.IO;
using Rcm.Device.Connector.Api.Configuration;
using Rcm.Device.Connector.Configuration;

namespace Rcm.Device.Connector.Tests.Configuration
{
    [TestFixture]
    public class FileBackendConfigurationGatewayIntegrationTests
    {
        private static string ConfigurationDirectoryPath => Path.Combine(Path.GetFullPath("data"), "backend");

        private static string ConnectionConfigurationFilePath =>
            Path.Combine(ConfigurationDirectoryPath, "connection.json");

        private static TestDirectory ConfigurationDirectory { get; } = new TestDirectory(ConfigurationDirectoryPath);

        [SetUp]
        public void EnsureCleanDataDirectory()
        {
            ConfigurationDirectory.PrepareClean();
        }

        [TearDown]
        public void ClearDataDirectory()
        {
            ConfigurationDirectory.Delete();
        }

        [Test]
        public void PersistsBackendConnectionConfiguration()
        {
            // given
            var writer = CreateFileBackendConfigurationGateway();
            var reader = CreateFileBackendConfigurationGateway();

            var writtenConfiguration = new ConnectionConfiguration(
                baseUri: "http://dummy.server",
                deviceIdentifier: "dummy device id",
                deviceKey: "dummy device key");

            // when
            writer.WriteConfiguration(writtenConfiguration);
            var readConfiguration = reader.ReadConfiguration();

            // then
            Assert.IsNotNull(readConfiguration);
            Assert.AreEqual(writtenConfiguration.BaseUri, readConfiguration!.BaseUri);
            Assert.AreEqual(writtenConfiguration.DeviceIdentifier, readConfiguration.DeviceIdentifier);
            Assert.AreEqual(writtenConfiguration.DeviceKey, readConfiguration.DeviceKey);
        }

        [Test]
        public void PersistsConfigurationToFileLocatedInDataStorageLocation()
        {
            // given
            var backendConfigurationGateway = CreateFileBackendConfigurationGateway();

            var dummyConfiguration = new ConnectionConfiguration(
                baseUri: "http://dummy.server",
                deviceIdentifier: "dummy device id",
                deviceKey: "dummy device key");

            // when
            backendConfigurationGateway.WriteConfiguration(dummyConfiguration);

            // then
            Assert.AreEqual(
                new Dictionary<string, string>
                {
                    ["baseUri"] = "http://dummy.server",
                    ["deviceIdentifier"] = "dummy device id",
                    ["deviceKey"] = "dummy device key"
                },
                ReadFileAsDictionary(ConnectionConfigurationFilePath));
        }

        [Test]
        public void ThrowsForNonExtantConfigurationDirectoryOnWriteIfItDidNotExist()
        {
            // given
            ConfigurationDirectory.Delete();

            var backendConfigurationGateway = CreateFileBackendConfigurationGateway();

            var dummyConfiguration = new ConnectionConfiguration(
                baseUri: "http://dummy.server",
                deviceIdentifier: "dummy device id",
                deviceKey: "dummy device key");

            // when
            void WriteConfigurationToNonExtantDirectory()
            {
                backendConfigurationGateway.WriteConfiguration(dummyConfiguration);
            }

            // then
            Assert.Catch<DirectoryNotFoundException>(WriteConfigurationToNonExtantDirectory);
        }

        [Test]
        public void OverwritesExistingConfigurationFile()
        {
            // given
            var backendConfigurationGateway = CreateFileBackendConfigurationGateway();

            File.WriteAllLines(ConnectionConfigurationFilePath, Enumerable.Repeat("dummy contents", 100));

            var dummyConfiguration = new ConnectionConfiguration(
                baseUri: "http://dummy.server",
                deviceIdentifier: "dummy device id",
                deviceKey: "dummy device key");

            // when
            backendConfigurationGateway.WriteConfiguration(dummyConfiguration);

            // then
            Assert.AreEqual(
                new Dictionary<string, string>
                {
                    ["baseUri"] = "http://dummy.server",
                    ["deviceIdentifier"] = "dummy device id",
                    ["deviceKey"] = "dummy device key"
                },
                ReadFileAsDictionary(ConnectionConfigurationFilePath));
        }

        [Test]
        public void ReturnsNullWhenNoConfigurationFileExists()
        {
            // given
            var backendConfigurationGateway = CreateFileBackendConfigurationGateway();

            // when
            var configuration = backendConfigurationGateway.ReadConfiguration();

            // then
            Assert.IsNull(configuration);
        }

        [Test]
        public void ReturnsNullWhenAttemptingToReadMalformedJson()
        {
            // given
            File.WriteAllText(ConnectionConfigurationFilePath, "\"malformed\": \"json\"");

            var backendConfigurationGateway = CreateFileBackendConfigurationGateway();

            // when
            var configuration = backendConfigurationGateway.ReadConfiguration();

            // then
            Assert.IsNull(configuration);
        }

        [Test]
        [TestCase(@"{ ""baseUri"": ""http://dummy.server"", ""deviceIdentifier"": ""dummy device id"" }")]
        [TestCase(@"{ ""baseUri"": ""http://dummy.server"", ""deviceKey"": ""dummy device key"" }")]
        [TestCase(@"{ ""deviceIdentifier"": ""dummy device id"", ""deviceKey"": ""dummy device key"" }")]
        public void ReturnsNullWhenAttemptingToReadRecordThatDoesNotContainAllProperties(
            string jsonThatDoesNotContainAllConfigurationProperties)
        {
            // given
            File.WriteAllText(ConnectionConfigurationFilePath, jsonThatDoesNotContainAllConfigurationProperties);

            var backendConfigurationGateway = CreateFileBackendConfigurationGateway();

            // when
            var configuration = backendConfigurationGateway.ReadConfiguration();

            // then
            Assert.IsNull(configuration);
        }

        [Test]
        public void ReturnsNullWhenAttemptingToReadFromNonExtantConfigurationDirectory()
        {
            // given
            ConfigurationDirectory.Delete();

            var backendConfigurationGateway = CreateFileBackendConfigurationGateway();

            // when
            var configuration = backendConfigurationGateway.ReadConfiguration();

            // then
            Assert.IsNull(configuration);
        }

        [Test]
        public void EraseRemovesConfiguration()
        {
            // given
            CreateDummyConfigurationFile(ConnectionConfigurationFilePath);

            var deletingConfigurationGateway = CreateFileBackendConfigurationGateway();
            var readingConfigurationGateway = CreateFileBackendConfigurationGateway();

            // when
            deletingConfigurationGateway.EraseConfiguration();

            // then
            Assert.IsNull(readingConfigurationGateway.ReadConfiguration());
        }

        [Test]
        public void EraseDeletesConfigurationFileFromDisk()
        {
            // given
            CreateDummyConfigurationFile(ConnectionConfigurationFilePath);

            var configurationGateway = CreateFileBackendConfigurationGateway();

            // when
            configurationGateway.EraseConfiguration();

            // then
            FileAssert.DoesNotExist(ConnectionConfigurationFilePath);
        }

        [Test]
        public void EraseDoesNotThrowWhenConfigurationFileDoesNotExist()
        {
            // given
            EnsureFileNonExistence(ConnectionConfigurationFilePath);

            var configurationGateway = CreateFileBackendConfigurationGateway();

            // when
            void EraseNonExtantConfigurationFile() => configurationGateway.EraseConfiguration();

            // then
            Assert.DoesNotThrow(EraseNonExtantConfigurationFile);
        }

        [Test]
        public void EraseDoesNotThrowWhenConfigurationDirectoryDoesNotExist()
        {
            // given
            ConfigurationDirectory.Delete();

            var configurationGateway = CreateFileBackendConfigurationGateway();

            // when
            void EraseConfigurationFromNonExtantDirectory() => configurationGateway.EraseConfiguration();

            // then
            Assert.DoesNotThrow(EraseConfigurationFromNonExtantDirectory);
            
        }

        [Test]
        [Platform(Include = "Win")]
        public void EraseThrowsOriginalExceptionWhenTheFileCannotBeDeleted()
        {
            // given
            using var fileLock = LockFile(ConnectionConfigurationFilePath);

            var configurationGateway = CreateFileBackendConfigurationGateway();

            // when
            void EraseLockedFile() => configurationGateway.EraseConfiguration();

            // then
            Assert.Catch<IOException>(EraseLockedFile);
        }

        private static FileConnectionConfigurationGateway CreateFileBackendConfigurationGateway(
            IFileBackendStorageLocation? backendConfigurationLocation = null)
        {
            return new FileConnectionConfigurationGateway(
                NullLogger<FileConnectionConfigurationGateway>.Instance,
                backendConfigurationLocation ?? new StubFileBackendStorageLocation());
        }

        private static IReadOnlyDictionary<string, string>? ReadFileAsDictionary(string path)
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
        }

        private static void CreateDummyConfigurationFile(string path)
        {
            var configuration = new
            {
                baseUri = "http://dummy.server",
                deviceIdentifier = "dummy device id",
                deviceKey = "dummy device key"
            };

            File.WriteAllText(path, JsonSerializer.Serialize(configuration));
        }

        private static IDisposable LockFile(string path)
        {
            return File.OpenWrite(path);
        }

        private static void EnsureFileNonExistence(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            File.Delete(path);
        }

        private class StubFileBackendStorageLocation : IFileBackendStorageLocation
        {
            public string GetDirectoryPath()
            {
                return ConfigurationDirectory.Path;
            }
        }
    }
}
