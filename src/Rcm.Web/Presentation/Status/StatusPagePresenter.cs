using System;
using Rcm.Connector.Api.Status;

namespace Rcm.Web.Presentation.Status
{
    public class StatusPagePresenter : IStatusPagePresenter
    {
        private readonly IConnectionStatusAccessor _connectionStatusAccessor;

        public StatusPagePresenter(IConnectionStatusAccessor connectionStatusAccessor)
        {
            _connectionStatusAccessor = connectionStatusAccessor;
        }

        public ConnectivityStatusModel GetStatus()
        {
            return _connectionStatusAccessor.GetStatus() switch
            {
                ConnectionStatus.Connected connected => new ConnectivityStatusModel(ConnectionStatusModel.Active, connected.Uptime),
                ConnectionStatus.Disconnected _ => new ConnectivityStatusModel(ConnectionStatusModel.Inactive, null),
                ConnectionStatus.NotEnabled _ => new ConnectivityStatusModel(ConnectionStatusModel.NotConfigured, null),
                object value => throw new NotSupportedException($"Connection status {value.GetType().FullName} is not supported")
            };
        }
    }
}
