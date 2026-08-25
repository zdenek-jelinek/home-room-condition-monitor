using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;

namespace Rcm.Web.Extensions;

public static class FlatpickrImportMapEndpointConventionBuilderExtensions
{
    public static TEndpointConventionBuilder WithFlatpickrImportMap<TEndpointConventionBuilder>(this TEndpointConventionBuilder builder)
        where TEndpointConventionBuilder : IEndpointConventionBuilder
    {
        var flatpickrImportMap = new ImportMapDefinition(imports: CreateFlatpickrImportMap(), scopes: null, integrity: null);

        builder.Add(b => AppendImportMap(b, flatpickrImportMap));

        return builder;
    }

    private static Dictionary<string, string> CreateFlatpickrImportMap()
    {
        // flatpickr does not support ESM directly, unfortunately.
        // The first thing is the "flatpickr" bare import which is fine to resolve with an import map.
        // The other thing is that the deeper import statements use CJS path formats which don't work since ESM does not perform any module path resolution.
        // The following is the minimum setup that works with the current version of flatpickr.
        // It might break with a future version. This is not a significant risk since the project is inactive.

        return new()
        {
            ["flatpickr"] = ComposePath("index.js"),
            [ComposePath("types/options")] = ComposePath("types/options.js"),
            [ComposePath("l10n/default")] = ComposePath("l10n/default.js"),
            [ComposePath("utils/dom")] = ComposePath("utils/dom.js"),
            [ComposePath("utils/dates")] = ComposePath("utils/dates.js"),
            [ComposePath("utils/formatting")] = ComposePath("utils/formatting.js"),
            [ComposePath("utils")] = ComposePath("utils/index.js"),
            [ComposePath("utils/polyfills")] = ComposePath("utils/polyfills.js")
        };

        static string ComposePath(string segment) => $"./lib/flatpickr/dist/esm/{segment}";
    }

    private static void AppendImportMap(EndpointBuilder builder, ImportMapDefinition importMap)
    {
        var preexistingImportMap = builder.Metadata.OfType<ImportMapDefinition>().FirstOrDefault();
        if (preexistingImportMap != null)
        {
            builder.Metadata.Remove(preexistingImportMap);
            builder.Metadata.Add(ImportMapDefinition.Combine(preexistingImportMap, importMap));
        }
        else
        {
            builder.Metadata.Add(importMap);
        }
    }
}
