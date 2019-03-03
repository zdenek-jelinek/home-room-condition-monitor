using Rcm.Connector.Api.Status;

namespace Rcm.Connector.MindSphere
{
    public class MindSphereConnectionStatusAccessor : IConnectionStatusAccessor
    {
        private readonly IMindSphereConnectionStatus _connectionStatus;

        public MindSphereConnectionStatusAccessor(IMindSphereConnectionStatus connectionStatus)
        {
            _connectionStatus = connectionStatus;
        }

        public ConnectionStatus GetStatus()
        {
            if (!_connectionStatus.IsOnboarded)
            {
                return new ConnectionStatus.NotEnabled();
            }

            if (_connectionStatus.Uptime is null)
            {
                return new ConnectionStatus.Disconnected();
            }

            return new ConnectionStatus.Connected(_connectionStatus.Uptime.Value);
        }
    }
}
