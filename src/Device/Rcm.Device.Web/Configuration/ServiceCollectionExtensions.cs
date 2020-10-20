using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rcm.Device.Web.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Install<TInstaller>(this IServiceCollection services)
            where TInstaller : IInstaller, new()
        {
            var installer = new TInstaller();
            installer.Install(services);
            return services;
        }

        public static IServiceCollection Install<TInstaller>(
            this IServiceCollection services,
            IConfiguration configuration)
            where TInstaller : IConfigurableInstaller, new()
        {
            var installer = new TInstaller();
            installer.Install(services, configuration);
            return services;
        }
    }
}
