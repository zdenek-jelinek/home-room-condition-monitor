using Microsoft.Extensions.DependencyInjection;

namespace Rcm.Device.Web.Configuration;

public interface IInstaller
{
    void Install(IServiceCollection services);
}