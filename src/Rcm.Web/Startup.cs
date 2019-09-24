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
using Rcm.Web.Presentation;

namespace Rcm.Web
{
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
            services.AddRazorPages(o => o.Conventions.AddPageRoute("/Daily", ""));

            services
                .Install<CommonServicesInstaller>()
                .Install<ModeBasedMeasurementServicesInstaller>()
                .Install<DataCollectionServicesInstaller>()
                .Install<AggregatesServicesInstaller>()
                .Install<PresentationInstaller>()
                .Install<StubConnectivityInstaller>();
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
