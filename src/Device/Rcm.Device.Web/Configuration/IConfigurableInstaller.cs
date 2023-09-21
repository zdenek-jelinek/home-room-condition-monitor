using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rcm.Device.Web.Configuration;

public interface IConfigurableInstaller
{
    void Install(IServiceCollection services, IConfiguration configuration);
}