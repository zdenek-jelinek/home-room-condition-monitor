using Microsoft.AspNetCore.Mvc.Rendering;

namespace Rcm.Device.Web.Extensions
{
    public static class HtmlHelperExtensions
    {
        public static string GetActiveClassIfCurrentPage(this IHtmlHelper htmlHelper, string page)
        {
            if (htmlHelper.ViewContext.RouteData.Values["page"]?.Equals(page) ?? false)
            {
                return "active";
            }
            
            return "";
        }
    }
}
