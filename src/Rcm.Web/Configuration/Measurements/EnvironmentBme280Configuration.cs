using System;
using System.Globalization;
using Rcm.Bme280;

namespace Rcm.Web.Configuration.Measurements
{
    public class EnvironmentBme280Configuration : IBme280Configuration
    {
        public string I2cBusAddress { get; }
        public byte DeviceAddress { get; }

        public EnvironmentBme280Configuration()
        {
            const string bme280AddressConfigurationKey = "BME280_ADDRESS";
            var address = Environment.GetEnvironmentVariable(bme280AddressConfigurationKey);
            if (address is null)
            {
                throw new InvalidOperationException($"BME280 address not supplied in the environment configuration. "
                    + $"Expected under key \"{bme280AddressConfigurationKey}\" with format {{bus:device}}");
            }

            var parts = address.Split(":", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                throw new InvalidOperationException($"BME280 address has invalid format. "
                    + $"Key \"{bme280AddressConfigurationKey}\", expected \"{{bus}}:{{device}}\", got \"{address}\"");
            }

            I2cBusAddress = parts[0];

            var rawDeviceAddress = parts[1];
            if (rawDeviceAddress.StartsWith("0x"))
            {
                DeviceAddress = Byte.Parse(rawDeviceAddress.AsSpan().Slice(2), NumberStyles.HexNumber);
            }
            else
            {
                DeviceAddress = Byte.Parse(rawDeviceAddress);
            }
        }
    }
}
