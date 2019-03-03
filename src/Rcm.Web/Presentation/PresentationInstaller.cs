using Microsoft.Extensions.DependencyInjection;
using Rcm.Web.Configuration;
using Rcm.Web.Presentation.Status;

namespace Rcm.Web.Presentation
{
    public class PresentationInstaller : IInstaller
    {
        public void Install(IServiceCollection services)
        {
            services.AddTransient<IStatusPagePresenter, StatusPagePresenter>();
        }
    }
}
