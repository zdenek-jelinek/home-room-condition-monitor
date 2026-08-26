using Microsoft.Extensions.DependencyInjection;
using Rcm.DataCollection;
using Rcm.DataCollection.Api;
using Rcm.DataCollection.Files;
using Rcm.Services.Measurements.Retrieval;
using Rcm.Web.Services;

namespace Rcm.Web.Configuration.DataCollection;

public class DataCollectionServicesInstaller : IInstaller
{
    public void Install(IServiceCollection services)
    {
        services
            .AddTransient<IMeasurementCollector, MeasurementCollector>()
            .AddSingleton<IMeasurementsStorage, CombinedFileAndMemoryMeasurementsStorage>()
            .AddSingleton<IMeasurementsWriter>(s => s.GetRequiredService<IMeasurementsStorage>())
            .AddSingleton<IMeasurementsReader>(s => s.GetRequiredService<IMeasurementsStorage>())
            .AddTransient<IMeasurementsAccessor, MeasurementsAccessor>()
            .AddTransient<IMeasurementsFileAccess, MeasurementsFileAccess>();

        services.AddHostedService<PeriodicDataCollectionService>();
    }
}
