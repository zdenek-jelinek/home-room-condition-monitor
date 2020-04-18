using Microsoft.Extensions.DependencyInjection;
using Rcm.DataCollection;
using Rcm.DataCollection.Api;
using Rcm.DataCollection.Files;
using Rcm.Web.Services;

namespace Rcm.Web.Configuration.DataCollection
{
    public class DataCollectionServicesInstaller : IInstaller
    {
        public void Install(IServiceCollection services)
        {
            services
                .AddTransient<IMeasurementCollector, MeasurementCollector>()
                .AddSingleton<ICollectedDataStorage, CombinedFileAndMemoryCollectedDataStorage>()
                .AddSingleton<ICollectedDataWriter>(s => s.GetRequiredService<ICollectedDataStorage>())
                .AddSingleton<ICollectedDataAccessor>(s => s.GetRequiredService<ICollectedDataStorage>())
                .AddTransient<ICollectedDataFileAccess, CollectedDataFileAccess>();

            services.AddHostedService<PeriodicDataCollectionService>();
        }
    }
}
