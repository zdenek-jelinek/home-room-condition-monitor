using Rcm.Connector.Api.Configuration;

namespace Rcm.Connector.Configuration
{
    public interface IConnectionConfigurationReader
    {
        ConnectionConfiguration? ReadConfiguration();
    }
}
