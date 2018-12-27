using Microsoft.Extensions.DependencyInjection;
using Rcm.Common;

namespace Rcm.Web.Configuration.Common
{
    public class CommonServicesInstaller : IInstaller
    {
        public void Install(IServiceCollection services)
        {
            services.AddTransient<IClock, Clock>();
        }
    }
}
