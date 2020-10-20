using Rcm.Device.Connector.Api.Configuration;

namespace Rcm.Device.Connector.Configuration
{
    public interface IConnectionConfigurationReader
    {
        ConnectionConfiguration? ReadConfiguration();
    }
}
