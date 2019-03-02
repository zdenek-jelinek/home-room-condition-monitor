using System;

namespace Rcm.Web.Presentation.Status
{
    public class ConnectivityStatusModel
    {
        public ConnectionStatusModel ConnectionStatus { get; }
        public TimeSpan? Uptime { get; }

        public ConnectivityStatusModel(ConnectionStatusModel status, TimeSpan? uptime)
        {
            ConnectionStatus = status;
            Uptime = uptime;
        }
    }
}