using Microsoft.Extensions.DependencyInjection;

namespace Rcm.Web.Configuration
{
    public interface IInstaller
    {
        void Install(IServiceCollection services);
    }
}