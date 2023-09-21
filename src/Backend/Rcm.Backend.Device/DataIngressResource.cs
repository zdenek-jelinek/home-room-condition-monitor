using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Rcm.Backend.Common;
using Rcm.Backend.Common.Http;
using Rcm.Backend.Device.Api;
using Rcm.Backend.Persistence.Devices;
using Rcm.Backend.Persistence.Measurements;
using Rcm.Common;
using static System.Globalization.CultureInfo;

namespace Rcm.Backend.Device;

public static class DataIngressResource
{
    [FunctionName("MeasurementIngress")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = DeviceRoutes.MeasurementsIngress)] HttpRequest request,
        [Table("devices")] CloudTable devices,
        [Table("measurements")] CloudTable measurementsTable,
        ILogger logger,
        CancellationToken token)
    {
        try
        {
            var authorizationToken = GetAuthorizationToken(request);

            var measurements = await ParseMeasurements(request.Body, token);

            await AuthorizeAsync(measurements.DeviceIdentifier, authorizationToken, devices, token);

            await StoreMeasurementsAsync(measurementsTable, measurements, token);

            return new NoContentResult();
        }
        catch (Exception e)
        {
            return new ExceptionHandler(logger, EnvironmentProperties.Name).Handle(e);
        }
    }

    private static async Task StoreMeasurementsAsync(CloudTable measurementsTable, DeviceMeasurements measurements, CancellationToken token)
    {
        var measurementsGateway = new MeasurementsGateway(measurementsTable);

        await measurementsGateway.StoreAsync(measurements, token);
    }

    private static async Task AuthorizeAsync(string deviceId, string authorizationToken, CloudTable devices, CancellationToken token)
    {
        var deviceGateway = new DeviceGateway(devices);
            
        var authorized = await deviceGateway.AuthorizeAsync(deviceId, authorizationToken, token);
        if (!authorized)
        {
            throw new AuthorizationException();
        }
    }

    private static string GetAuthorizationToken(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Authorization", out var token)
            || String.IsNullOrEmpty(token)
            || token.Count != 1
            || !token[0].StartsWith("Bearer ", StringComparison.InvariantCultureIgnoreCase))
        {
            throw new AuthorizationException();
        }

        return token[0].Substring("Bearer ".Length);
    }

    private static async Task<DeviceMeasurements> ParseMeasurements(Stream stream, CancellationToken token)
    {
        var measurements = await JsonSerializer.DeserializeAsync<MeasurementsIngressModel>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            token);

        if (measurements == null)
        {
            throw new InputValidationException("A non-null object payload is required.");
        }

        return Convert(measurements);
    }

    private static DeviceMeasurements Convert(MeasurementsIngressModel measurements)
    {
        if (String.IsNullOrEmpty(measurements.DeviceIdentifier))
        {
            throw new InputValidationException(nameof(MeasurementsIngressModel.DeviceIdentifier), "The value must not be non-empty string.");
        }

        if (measurements.Measurements is null)
        {
            throw new InputValidationException(nameof(MeasurementsIngressModel.Measurements), "The value is required.");
        }

        var convertedEntries = measurements.Measurements.Select(ConvertEntry).ToArray();

        return new DeviceMeasurements(measurements.DeviceIdentifier, convertedEntries);
    }

    private static Measurement ConvertEntry(MeasurementEntryIngressModel entry, int index)
    {
        if (!DateTimeOffset.TryParseExact(entry.Time, DateTimeFormat.Iso8601DateTime, InvariantCulture, default, out var time))
        {
            ThrowMeasurementPropertyError(
                index,
                nameof(MeasurementEntryIngressModel.Time),
                "The value is not a valid ISO-8601 datetime");
        }

        if (entry.Temperature is null)
        {
            ThrowMeasurementPropertyError(index, nameof(MeasurementEntryIngressModel.Temperature), "The value is required");
        }

        if (entry.Humidity is null)
        {
            ThrowMeasurementPropertyError(index, nameof(MeasurementEntryIngressModel.Humidity), "The value is required");
        }

        if (entry.Pressure is null)
        {
            ThrowMeasurementPropertyError(index, nameof(MeasurementEntryIngressModel.Pressure), "The value is required");
        }

        return new Measurement(time.UtcDateTime, entry.Temperature!.Value, entry.Pressure!.Value, entry.Humidity!.Value);
    }

    private static void ThrowMeasurementPropertyError(int index, string property, string message)
    {

        throw new InputValidationException(
            $"{nameof(MeasurementsIngressModel.Measurements)}[{index}].{property}",
            message);
    }
}