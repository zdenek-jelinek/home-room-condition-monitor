namespace Rcm.Connector.Api.Status
{
    public interface IConnectionStatusAccessor
    {
        ConnectionStatus GetStatus();
    }
}
