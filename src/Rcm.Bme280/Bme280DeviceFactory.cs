using System;
using Microsoft.Extensions.Logging;
using Rcm.Common;
using Rcm.I2c;
using Rcm.Measurement.Api;

namespace Rcm.Bme280
{
    public class Bme280DeviceFactory : IMeasurementProviderFactory, IDisposable
    {
        private readonly ILogger<Bme280I2cDevice> _deviceLogger;
        private readonly IBme280Configuration _bme280Configuration;
        private readonly IClock _clock;
        private readonly I2cBusFactory _i2cBusFactory;
        private readonly Lazy<Bme280I2cDevice> _device;

        public Bme280DeviceFactory(ILogger<Bme280I2cDevice> deviceLogger, IClock clock, I2cBusFactory i2cBusFactory, IBme280Configuration bme280Configuration)
        {
            _deviceLogger = deviceLogger;
            _clock = clock;
            _bme280Configuration = bme280Configuration;
            _i2cBusFactory = i2cBusFactory;

            _device = new Lazy<Bme280I2cDevice>(CreateDevice);
        }

        public IMeasurementProvider Create() => 
            _device.Value;

        public void Dispose()
        {
            if (_device.IsValueCreated)
            {
                _device.Value.Dispose();
            }
        }

        private Bme280I2cDevice CreateDevice() =>
            new Bme280I2cDevice(
                _deviceLogger,
                _clock,
                _i2cBusFactory.Open(_bme280Configuration.I2cBusAddress),
                _bme280Configuration.DeviceAddress);
    }
}
