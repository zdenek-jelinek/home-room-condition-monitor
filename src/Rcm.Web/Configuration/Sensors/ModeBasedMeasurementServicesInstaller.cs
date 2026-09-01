using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rcm.I2c;
using Rcm.Sensors.Abstractions;
using Rcm.Sensors.Bme280;
using Rcm.Sensors.Fakes;

namespace Rcm.Web.Configuration.Sensors;

public class ModeBasedMeasurementServicesInstaller : IConfigurableInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        var modeSection = configuration.GetSection("mode");
        if (string.Equals(modeSection.Value, "I2C", StringComparison.OrdinalIgnoreCase))
        {
            InstallI2cServices(services, configuration);
        }
        else if (string.Equals(modeSection.Value, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            InstallFakeServices(services);
        }
        else
        {
            throw new NotSupportedException(
                $"Measurement access mode '{modeSection.Value}' is not supported. Source configuration path: '{modeSection.Path}'.");
        }

        InstallCommonServices(services);
    }

    private void InstallCommonServices(IServiceCollection services)
    {
        services.AddTransient(s => s.GetRequiredService<ISensorFactory>().Create());
    }

    private void InstallFakeServices(IServiceCollection services)
    {
        services.AddSingleton<ISensorFactory, FakeSensorFactory>();
    }

    private void InstallI2cServices(IServiceCollection services, IConfiguration measurementI2cAccessConfiguration)
    {
        services
            .AddOptions<I2cAccessOptions>()
            .Bind(measurementI2cAccessConfiguration)
            .ValidateDataAnnotations();

        services
            .AddSingleton<ISensorFactory, Bme280DeviceFactory>()
            .AddTransient<I2cBusFactory>();
    }
}
