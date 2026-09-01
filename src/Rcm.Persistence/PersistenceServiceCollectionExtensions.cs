using Microsoft.Extensions.DependencyInjection;
using Rcm.Persistence.Abstractions;
using Rcm.Persistence.Files;

namespace Rcm.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddCombinedMemoryAndFilePersistence(this IServiceCollection services)
    {
        return services
            .AddSingleton<IMeasurementsStorage, CombinedFileAndMemoryMeasurementsStorage>()
            .AddTransient<IMeasurementsFileAccess, MeasurementsFileAccess>()
            .AddCommonPersistenceServices();
    }

    public static IServiceCollection AddInMemoryPersistence(this IServiceCollection services)
    {
        return services
            .AddSingleton<IMeasurementsStorage, InMemoryMeasurementsStorage>()
            .AddCommonPersistenceServices();
    }

    private static IServiceCollection AddCommonPersistenceServices(this IServiceCollection services)
    {
        return services
            .AddTransient<IMeasurementsWriter>(s => s.GetRequiredService<IMeasurementsStorage>())
            .AddTransient<IMeasurementsReader>(s => s.GetRequiredService<IMeasurementsStorage>());
    }
}
