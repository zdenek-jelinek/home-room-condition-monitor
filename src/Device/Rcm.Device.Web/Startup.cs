using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rcm.Device.Web.Configuration;
using Rcm.Device.Web.Configuration.Aggregates;
using Rcm.Device.Web.Configuration.Common;
using Rcm.Device.Web.Configuration.Connectivity;
using Rcm.Device.Web.Configuration.DataCollection;
using Rcm.Device.Web.Configuration.Measurements;

namespace Rcm.Device.Web;

[SuppressMessage("Style", "IDE0058:Expression value is never used")]
public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        var mvc = services.AddRazorPages(o => o.Conventions.AddPageRoute("/Now", ""));

#if DEBUG
        mvc.AddRazorRuntimeCompilation();
#endif

        var measurementsConfiguration = _configuration.GetSection("measurements");

        services
            .Install<CommonServicesInstaller>(_configuration)
            .Install<ModeBasedMeasurementServicesInstaller>(measurementsConfiguration.GetSection("access"))
            .Install<DataCollectionServicesInstaller>()
            .Install<AggregatesServicesInstaller>()
            .Install<ConnectivityInstaller>();
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            application.UseDeveloperExceptionPage();
        }
        else
        {
            application.UseExceptionHandler("/Error");
        }

        application
            .UseStaticFiles()
            .UseRouting()
            .UseEndpoints(e => 
            {
                e.MapControllers();
                e.MapRazorPages();
            });
    }
}