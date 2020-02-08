using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Rcm.Backend.Device.Api;
using Rcm.Common;
using Rcm.Common.Http;
using Rcm.Connector.Api.Configuration;
using static System.Globalization.CultureInfo;
using static System.Text.Encoding;

namespace Rcm.Connector.Upload
{
    public class MeasurementClient
    {
        public static string HttpClientName => "Backend";

        private readonly IHttpClient _httpClient;
        private readonly ConnectionConfiguration _connectionConfiguration;

        public MeasurementClient(IHttpClient httpClient, ConnectionConfiguration connectionConfiguration)
        {
            _httpClient = httpClient;
            _connectionConfiguration = connectionConfiguration;
        }

        public Task UploadAsync(IEnumerable<MeasurementEntry> measurements, CancellationToken token)
        {
            var payload = Serialize(measurements);

            return UploadAsync(payload, token);
        }

        private async Task UploadAsync(string payload, CancellationToken token)
        {
            var uploadRequest = new HttpRequestMessage(HttpMethod.Post, $"{_connectionConfiguration.BaseUri}/{DeviceRoutes.MeasurementsIngress}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _connectionConfiguration.DeviceKey) },
                Content = new StringContent(payload, UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(uploadRequest, token);

            _ = response.EnsureSuccessStatusCode();
        }

        private string Serialize(IEnumerable<MeasurementEntry> measurements)
        {
            var ingressModel = new MeasurementsIngressModel
            {
                DeviceIdentifier = _connectionConfiguration.DeviceIdentifier,
                Measurements = measurements.Select(ToIngressModel)
            };

            return JsonSerializer.Serialize(ingressModel);
        }

        private MeasurementEntryIngressModel ToIngressModel(MeasurementEntry measurement)
        {
            return new MeasurementEntryIngressModel
            {
                Time = measurement.Time.ToString(DateTimeFormat.Iso8601DateTime, InvariantCulture),
                Humidity = measurement.RelativeHumidity,
                Pressure = measurement.HpaPressure,
                Temperature = measurement.CelsiusTemperature
            };
        }
    }
}
