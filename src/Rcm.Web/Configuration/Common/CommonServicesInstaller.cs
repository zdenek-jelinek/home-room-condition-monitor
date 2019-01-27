using Microsoft.Extensions.DependencyInjection;
using Rcm.Common;
using Rcm.Common.IO;

namespace Rcm.Web.Configuration.Common
{
    public class CommonServicesInstaller : IInstaller
    {
        public void Install(IServiceCollection services)
        {
            services
                .AddTransient<IClock, Clock>()
                .AddTransient<IFileAccess, FileAccessAdapter>();
        }
    }
}
