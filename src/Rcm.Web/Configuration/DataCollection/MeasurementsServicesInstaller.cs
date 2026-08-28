using Microsoft.Extensions.DependencyInjection;
using Rcm.Persistence;
using Rcm.Persistence.Abstractions;
using Rcm.Persistence.Files;
using Rcm.Services.Measurements.Collection;
using Rcm.Services.Measurements.Retrieval;

namespace Rcm.Web.Configuration.DataCollection;

public class MeasurementsServicesInstaller : IInstaller
{
    public void Install(IServiceCollection services)
    {
        services
            .AddTransient<IMeasurementTimingsCalculator, MeasurementTimingsCalculator>()
            .AddTransient<IMeasurementCollector, MeasurementCollector>()
            .AddSingleton<IMeasurementsStorage, CombinedFileAndMemoryMeasurementsStorage>()
            .AddSingleton<IMeasurementsWriter>(s => s.GetRequiredService<IMeasurementsStorage>())
            .AddSingleton<IMeasurementsReader>(s => s.GetRequiredService<IMeasurementsStorage>())
            .AddTransient<IMeasurementsAccessor, MeasurementsAccessor>()
            .AddTransient<IMeasurementsFileAccess, MeasurementsFileAccess>();

        services.AddHostedService<PeriodicMeasurementCollectionService>();
    }
}
