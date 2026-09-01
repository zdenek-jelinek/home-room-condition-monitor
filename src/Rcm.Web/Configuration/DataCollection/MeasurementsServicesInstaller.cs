using Microsoft.Extensions.DependencyInjection;
using Rcm.Persistence;
using Rcm.Persistence.Abstractions;
using Rcm.Persistence.Files;

namespace Rcm.Web.Configuration.DataCollection;

public class MeasurementsServicesInstaller : IInstaller
{
    public void Install(IServiceCollection services)
    {
        services
            .AddSingleton<IMeasurementsStorage, CombinedFileAndMemoryMeasurementsStorage>()
            .AddSingleton<IMeasurementsWriter>(s => s.GetRequiredService<IMeasurementsStorage>())
            .AddSingleton<IMeasurementsReader>(s => s.GetRequiredService<IMeasurementsStorage>())
            .AddTransient<IMeasurementsFileAccess, MeasurementsFileAccess>();
    }
}
