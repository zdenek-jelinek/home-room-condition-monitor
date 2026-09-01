using Microsoft.Extensions.DependencyInjection;
using Rcm.Common.IO;
using Rcm.Common.Temporal;

namespace Rcm.Common;

public static class CommonServicesServiceCollectionExtensions
{
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        return services
            .AddTransient<IClock, Clock>()
            .AddTransient<IFileAccess, FileAccessAdapter>();
    }
}
