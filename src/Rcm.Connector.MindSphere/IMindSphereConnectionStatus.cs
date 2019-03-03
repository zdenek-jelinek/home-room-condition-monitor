using System;

namespace Rcm.Connector.MindSphere
{
    public interface IMindSphereConnectionStatus
    {
        bool IsOnboarded { get; }
        TimeSpan? Uptime { get; }
    }
}