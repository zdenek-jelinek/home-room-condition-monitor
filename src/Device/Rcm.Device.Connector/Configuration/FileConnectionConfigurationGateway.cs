using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Rcm.Device.Connector.Api.Configuration;

namespace Rcm.Device.Connector.Configuration;

public class FileConnectionConfigurationGateway
    : IConnectionConfigurationGateway,
        IConnectionConfigurationReader,
        IConnectionConfigurationWriter
{
    private readonly ILogger<FileConnectionConfigurationGateway> _logger;
    private readonly IFileBackendStorageLocation _storageLocation;

    private string BackendConfigurationFilePath =>
        Path.Combine(_storageLocation.GetDirectoryPath(), "connection.json");

    public FileConnectionConfigurationGateway(
        ILogger<FileConnectionConfigurationGateway> logger,
        IFileBackendStorageLocation storageLocation)
    {
        _logger = logger;
        _storageLocation = storageLocation;
    }

    public ConnectionConfiguration? ReadConfiguration()
    {
        if (!File.Exists(BackendConfigurationFilePath))
        {
            _logger.LogDebug("Skipping configuration read - file does not exist");
            return null;
        }

        return Parse(File.ReadAllText(BackendConfigurationFilePath));
    }

    public void WriteConfiguration(ConnectionConfiguration configuration)
    {
        File.WriteAllText(BackendConfigurationFilePath, Serialize(configuration));
    }

    public void EraseConfiguration()
    {
        if (!File.Exists(BackendConfigurationFilePath))
        {
            return;
        }

        File.Delete(BackendConfigurationFilePath);
    }

    private string Serialize(ConnectionConfiguration configuration)
    {
        var persistenceModel = new BackendConfigurationPersistenceModel
        {
            baseUri = configuration.BaseUri,
            deviceIdentifier = configuration.DeviceIdentifier,
            deviceKey = configuration.DeviceKey
        };

        return JsonSerializer.Serialize(persistenceModel);
    }

    private ConnectionConfiguration? Parse(string data)
    {
        var persistenceModel = Deserialize(data);
        if (persistenceModel is null)
        {
            _logger.LogDebug("Skipping configuration read - file could not be parsed as json");
            return null;
        }

        if (persistenceModel.baseUri is null
            || persistenceModel.deviceIdentifier is null
            || persistenceModel.deviceKey is null)
        {
            _logger.LogDebug("Skipping configuration read - file does not contain all expected properties");
            return null;
        }

        return new ConnectionConfiguration(
            baseUri: persistenceModel.baseUri,
            deviceIdentifier: persistenceModel.deviceIdentifier,
            deviceKey: persistenceModel.deviceKey);
    }

    private static BackendConfigurationPersistenceModel? Deserialize(string data)
    {
        try
        {
            return JsonSerializer.Deserialize<BackendConfigurationPersistenceModel>(data);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Serialization contract")]
    private class BackendConfigurationPersistenceModel
    {
        public string? baseUri { get; set; }
        public string? deviceIdentifier { get; set; }
        public string? deviceKey { get; set; }
    }
}