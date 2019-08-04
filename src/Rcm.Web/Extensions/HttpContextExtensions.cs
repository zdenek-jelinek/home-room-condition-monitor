using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Rcm.Web.Extensions
{
    public static class HttpContextExtensions
    {
        public static string AppendVersionToPath(this HttpContext context, string path)
        {
            return context
                .RequestServices
                .GetRequiredService<IFileVersionProvider>()
                .AddFileVersionToPath(context.Request.PathBase, path);
        }
    }
}
