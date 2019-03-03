using Microsoft.Extensions.DependencyInjection;
using Rcm.Connector.Api.Status;
using Rcm.Connector.MindSphere;
using Rcm.Connector.MindSphere.Connection;

namespace Rcm.Web.Configuration.Connectivity
{
    public class MindSphereConnectivityInstaller : IInstaller
    {
        public void Install(IServiceCollection services) =>
            services
                .AddTransient<IConnectionStatusAccessor, MindSphereConnectionStatusAccessor>()
                .AddTransient<IMindSphereConnectionStatus, MindSphereConnection>();
    }
}
