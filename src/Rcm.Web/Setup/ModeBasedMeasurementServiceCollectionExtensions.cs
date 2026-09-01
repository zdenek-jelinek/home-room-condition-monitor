using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rcm.Sensors.Abstractions;
using Rcm.Sensors.Bme280;
using Rcm.Sensors.Fakes;

namespace Rcm.Web.Setup;

public static class ModeBasedMeasurementServiceCollectionExtensions
{
    public static IServiceCollection AddMeasurementSensor(this IServiceCollection services, IConfiguration configuration)
    {
        var modeSection = configuration.GetSection("mode");
        if (string.Equals(modeSection.Value, "I2C", StringComparison.OrdinalIgnoreCase))
        {
            services.AddBme280Sensor(configuration);
        }
        else if (string.Equals(modeSection.Value, "Fake", StringComparison.OrdinalIgnoreCase))
        {
            services.AddFakeSensor();
        }
        else
        {
            throw new NotSupportedException(
                $"Measurement access mode '{modeSection.Value}' is not supported. Source configuration path: '{modeSection.Path}'.");
        }

        // Register ISensor resolved via a delegate.
        return services.AddTransient(s => s.GetRequiredService<ISensorFactory>().Create());
    }

}
