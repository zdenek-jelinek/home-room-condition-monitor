using Microsoft.Extensions.DependencyInjection;

namespace Rcm.Web.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Install<TInstaller>(this IServiceCollection services) where TInstaller : IInstaller, new()
        {
            var installer = new TInstaller();
            installer.Install(services);
            return services;
        }
    }
}
