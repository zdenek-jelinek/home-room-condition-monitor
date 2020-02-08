using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rcm.Web.Configuration;
using Rcm.Web.Configuration.Aggregates;
using Rcm.Web.Configuration.Common;
using Rcm.Web.Configuration.Connectivity;
using Rcm.Web.Configuration.DataCollection;
using Rcm.Web.Configuration.Measurements;

namespace Rcm.Web
{
    [SuppressMessage("Style", "IDE0058:Expression value is never used")]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            var mvc = services.AddRazorPages(o => o.Conventions.AddPageRoute("/Now", ""));

#if DEBUG
            mvc.AddRazorRuntimeCompilation();
#endif

            services
                .Install<CommonServicesInstaller>()
                .Install<ModeBasedMeasurementServicesInstaller>()
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
}
