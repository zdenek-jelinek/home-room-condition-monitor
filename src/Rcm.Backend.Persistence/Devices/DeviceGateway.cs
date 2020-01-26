using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Rcm.Backend.Common;
using CryptoRandomNumberGenerator = System.Security.Cryptography.RandomNumberGenerator;

namespace Rcm.Backend.Persistence.Devices
{
    public class DeviceGateway
    {
        private readonly CloudTable _devicesTable;

        public DeviceGateway(CloudTable devices) => _devicesTable = devices;

        public async Task<bool> AuthorizeAsync(string deviceId, string authorizationToken, CancellationToken cancellationToken)
        {
            var newTableCreated = await _devicesTable.CreateIfNotExistsAsync(default, default, cancellationToken);
            if (newTableCreated)
            {
                return false;
            }

            var retrieval = TableOperation.Retrieve<DeviceEntity>(deviceId, "0", DeviceEntity.Columns);

            var result = await _devicesTable.ExecuteAsync(retrieval, default, default, cancellationToken);

            return result.Result != null
                && result.Result is DeviceEntity entity
                && entity.Key.Equals(authorizationToken, StringComparison.InvariantCulture);
        }

        public async Task<DeviceRegistration> CreateAsync(string deviceName, CancellationToken token)
        {
            await _devicesTable.CreateIfNotExistsAsync(default, default, token);

            var deviceKey = GenerateDeviceKey();

            var deviceIdentifier = Guid.NewGuid().ToString();
            await PersistDeviceAsync(deviceName, deviceKey, deviceIdentifier, token);

            return new DeviceRegistration(deviceName, deviceIdentifier, deviceKey);
        }

        private async Task PersistDeviceAsync(
            string deviceName,
            string deviceKey,
            string deviceIdentifier,
            CancellationToken token)
        {
            var device = new DeviceEntity
            {
                Name = deviceName,
                PartitionKey = deviceIdentifier,
                RowKey = "0",
                Key = deviceKey
            };

            var result = await _devicesTable.ExecuteAsync(TableOperation.Insert(device), default, default, token);
            if (result.HttpStatusCode == (int)HttpStatusCode.Conflict)
            {
                throw new ConflictException($"A device with name \"{deviceName}\" already exists.");
            }
        }

        private static string GenerateDeviceKey()
        {
            var key = new byte[128];

            CryptoRandomNumberGenerator.Fill(key);

            return Convert.ToBase64String(key);
        }

        private class DeviceEntity : TableEntity
        {
            public string Name { get; set; } = String.Empty;
            public string Key { get; set; } = String.Empty;

            public static List<string> Columns { get; } = new List<string>
            {
                nameof(PartitionKey),
                nameof(RowKey),
                nameof(Name),
                nameof(Key)
            };
        }
    }
}
