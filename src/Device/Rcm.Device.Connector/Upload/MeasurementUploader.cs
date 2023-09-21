using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rcm.Common;
using Rcm.Common.Http;
using Rcm.Device.Connector.Api.Upload;
using Rcm.Device.Connector.Configuration;

namespace Rcm.Device.Connector.Upload;

public class MeasurementUploader : IMeasurementUploader
{
    private readonly ILogger<MeasurementUploader> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConnectionConfigurationReader _connectionConfigurationReader;
    private readonly ILatestUploadedMeasurementWriter _latestUploadedMeasurementWriter;

    public MeasurementUploader(
        ILogger<MeasurementUploader> logger,
        IHttpClientFactory httpClientFactory,
        IConnectionConfigurationReader connectionConfigurationReader,
        ILatestUploadedMeasurementWriter latestUploadedMeasurementWriter)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _connectionConfigurationReader = connectionConfigurationReader;
        _latestUploadedMeasurementWriter = latestUploadedMeasurementWriter;
    }

    public async Task UploadAsync(IReadOnlyCollection<MeasurementEntry> measurements, CancellationToken token)
    {
        var measurementClient = CreateMeasurementClient();
        if (measurementClient is null)
        {
            return;
        }

        await UploadMeasurementAndSetAsLatestAsync(measurements, measurementClient, token);
    }

    private MeasurementClient? CreateMeasurementClient()
    {
        var configuration = _connectionConfigurationReader.ReadConfiguration();
        if (configuration is null)
        {
            return null;
        }

        return new MeasurementClient(_httpClientFactory.Create(MeasurementClient.HttpClientName), configuration);
    }

    private async Task UploadMeasurementAndSetAsLatestAsync(
        IReadOnlyCollection<MeasurementEntry> measurements,
        MeasurementClient measurementClient,
        CancellationToken token)
    {
        try
        {
            await measurementClient.UploadAsync(measurements, token);
            _latestUploadedMeasurementWriter.SetLatestMeasurementUploadTime(measurements.Max(m => m.Time));
        }
        catch (HttpRequestException e)
        {
            _logger.LogInformation(e, "Failed to upload measurements to back-end");
        }
    }
}