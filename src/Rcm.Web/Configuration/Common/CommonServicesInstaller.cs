using Microsoft.Extensions.DependencyInjection;
using Rcm.Common;
using Rcm.Common.Http;
using Rcm.Common.IO;
using Rcm.Device.Common;

namespace Rcm.Web.Configuration.Common
{
    public class CommonServicesInstaller : IInstaller
    {
        public void Install(IServiceCollection services)
        {
            services
                .AddTransient<IClock, Clock>()
                .AddTransient<IFileAccess, FileAccessAdapter>()
                .AddTransient<IHttpClientFactory, HttpClientFactoryAdapter>()
                .AddTransient<IDataStorageLocation, EnvironmentDataStorageLocation>();
        }
    }
}
