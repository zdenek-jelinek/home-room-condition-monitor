using Microsoft.Extensions.DependencyInjection;
using Rcm.Services.Aggregates;

namespace Rcm.Web.Configuration.Aggregates;

public class AggregatesServicesInstaller : IInstaller
{
    public void Install(IServiceCollection services)
    {
        services.AddTransient<IMeasurementAggregatesAccessor, MeasurementAggregatesAccessor>();
    }
}
