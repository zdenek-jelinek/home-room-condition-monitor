using Microsoft.Extensions.DependencyInjection;
using Rcm.Connector.Api.Status;

namespace Rcm.Web.Configuration.Connectivity
{
    public class StubConnectivityInstaller : IInstaller
    {
        public void Install(IServiceCollection services)
        {
            services.AddTransient<IConnectionStatusAccessor, StubConnectionStatusAccessor>();
        }

        private class StubConnectionStatusAccessor : IConnectionStatusAccessor
        {
            public ConnectionStatus GetStatus() => new ConnectionStatus.NotEnabled();
        }
    }
}
