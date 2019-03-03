using System;

namespace Rcm.Connector.MindSphere.Connection
{
    public class MindSphereConnection : IMindSphereConnectionStatus
    {
        public bool IsOnboarded { get; } = false;
        public TimeSpan? Uptime { get; } = null;

    }
}
