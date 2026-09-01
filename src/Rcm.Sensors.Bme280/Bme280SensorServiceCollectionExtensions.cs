using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rcm.I2c;
using Rcm.Sensors.Abstractions;

namespace Rcm.Sensors.Bme280;

public static class Bme280SensorServiceCollectionExtensions
{
    public static IServiceCollection AddBme280Sensor(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<I2cAccessOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations();

        return services
            .AddSingleton<ISensorFactory, Bme280DeviceFactory>()
            .AddTransient<I2cBusFactory>();
    }
}
