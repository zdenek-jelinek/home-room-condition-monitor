using System;

namespace Rcm.Connector.Api.Status
{
    public abstract class ConnectionStatus
    {
        public class NotEnabled : ConnectionStatus
        {
        }

        public class Disconnected : ConnectionStatus
        {
        }

        public class Connected : ConnectionStatus
        {
            public TimeSpan Uptime { get; }
            public Connected(TimeSpan uptime)
            {
                Uptime = uptime;
            }
        }
    }
}