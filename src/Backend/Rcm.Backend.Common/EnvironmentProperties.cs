using System;

namespace Rcm.Backend.Common;

public static class EnvironmentProperties
{
    public static string Name => GetEnvironmentVariable("ENVIRONMENT_NAME") ?? EnvironmentNames.Production;

    private static string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }
}