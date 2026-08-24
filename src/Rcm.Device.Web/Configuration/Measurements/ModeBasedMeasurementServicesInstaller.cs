using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rcm.Device.Bme280;
using Rcm.Device.I2c;
using Rcm.Device.Measurement.Api;
using Rcm.Device.Measurement.Stubs;

namespace Rcm.Device.Web.Configuration.Measurements;

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

            case MeasurementAccessMode.Stub:
                InstallStubServices(services);
                break;

            default:
                throw new NotSupportedException($"Measurement access mode {mode} is not supported");

        }

        InstallCommonServices(services);
    }

    private void InstallCommonServices(IServiceCollection services)
    {
        services.AddTransient(s => s.GetRequiredService<IMeasurementProviderFactory>().Create());
    }

    private void InstallStubServices(IServiceCollection services)
    {
        services.AddSingleton<IMeasurementProviderFactory, StubMeasurementProviderFactory>();
    }

    private void InstallI2cServices(IServiceCollection services, IConfiguration measurementI2cAccessConfiguration)
    {
        services
            .AddOptions<I2cAccessConfiguration>()
            .Bind(measurementI2cAccessConfiguration)
            .ValidateDataAnnotations();

        services
            .AddSingleton<IMeasurementProviderFactory, Bme280DeviceFactory>()
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