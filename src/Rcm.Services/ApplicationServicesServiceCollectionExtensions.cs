using Microsoft.Extensions.DependencyInjection;
using Rcm.Services.Aggregates;
using Rcm.Services.Measurements.Collection;
using Rcm.Services.Measurements.Retrieval;

namespace Rcm.Services;

public static class ApplicationServicesServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        return services
            .AddMeasurementAggregatesRetrieval()
            .AddMeasurementsRetrieval()
            .AddMeasurementsCollection();
    }

    private static IServiceCollection AddMeasurementAggregatesRetrieval(this IServiceCollection services)
    {
        return services.AddTransient<IMeasurementAggregatesAccessor, MeasurementAggregatesAccessor>();
    }

    private static IServiceCollection AddMeasurementsRetrieval(this IServiceCollection services)
    {
        return services.AddTransient<IMeasurementsAccessor, MeasurementsAccessor>();
    }

    private static IServiceCollection AddMeasurementsCollection(this IServiceCollection services)
    {
        return services
            .AddHostedService<PeriodicMeasurementCollectionService>()
            .AddTransient<IMeasurementTimingsCalculator, MeasurementTimingsCalculator>()
            .AddTransient<IMeasurementCollector, MeasurementCollector>();
    }
}
