using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rcm.Common.Temporal;
using Rcm.I2c;
using Rcm.Sensors.Abstractions;

namespace Rcm.Sensors.Bme280;

public sealed class Bme280DeviceFactory : ISensorFactory, IDisposable
{
    private readonly ILogger<Bme280I2cDevice> _deviceLogger;
    private readonly IOptions<I2cAccessOptions> _i2cAccessOptions;
    private readonly IClock _clock;
    private readonly I2cBusFactory _i2cBusFactory;

    private Bme280I2cDevice? _device;

    public Bme280DeviceFactory(ILogger<Bme280I2cDevice> deviceLogger, IClock clock, I2cBusFactory i2cBusFactory, IOptions<I2cAccessOptions> i2CAccessOptions)
    {
        _deviceLogger = deviceLogger;
        _clock = clock;
        _i2cBusFactory = i2cBusFactory;
        _i2cAccessOptions = i2CAccessOptions;
    }

    public ISensor Create()
    {
        return _device ??= CreateDevice();
    }

    public void Dispose()
    {
        _device?.Dispose();
    }

    private Bme280I2cDevice CreateDevice()
    {
        var options = _i2cAccessOptions.Value;

        var i2CBus = _i2cBusFactory.Open(options.BusAddress);

        return new(_deviceLogger, _clock, i2CBus, options.DeviceAddress.Value);
    }
}
