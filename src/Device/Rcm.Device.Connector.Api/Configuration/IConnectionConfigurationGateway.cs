namespace Rcm.Device.Connector.Api.Configuration;

public interface IConnectionConfigurationGateway
{
    ConnectionConfiguration? ReadConfiguration();
    void WriteConfiguration(ConnectionConfiguration configuration);
    void EraseConfiguration();
}