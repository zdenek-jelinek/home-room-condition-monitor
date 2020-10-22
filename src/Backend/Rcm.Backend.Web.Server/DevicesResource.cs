using System;
using System.IO;
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
using Rcm.Backend.Common.Contracts;
using Rcm.Backend.Common.Http;
using Rcm.Backend.Persistence.Devices;

namespace Rcm.Backend.Web.Server
{
    public static class DevicesResource
    {
        [FunctionName("Devices")]
        public static async Task<IActionResult> CreateDeviceAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "devices")] HttpRequest request,
            [Table("devices")] CloudTable devices,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                var deviceName = await GetDeviceNameAsync(request.Body, cancellationToken);

                var deviceRegistration = await CreateDeviceAsync(deviceName, devices, cancellationToken);

                return new OkObjectResult(MapToOutput(deviceRegistration));
            }
            catch (Exception e)
            {
                return new ExceptionHandler(logger, EnvironmentProperties.Name).Handle(e);
            }
        }

        private static Task<DeviceRegistration> CreateDeviceAsync(string deviceName, CloudTable devices, CancellationToken cancellationToken)
        {
            var deviceGateway = new DeviceGateway(devices);

            return deviceGateway.CreateAsync(deviceName, cancellationToken);
        }

        private static DeviceRegistrationOutputNetworkModel MapToOutput(DeviceRegistration deviceRegistration)
        {
            return new DeviceRegistrationOutputNetworkModel
            {
                Identifier = deviceRegistration.Identifier,
                Key = deviceRegistration.Key,
                Name = deviceRegistration.Name
            };
        }

        private static async Task<string> GetDeviceNameAsync(Stream stream, CancellationToken cancellationToken)
        {
            var deviceRegistration = await JsonSerializer.DeserializeAsync<DeviceRegistrationInputNetworkModel>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);

            if (String.IsNullOrEmpty(deviceRegistration.Name))
            {
                throw new InputValidationException(
                    $"{nameof(DeviceRegistrationInputNetworkModel.Name)}",
                    "The value must not be null or empty.");
            }

            return deviceRegistration.Name;
        }
    }
}
