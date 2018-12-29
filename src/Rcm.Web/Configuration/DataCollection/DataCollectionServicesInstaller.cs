using Microsoft.Extensions.DependencyInjection;
using Rcm.DataCollection;
using Rcm.DataCollection.Api;
using Rcm.Web.Services;

namespace Rcm.Web.Configuration.DataCollection
{
    public class DataCollectionServicesInstaller : IInstaller
    {
        public void Install(IServiceCollection services)
        {
            services
                .AddTransient<IMeasurementCollector, MeasurementCollector>()
                .AddSingleton<ICollectedDataStorage, InMemoryCollectedDataStorage>()
                .AddTransient<ICollectedDataWriter>(s => s.GetRequiredService<ICollectedDataStorage>())
                .AddTransient<ICollectedDataAccessor>(s => s.GetRequiredService<ICollectedDataStorage>());

            services.AddHostedService<PeriodicDataCollectionService>();
        }
    }
}
