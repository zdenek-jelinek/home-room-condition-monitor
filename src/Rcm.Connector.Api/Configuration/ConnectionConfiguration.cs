namespace Rcm.Connector.Api.Configuration
{
    public class ConnectionConfiguration
    {
        public string BaseUri { get; }
        public string DeviceIdentifier { get; }
        public string DeviceKey { get; }

        public ConnectionConfiguration(string baseUri, string deviceIdentifier, string deviceKey)
        {
            BaseUri = baseUri;
            DeviceIdentifier = deviceIdentifier;
            DeviceKey = deviceKey;
        }
    }
}
