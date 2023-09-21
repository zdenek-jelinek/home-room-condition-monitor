using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rcm.Backend.Device.Api;
using Rcm.Common;
using Rcm.Common.Tasks;
using Rcm.Common.TestDoubles.Http;
using Rcm.Device.Connector.Api.Configuration;
using Rcm.Device.Connector.Upload;

namespace Rcm.Device.Connector.Tests.Upload;

[TestFixture]
public class MeasurementClientUnitTests
{
    private static ConnectionConfiguration Configuration =>
        new ConnectionConfiguration("http://dummy-uri.com", "dummy device identifier", "dummy device key");

    private DateTimeOffset DummyTime { get; } = new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));

    [Test]
    public async Task UploadsMeasurementEntriesAsJsonWithDeviceIdAndAuthenticationToken()
    {
        // given
        var measurements = new[]
        {
            new MeasurementEntry(DummyTime, 30m, 45.1m, 1010.27m),
            new MeasurementEntry(DummyTime.AddMinutes(10), -10, 27.4m, 1011.43m)
        };

        var expectedPayload = new MeasurementsIngressModel
        {
            DeviceIdentifier = Configuration.DeviceIdentifier,
            Measurements = new[]
            {
                new MeasurementEntryIngressModel
                {
                    Time = "2000-01-01T12:00:00+02:00",
                    Temperature = measurements[0].CelsiusTemperature,
                    Humidity = measurements[0].RelativeHumidity,
                    Pressure = measurements[0].HpaPressure
                },
                new MeasurementEntryIngressModel
                {
                    Time = "2000-01-01T12:10:00+02:00",
                    Temperature = measurements[1].CelsiusTemperature,
                    Humidity = measurements[1].RelativeHumidity,
                    Pressure = measurements[1].HpaPressure
                }
            }
        };

        var spyHttpClient = new SpyHttpClient();

        var client = new MeasurementClient(spyHttpClient, Configuration);

        // when
        await client.UploadAsync(measurements, default);

        // then
        var sentRequest = spyHttpClient.SentRequest!;
        Assert.IsNotNull(sentRequest);
        Assert.AreEqual(HttpMethod.Post, sentRequest.Method);
        Assert.AreEqual($"{Configuration.BaseUri}/{DeviceRoutes.MeasurementsIngress}", sentRequest.RequestUri?.AbsoluteUri);
        Assert.IsNotNull(sentRequest.Headers.Authorization);
        Assert.AreEqual("Bearer", sentRequest.Headers.Authorization!.Scheme);
        Assert.AreEqual(Configuration.DeviceKey, sentRequest.Headers.Authorization.Parameter);

        Assert.IsNotNull(sentRequest.Content);
        var requestBody = await sentRequest.Content!.ReadAsStreamAsync();
        var sentPayload = await JsonSerializer.DeserializeAsync<MeasurementsIngressModel>(requestBody);
        Assert.That(expectedPayload, Is.EqualTo(sentPayload).Using<MeasurementsIngressModel>(MeasurementIngressModelEquals));
    }

    [Test]
    public async Task CancellationStopsWaitingForPendingRequest()
    {
        // given
        var dummyMeasurements = new[] { new MeasurementEntry(DummyTime, 30m, 45.1m, 1010.27m) };

        var blockingHttpClient = new BlockingHttpClient();
        var client = new MeasurementClient(blockingHttpClient, Configuration);

        using var tokenSource = new CancellationTokenSource();

        // when
        var upload = client.UploadAsync(dummyMeasurements, tokenSource.Token);

        await blockingHttpClient.Blocked;

        var uploadCompletedBeforeCancellation = upload.IsCompleted;

        tokenSource.Cancel();

        // then
        Assert.IsTrue(await upload.TryWait(TimeSpan.FromSeconds(1)), "Upload wait stopped after cancellation");
        Assert.AreEqual(TaskStatus.Canceled, upload.Status);
        Assert.IsFalse(uploadCompletedBeforeCancellation, nameof(uploadCompletedBeforeCancellation));
    }

    private bool MeasurementIngressModelEquals(MeasurementsIngressModel a, MeasurementsIngressModel b)
    {
        return a.DeviceIdentifier!.Equals(b.DeviceIdentifier, StringComparison.OrdinalIgnoreCase)
            && AreEqual(a.Measurements!, b.Measurements!);
    }

    private static bool AreEqual(IEnumerable<MeasurementEntryIngressModel> a, IEnumerable<MeasurementEntryIngressModel> b)
    {
        var aAsSet = new HashSet<MeasurementEntryIngressModel>(a, new MeasurementEntryIngressModelEqualityComparer());

        return aAsSet.SetEquals(b);
    }

    private class MeasurementEntryIngressModelEqualityComparer : IEqualityComparer<MeasurementEntryIngressModel>
    {
        public bool Equals([AllowNull] MeasurementEntryIngressModel x, [AllowNull] MeasurementEntryIngressModel y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return ParseTime(x.Time!).Equals(ParseTime(y.Time!))
                && x.Temperature == y.Temperature
                && x.Pressure == y.Pressure
                && x.Humidity == y.Humidity;
        }

        private DateTimeOffset ParseTime(string time)
        {
            return DateTimeOffset.ParseExact(time, DateTimeFormat.Iso8601DateTime, CultureInfo.InvariantCulture);
        }

        public int GetHashCode([DisallowNull] MeasurementEntryIngressModel obj)
        {
            return HashCode.Combine(obj.Time, obj.Temperature, obj.Pressure, obj.Humidity);
        }
    }
}