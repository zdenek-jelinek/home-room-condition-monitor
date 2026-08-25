using System;
using Microsoft.Extensions.Logging;
using Rcm.Common.Temporal;
using Rcm.I2c;
using Rcm.Sensors.Abstractions;

namespace Rcm.Sensors.Bme280;

public sealed class Bme280DeviceFactory : ISensorFactory, IDisposable
{
    private readonly ILogger<Bme280I2cDevice> _deviceLogger;
    private readonly II2cAccessConfiguration _i2cAccessConfiguration;
    private readonly IClock _clock;
    private readonly I2cBusFactory _i2cBusFactory;
    private readonly Lazy<Bme280I2cDevice> _device;

    public Bme280DeviceFactory(ILogger<Bme280I2cDevice> deviceLogger, IClock clock, I2cBusFactory i2cBusFactory, II2cAccessConfiguration i2cAccessConfiguration)
    {
        _deviceLogger = deviceLogger;
        _clock = clock;
        _i2cAccessConfiguration = i2cAccessConfiguration;
        _i2cBusFactory = i2cBusFactory;

        _device = new Lazy<Bme280I2cDevice>(CreateDevice);
    }

    public ISensor Create()
    {
        return _device.Value;
    }

    public void Dispose()
    {
        if (_device.IsValueCreated)
        {
            _device.Value.Dispose();
        }
    }

    private Bme280I2cDevice CreateDevice()
    {
        var i2CBus = _i2cBusFactory.Open(_i2cAccessConfiguration.I2cBusAddress);

        return new(_deviceLogger, _clock, i2CBus, _i2cAccessConfiguration.DeviceAddress);
    }
}
