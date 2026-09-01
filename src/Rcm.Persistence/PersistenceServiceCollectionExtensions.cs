using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rcm.Persistence.Abstractions;
using Rcm.Persistence.Files;
using Rcm.Persistence.Files.Navigation;

namespace Rcm.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddCombinedMemoryAndFilePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<DataStorageOptions>()
            .Bind(configuration.GetSection("dataStorage"))
            .ValidateDataAnnotations();

        return services
            .AddSingleton<IMeasurementsStorage, CombinedFileAndMemoryMeasurementsStorage>()
            .AddTransient<IMeasurementsFileAccess, MeasurementsFileAccess>()
            .AddTransient<MeasurementsFilesNavigator>()
            .AddTransient<IDataStorageLocation, DataStorageLocation>()
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
            .AddSingleton<IMeasurementsWriter>(s => s.GetRequiredService<IMeasurementsStorage>())
            .AddSingleton<IMeasurementsReader>(s => s.GetRequiredService<IMeasurementsStorage>());
    }
}
