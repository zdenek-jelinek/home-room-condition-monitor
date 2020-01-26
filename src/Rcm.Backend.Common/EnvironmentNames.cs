using System;

namespace Rcm.Backend.Common
{
    public static class EnvironmentNames
    {
        public static string Development => "Development";
        public static string Staging => "Staging";
        public static string Production => "Production";

        public static bool IsDevelopment(string? name)
        {
            return Development.Equals(name, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsStaging(string? name)
        {
            return Staging.Equals(name, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
