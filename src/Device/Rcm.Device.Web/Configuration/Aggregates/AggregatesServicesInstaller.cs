using Microsoft.Extensions.DependencyInjection;
using Rcm.Device.Aggregates;
using Rcm.Device.Aggregates.Api;

namespace Rcm.Device.Web.Configuration.Aggregates;

public class AggregatesServicesInstaller : IInstaller
{
    public void Install(IServiceCollection services)
    {
        services.AddTransient<IMeasurementAggregatesAccessor, MeasurementAggregatesAccessor>();
    }
}