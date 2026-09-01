using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rcm.Common.IO;
using Rcm.Common.Temporal;
using Rcm.Persistence.Files.Navigation;

namespace Rcm.Web.Configuration.Common;

public class CommonServicesInstaller : IConfigurableInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<DataStorageOptions>()
            .Bind(configuration.GetSection("dataStorage"))
            .ValidateDataAnnotations();

        services
            .AddTransient<IClock, Clock>()
            .AddTransient<IFileAccess, FileAccessAdapter>()
            .AddTransient<IDataStorageLocation, DataStorageLocation>();
    }
}
