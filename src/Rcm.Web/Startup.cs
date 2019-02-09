using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rcm.Web.Configuration;
using Rcm.Web.Configuration.Aggregates;
using Rcm.Web.Configuration.Common;
using Rcm.Web.Configuration.DataCollection;
using Rcm.Web.Configuration.Measurements;

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
            services
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddRazorPagesOptions(o => o.Conventions.AddPageRoute("/Daily", ""));

            services
                .Install<CommonServicesInstaller>()
                .Install<ModeBasedMeasurementServicesInstaller>()
                .Install<DataCollectionServicesInstaller>()
                .Install<AggregatesServicesInstaller>();
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseMvc();
        }
    }
}
