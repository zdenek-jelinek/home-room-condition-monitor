using Microsoft.Extensions.DependencyInjection;
using Rcm.Sensors.Abstractions;

namespace Rcm.Sensors.Fakes;

public static class FakeSensorServiceCollectionExtensions
{
    public static IServiceCollection AddFakeSensor(this IServiceCollection services)
    {
        return services.AddSingleton<ISensorFactory, FakeSensorFactory>();
    }
}
