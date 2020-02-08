using Rcm.Connector.Api.Configuration;

namespace Rcm.Connector.Configuration
{
    public interface IConnectionConfigurationWriter
    {
        void WriteConfiguration(ConnectionConfiguration configuration);
    }
}