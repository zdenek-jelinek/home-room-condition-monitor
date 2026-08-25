using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rcm.I2c;
using Rcm.Sensors.Abstractions;
using Rcm.Sensors.Bme280;
using Rcm.Sensors.Fakes;

namespace Rcm.Web.Configuration.Measurements;

public class ModeBasedMeasurementServicesInstaller : IConfigurableInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        var mode = new MeasurementAccessModeReader().Get(configuration);
        switch (mode)
        {
            case MeasurementAccessMode.I2c:
                InstallI2cServices(services, configuration);
                break;

            case MeasurementAccessMode.Fake:
                InstallFakeServices(services);
                break;

            default:
                throw new NotSupportedException($"Measurement access mode {mode} is not supported");

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
            .AddOptions<I2cAccessConfiguration>()
            .Bind(measurementI2cAccessConfiguration)
            .ValidateDataAnnotations();

        services
            .AddSingleton<ISensorFactory, Bme280DeviceFactory>()
            .AddTransient<II2cAccessConfiguration>(GetOptionValue<I2cAccessConfiguration>)
            .AddTransient<I2cBusFactory>();
    }

    private static T GetOptionValue<T>(IServiceProvider serviceProvider) where T : class, new()
    {
        return serviceProvider
            .GetRequiredService<IOptions<T>>()
            .Value;
    }
}