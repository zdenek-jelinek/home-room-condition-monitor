using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rcm.Common;
using Rcm.Common.Http;
using Rcm.Common.IO;
using Rcm.Device.Common;

namespace Rcm.Web.Configuration.Common
{
    public class CommonServicesInstaller : IConfigurableInstaller
    {
        public void Install(IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddOptions<DataStorageLocation>()
                .Bind(configuration.GetSection("dataStorage"))
                .ValidateDataAnnotations();

            services
                .AddTransient<IClock, Clock>()
                .AddTransient<IFileAccess, FileAccessAdapter>()
                .AddTransient<IHttpClientFactory, HttpClientFactoryAdapter>()
                .AddTransient<IDataStorageLocation>(
                    sp => sp.GetRequiredService<IOptions<DataStorageLocation>>().Value);
        }
    }
}
