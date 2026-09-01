using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rcm.Persistence;
using Rcm.Services;
using Rcm.Web.Configuration;
using Rcm.Web.Configuration.Common;
using Rcm.Web.Configuration.Sensors;
using Rcm.Web.Extensions;

namespace Rcm.Web;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddRazorPages(o => o.Conventions.AddPageRoute("/Now", ""));

        var measurementsConfiguration = configuration.GetSection("measurements");

        services
            .Install<CommonServicesInstaller>(configuration)
            .Install<ModeBasedMeasurementServicesInstaller>(measurementsConfiguration.GetSection("access"))
            .AddCombinedMemoryAndFilePersistence()
            .AddApplicationServices();
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
            .UseRouting()
            .UseEndpoints(e =>
            {
                e.MapStaticAssets().ShortCircuit();
                e.MapControllers();
                e.MapRazorPages().WithFlatpickrImportMap();
            });
    }
}
