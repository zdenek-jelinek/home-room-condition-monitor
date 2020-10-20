using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Rcm.Common;
using Rcm.Common.Http;
using Rcm.Common.TestDoubles;
using Rcm.Common.TestDoubles.Http;
using Rcm.Device.Connector.Api.Configuration;
using Rcm.Device.Connector.Api.Upload;
using Rcm.Device.Connector.Configuration;
using Rcm.Device.Connector.Upload;

namespace Rcm.Device.Connector.Tests.Upload
{
    [TestFixture]
    public class MeasurementUploaderUnitTests
    {
        private static MeasurementEntry DummyMeasurementEntry =>
            new MeasurementEntry(new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), 25m, 37m, 1010m);

        private static ConnectionConfiguration DummyConfiguration =>
            new ConnectionConfiguration("https://dummy.server", "dummy-device", "dummy-key");

        [Test]
        public async Task UploadsMeasurementDataToConfiguredBackendAndUpdatesLatestUploadedMeasurementTimeOnSuccess()
        {
            // given
            var spyHttpClient = new SpyHttpClient { Response = new HttpResponseMessage(HttpStatusCode.NoContent) };

            var spyLatestUploadedMeasurementWriter = new SpyLatestUploadedMeasurementWriter();

            var configuration = DummyConfiguration;

            var measurementUploader = CreateMeasurementUploader(
                spyHttpClient,
                configuration,
                spyLatestUploadedMeasurementWriter);

            var uploadedMeasurement = DummyMeasurementEntry;

            // when
            await measurementUploader.UploadAsync(new[] { uploadedMeasurement }, default);

            // then
            Assert.AreEqual(uploadedMeasurement.Time, spyLatestUploadedMeasurementWriter.AssignedLatestMeasurementTime);
            var sentRequest = spyHttpClient.SentRequest!;
            Assert.IsNotNull(sentRequest);
            Assert.AreEqual(configuration.BaseUri, $"{sentRequest.RequestUri.Scheme}://{sentRequest.RequestUri.Host}");
        }

        [Test]
        public async Task DoesNotUpdateLatestMeasurementOnUnsuccessfulUpload()
        {
            // given
            var erroneousResponseHttpClient = new StubHttpClient
            {
                Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            };

            var spyLatestUploadedMeasurementWriter = new SpyLatestUploadedMeasurementWriter();


            var measurementUploader = CreateMeasurementUploader(
                erroneousResponseHttpClient,
                DummyConfiguration,
                spyLatestUploadedMeasurementWriter);

            var measurements = new[] { DummyMeasurementEntry };

            // when
            await measurementUploader.UploadAsync(measurements, default);

            // then
            Assert.IsFalse(spyLatestUploadedMeasurementWriter.LatestMeasurementTimeSet);
        }

        [Test]
        public async Task DoesNothingIfConnectionIsNotConfigured()
        {
            // given
            var spyHttpClient = new SpyHttpClient();
            var spyLatestUploadedMeasurementWriter = new SpyLatestUploadedMeasurementWriter();

            var noConfiguration = (ConnectionConfiguration?)null;

            var measurementUploader = CreateMeasurementUploader(
                spyHttpClient,
                noConfiguration,
                spyLatestUploadedMeasurementWriter);

            var measurements = new[] { DummyMeasurementEntry };

            // when
            await measurementUploader.UploadAsync(measurements, default);

            // then
            Assert.IsNull(spyHttpClient.SentRequest);
            Assert.IsFalse(spyLatestUploadedMeasurementWriter.LatestMeasurementTimeSet);
        }

        public static IMeasurementUploader CreateMeasurementUploader(
            IHttpClient? httpClient = null,
            ConnectionConfiguration? connectionConfiguration = null,
            ILatestUploadedMeasurementWriter? latestUploadedMeasurementWriter = null)
        {
            return new MeasurementUploader(
                new DummyLogger<MeasurementUploader>(),
                new StubHttpClientFactory { Client = httpClient ?? new StubHttpClient() },
                new StubConnectionConfigurationReader { Configuration = connectionConfiguration },
                latestUploadedMeasurementWriter ?? new SpyLatestUploadedMeasurementWriter());
        }

        private class StubConnectionConfigurationReader : IConnectionConfigurationReader
        {
            public ConnectionConfiguration? Configuration { get; set; }

            public ConnectionConfiguration? ReadConfiguration()
            {
                return Configuration;
            }
        }

        private class SpyLatestUploadedMeasurementWriter : ILatestUploadedMeasurementWriter
        {
            public DateTimeOffset? AssignedLatestMeasurementTime { get; private set; }
            public bool LatestMeasurementTimeSet { get; private set; }

            public void SetLatestMeasurementUploadTime(DateTimeOffset? time)
            {
                LatestMeasurementTimeSet = true;
                AssignedLatestMeasurementTime = time;
            }
        }
    }
}
