﻿using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Rcm.Web.Extensions
{
    public static class HtmlHelperExtensions
    {
        public static string GetActiveClass(this IHtmlHelper htmlHelper, string page)
        {
            if (htmlHelper.ViewContext.RouteData.Values["page"]?.Equals(page) ?? false)
            {
                return "active";
            }
            
            return "";
        }
    }
}