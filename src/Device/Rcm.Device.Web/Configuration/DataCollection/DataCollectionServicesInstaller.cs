using Microsoft.Extensions.DependencyInjection;
using Rcm.Device.DataCollection;
using Rcm.Device.DataCollection.Api;
using Rcm.Device.DataCollection.Files;
using Rcm.Device.Web.Services;

namespace Rcm.Device.Web.Configuration.DataCollection
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
